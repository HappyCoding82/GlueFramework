namespace AustraliaPostCalculator.Models
{
    public class ParcelRequest
    {
        public double Length { get; set; } // cm
        public double Width { get; set; }  // cm
        public double Height { get; set; } // cm
        public double Weight { get; set; } // kg
        public string FromPostcode { get; set; }
        public string ToPostcode { get; set; }
        public PackagingType PackagingType { get; set; }
        public ServiceType ServiceType { get; set; }
        public int MyPostBand { get; set; } = 0; // 0-5
        public bool IsInternational { get; set; } = false;
        public string DestinationCountry { get; set; }
    }

    public enum PackagingType
    {
        FlatRate,    // Australia Post flat rate packaging (≤5kg)
        OwnPackaging // Own packaging
    }

    public enum ServiceType
    {
        ParcelPost,
        ExpressPost
    }

    public enum FlatRateSize
    {
        ExtraSmall,
        Small,
        Medium,
        Large,
        ExtraLarge
    }
}
