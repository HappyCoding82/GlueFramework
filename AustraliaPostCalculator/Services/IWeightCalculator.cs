using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public interface IWeightCalculator
    {
        double CalculateCubicWeight(double lengthCm, double widthCm, double heightCm);
        double GetChargeableWeight(double physicalWeight, double cubicWeight);
        string GetWeightTier(double chargeableWeight);
    }
}
