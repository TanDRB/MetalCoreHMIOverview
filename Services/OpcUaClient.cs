using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;
using MetalCoreHMIOverview.Models.Options;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Client;
using ISession = Opc.Ua.Client.ISession;

// SelectEndpoint / Session.Create đang được đánh dấu obsolete trong thư viện 1.5.378 nhưng vẫn là cách đơn giản nhất.
#pragma warning disable CS0618
namespace MetalCoreHMIOverview.Services
{
    /// <summary>
    /// Client OPC UA tới Kepware (opc.tcp://host:49320, SecurityPolicy None).
    /// Singleton: giữ một session và tự kết nối lại khi bị mất.
    /// </summary>
    public sealed class OpcUaClient : IOpcUaClient
    {
        private readonly OpcUaOptions _opt;
        private readonly ILogger<OpcUaClient> _log;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private ApplicationConfiguration? _config;
        private ISession? _session;
        private Subscription? _subscription;
        private ISession? _subscriptionSession;
        private string _tagKey = string.Empty;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, OpcReadResult> _latest = new();

        public OpcUaClient(IOptions<OpcUaOptions> options, ILogger<OpcUaClient> log)
        {
            _opt = options.Value;
            _log = log;
        }

        public bool IsConnected => _session?.Connected == true;
        public string? LastError { get; private set; }

        /// <summary>
        /// Đăng ký (subscription) toàn bộ tag với Kepware; giá trị mới được đẩy về và lưu trong bộ nhớ.
        /// Mỗi lần gọi trả về giá trị mới nhất của từng tag. Cách này không phải chờ từng thiết bị phản hồi.
        /// </summary>
        public async Task<IReadOnlyList<OpcReadResult>> ReadAsync(IReadOnlyList<TagDefinition> tags, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            if (tags.Count == 0) return Array.Empty<OpcReadResult>();

            try
            {
                var session = await EnsureConnectedAsync(ct);

                var key = string.Join('|', tags.Select(t => $"{t.Id}:{t.NodeId}"));
                if (_subscription == null || key != _tagKey || !ReferenceEquals(session, _subscriptionSession))
                    await RebuildSubscriptionAsync(session, tags, key, ct);

                LastError = null;
                return tags
                    .Select(t => _latest.TryGetValue(t.Id, out var r) ? r : new OpcReadResult(t.Id, null, false, now))
                    .ToList();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _log.LogWarning("OPC UA lỗi: {Message}", ex.Message);
                await DropSessionAsync();

                // Mất kết nối: trả về chất lượng Bad để giao diện biết
                return tags.Select(t => new OpcReadResult(t.Id, null, false, now)).ToList();
            }
        }

        private async Task RebuildSubscriptionAsync(ISession session, IReadOnlyList<TagDefinition> tags, string key, CancellationToken ct)
        {
            await RemoveSubscriptionAsync();
            _latest.Clear();

            var sub = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = _opt.PollIntervalMs,
                KeepAliveCount = 10,
                LifetimeCount = 100,
                PublishingEnabled = true,
                DisplayName = "MetalCoreHMI"
            };

            foreach (var t in tags)
            {
                var item = new MonitoredItem(sub.DefaultItem)
                {
                    StartNodeId = NodeId.Parse(t.NodeId),
                    AttributeId = Attributes.Value,
                    DisplayName = t.Id.ToString(),
                    SamplingInterval = _opt.PollIntervalMs,
                    QueueSize = 1,
                    DiscardOldest = true
                };
                item.Notification += OnNotification;
                sub.AddItem(item);
            }

            session.AddSubscription(sub);
            await sub.CreateAsync(ct);

