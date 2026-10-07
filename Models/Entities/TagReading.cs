namespace MetalCoreHMIOverview.Models.Entities
{
    /// <summary>Một lần đọc giá trị của tag, lưu lịch sử vào SQL Server.</summary>
    public class TagReading
    {
        public long Id { get; set; }
        public int TagDefinitionId { get; set; }
        public TagDefinition? TagDefinition { get; set; }

        public double? Value { get; set; }

        /// <summary>true khi OPC UA trả về StatusCode Good.</summary>
        public bool IsGood { get; set; }

        /// <summary>Thời điểm đọc (UTC).</summary>
        public DateTime TimestampUtc { get; set; }
    }
}
