namespace AustraliaPostCalculator.Models
{
    public class PostageResult
    {
        public double ChargeableWeight { get; set; }
        public double PhysicalWeight { get; set; }
        public double CubicWeight { get; set; }
        public decimal BasePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string ServiceType { get; set; }
        public string DestinationZone { get; set; }
        public string WeightTier { get; set; }
        public bool IsFlatRate { get; set; }
        public string FlatRateSize { get; set; }
        public int MyPostBand { get; set; }
        public List<string> CalculationSteps { get; set; } = new List<string>();
    }

    public class ZoneInfo
    {
        public string Zone { get; set; }
        public string Description { get; set; }
        public bool IsSameState { get; set; }
        public bool IsRemote { get; set; }
    }
}
