using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public interface IPostageCalculator
    {
        Task<PostageResult> CalculatePostageAsync(ParcelRequest request);
        Task<PostageResult> CalculateDomesticPostageAsync(ParcelRequest request);
        Task<PostageResult> CalculateInternationalPostageAsync(ParcelRequest request);
        bool ValidateParcelDimensions(ParcelRequest request);
        List<string> GetValidationErrors(ParcelRequest request);
    }
}
