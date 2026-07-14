using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public class WeightCalculator : IWeightCalculator
    {
        private const double CUBIC_CONVERSION_FACTOR = 250;

        public double CalculateCubicWeight(double lengthCm, double widthCm, double heightCm)
        {
            // Convert cm to meters and calculate cubic weight
            double lengthM = lengthCm / 100;
            double widthM = widthCm / 100;
            double heightM = heightCm / 100;
            
            double cubicVolume = lengthM * widthM * heightM;
            return cubicVolume * CUBIC_CONVERSION_FACTOR;
        }

        public double GetChargeableWeight(double physicalWeight, double cubicWeight)
        {
            // Return the greater of physical weight or cubic weight
            return Math.Max(physicalWeight, cubicWeight);
        }

        public string GetWeightTier(double chargeableWeight)
        {
            if (chargeableWeight <= 0.5)
                return "≤500g";
            else if (chargeableWeight <= 1.0)
                return ">500g - ≤1kg";
            else if (chargeableWeight <= 3.0)
                return ">1kg - ≤3kg";
            else if (chargeableWeight <= 5.0)
                return ">3kg - ≤5kg";
            else if (chargeableWeight <= 22.0)
                return ">5kg - ≤22kg";
            else
                return ">22kg (Over limit)";
        }
    }
}
