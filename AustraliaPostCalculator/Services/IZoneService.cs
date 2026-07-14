using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public interface IZoneService
    {
        ZoneInfo GetDomesticZone(string fromPostcode, string toPostcode);
        string GetInternationalZone(string country);
        bool IsValidPostcode(string postcode);
        bool IsValidAustralianPostcode(string postcode);
    }
}
