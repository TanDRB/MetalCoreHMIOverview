using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetalCoreHMIOverview.Controllers.Api
{
    /// <summary>API cho giao diện lấy dữ liệu tag (đọc từ Kepware, cache trong bộ nhớ).</summary>
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tags;
        private readonly ITagCache _cache;

        public TagsController(ITagService tags, ITagCache cache)
        {
            _tags = tags;
            _cache = cache;
        }

        /// <summary>
        /// GET /api/tags/stream - Server-Sent Events: đẩy toàn bộ giá trị tag về trình duyệt ngay khi có thay đổi
        /// (giao diện dùng EventSource, không cần thư viện). Cứ 15 giây không đổi thì gửi lại một lần để giữ kết nối.
        /// </summary>
        [HttpGet("stream")]
        public async Task Stream(CancellationToken ct)
        {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            var json = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
            var version = -1L;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    version = await _cache.WaitForChangeAsync(version, TimeSpan.FromSeconds(15), ct);
                    var payload = System.Text.Json.JsonSerializer.Serialize(_tags.GetLatest(), json);
                    await Response.WriteAsync($"data: {payload}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException) { /* trình duyệt đóng trang */ }
        }

        /// <summary>GET /api/tags/latest?machine=1&amp;section=Stage1</summary>
        [HttpGet("latest")]
        public ActionResult<IReadOnlyList<TagValueDto>> Latest([FromQuery] int? machine, [FromQuery] string? section) =>
            Ok(_tags.GetLatest(machine, section));

        /// <summary>GET /api/tags/{tagId}/history?from=...&amp;to=...&amp;take=500</summary>
        [HttpGet("{tagId:int}/history")]
        public async Task<ActionResult<IReadOnlyList<TagReadingDto>>> History(
            int tagId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int take = 500, CancellationToken ct = default) =>
            Ok(await _tags.GetHistoryAsync(tagId, from?.ToUniversalTime(), to?.ToUniversalTime(), take, ct));

        /// <summary>GET /api/tags/browse?nodeId=ns=2;s=Channel1 - duyệt cây tag Kepware để lấy NodeId (bỏ nodeId để xem gốc).</summary>
        [HttpGet("browse")]
        public async Task<ActionResult<IReadOnlyList<OpcNodeDto>>> Browse([FromQuery] string? nodeId, CancellationToken ct) =>
            Ok(await _tags.BrowseAsync(nodeId, ct));

        /// <summary>GET /api/tags/status - trạng thái kết nối OPC UA.</summary>
        [HttpGet("status")]
        public ActionResult<OpcStatusDto> Status() => Ok(_tags.GetStatus());
    }
}
