using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public interface IPricingService
    {
        decimal GetBasePrice(string serviceType, string weightTier, string zone, bool isFlatRate = false);
        decimal GetDiscountRate(int myPostBand, string serviceType);
        decimal GetFlatRatePrice(FlatRateSize size, ServiceType serviceType);
        bool IsFlatRateEligible(double weight, PackagingType packagingType);
        decimal GetPerKgCharge(string zone);
    }
}