            _subscription = sub;
            _subscriptionSession = session;
            _tagKey = key;
            _log.LogInformation("Đã đăng ký {Count} tag với Kepware", tags.Count);
        }

        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            if (!int.TryParse(item.DisplayName, out var tagId)) return;
            foreach (var dv in item.DequeueValues())
            {
                var good = StatusCode.IsGood(dv.StatusCode);
                _latest[tagId] = new OpcReadResult(tagId, ToDouble(dv.Value), good, DateTime.UtcNow);
            }
        }

        private async Task RemoveSubscriptionAsync()
        {
            var sub = _subscription;
            var ses = _subscriptionSession;
            _subscription = null;
            _subscriptionSession = null;
            _tagKey = string.Empty;
            if (sub == null) return;
            try { if (ses != null && ses.Connected) await ses.RemoveSubscriptionAsync(sub); } catch { /* bỏ qua */ }
            try { sub.Dispose(); } catch { /* bỏ qua */ }
        }

        public async Task<IReadOnlyList<OpcNodeDto>> BrowseAsync(string? nodeId, CancellationToken ct)
        {
            var session = await EnsureConnectedAsync(ct);
            var start = string.IsNullOrWhiteSpace(nodeId) ? ObjectIds.ObjectsFolder : NodeId.Parse(nodeId);

            var request = new BrowseDescriptionCollection
            {
                new BrowseDescription
                {
                    NodeId = start,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            var response = await session.BrowseAsync(null, null, 0, request, ct);
            var refs = response.Results.Count > 0 ? response.Results[0].References : new ReferenceDescriptionCollection();

            return refs.Select(r => new OpcNodeDto(
                ExpandedNodeId.ToNodeId(r.NodeId, session.NamespaceUris).ToString(),
                r.DisplayName.Text,
                r.NodeClass.ToString(),
                r.NodeClass == NodeClass.Object)).ToList();
        }

        private async Task<ISession> EnsureConnectedAsync(CancellationToken ct)
        {
            if (_session is { Connected: true }) return _session;

            await _gate.WaitAsync(ct);
            try
            {
                if (_session is { Connected: true }) return _session;
                await DropSessionAsync();

                _config ??= await BuildConfigurationAsync();

                _log.LogInformation("Kết nối OPC UA: {Url}", _opt.EndpointUrl);

                var endpointDesc = CoreClientUtils.SelectEndpoint(_config, _opt.EndpointUrl, useSecurity: false);
                var endpoint = new ConfiguredEndpoint(null, endpointDesc, EndpointConfiguration.Create(_config));

                IUserIdentity identity = string.IsNullOrWhiteSpace(_opt.Username)
                    ? new UserIdentity(new AnonymousIdentityToken())
                    : new UserIdentity(_opt.Username, System.Text.Encoding.UTF8.GetBytes(_opt.Password ?? string.Empty));

                _session = await Session.Create(
                    _config, endpoint, false, _opt.ApplicationName,
                    (uint)_opt.SessionTimeoutMs, identity, null, ct);

                _log.LogInformation("Đã kết nối OPC UA ({Url})", _opt.EndpointUrl);
                return _session;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<ApplicationConfiguration> BuildConfigurationAsync()
        {
            var pki = Path.Combine(AppContext.BaseDirectory, "pki");
            CertificateTrustList Store(string name) => new() { StoreType = "Directory", StorePath = Path.Combine(pki, name) };

            var config = new ApplicationConfiguration
            {
                ApplicationName = _opt.ApplicationName,
                ApplicationType = ApplicationType.Client,
                ApplicationUri = $"urn:{Utils.GetHostName()}:{_opt.ApplicationName}",
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(pki, "own"),
                        SubjectName = $"CN={_opt.ApplicationName}"
                    },
                    TrustedIssuerCertificates = Store("issuer"),
                    TrustedPeerCertificates = Store("trusted"),
                    RejectedCertificateStore = Store("rejected"),
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = _opt.SessionTimeoutMs }
            };

            await config.ValidateAsync(ApplicationType.Client);
            config.CertificateValidator.CertificateValidation += (_, e) => e.Accept = true;
            return config;
        }

        private async Task DropSessionAsync()
        {
            await RemoveSubscriptionAsync();
            var s = _session;
            _session = null;
            if (s == null) return;
            try { await s.CloseAsync(); } catch { /* bỏ qua */ }
            s.Dispose();
        }

        private static double? ToDouble(object? v)
        {
            try
            {
                return v switch
                {
                    null => null,
                    bool b => b ? 1 : 0,
                    string s => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
                    _ => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)
                };
            }
            catch { return null; }
        }

        public async ValueTask DisposeAsync()
        {
            await DropSessionAsync();
            _gate.Dispose();
        }
    }
}
