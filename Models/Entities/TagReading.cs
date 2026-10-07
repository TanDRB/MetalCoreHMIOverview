namespace MetalCoreHMIOverview.Models.Entities
{
    public class TagReading
    {
        public long Id { get; set; }
        public int TagDefinitionId { get; set; }
        public TagDefinition? TagDefinition { get; set; }

        public double? Value { get; set; }

        /// <summary>true khi OPC UA trả về StatusCode Good.</summary>
        public bool IsGood { get; set; }

        public DateTime TimestampUtc { get; set; }
    }
}
