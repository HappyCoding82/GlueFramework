using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public class PostageCalculator : IPostageCalculator
    {
        private readonly IWeightCalculator _weightCalculator;
        private readonly IZoneService _zoneService;
        private readonly IPricingService _pricingService;

        public PostageCalculator(
            IWeightCalculator weightCalculator,
            IZoneService zoneService,
            IPricingService pricingService)
        {
            _weightCalculator = weightCalculator;
            _zoneService = zoneService;
            _pricingService = pricingService;
        }

        public async Task<PostageResult> CalculatePostageAsync(ParcelRequest request)
        {
            if (request.IsInternational)
                return await CalculateInternationalPostageAsync(request);
            else
                return await CalculateDomesticPostageAsync(request);
        }

        public async Task<PostageResult> CalculateDomesticPostageAsync(ParcelRequest request)
        {
            var result = new PostageResult
            {
                PhysicalWeight = request.Weight,
                MyPostBand = request.MyPostBand,
                ServiceType = request.ServiceType.ToString()
            };

            // Calculate weights
            result.CubicWeight = _weightCalculator.CalculateCubicWeight(request.Length, request.Width, request.Height);
            result.ChargeableWeight = _weightCalculator.GetChargeableWeight(result.PhysicalWeight, result.CubicWeight);
            result.WeightTier = _weightCalculator.GetWeightTier(result.ChargeableWeight);

            result.CalculationSteps.Add($"Physical weight: {result.PhysicalWeight:F2}kg");
            result.CalculationSteps.Add($"Cubic weight: {result.CubicWeight:F2}kg (L{request.Length}×W{request.Width}×H{request.Height}cm × 250)");
            result.CalculationSteps.Add($"Chargeable weight: {result.ChargeableWeight:F2}kg (greater of physical/cubic)");

            // Determine zone
            var zoneInfo = _zoneService.GetDomesticZone(request.FromPostcode, request.ToPostcode);
            result.DestinationZone = zoneInfo.Zone;
            result.CalculationSteps.Add($"Destination zone: {zoneInfo.Zone} ({zoneInfo.Description})");

            // Check if flat rate applies
            if (_pricingService.IsFlatRateEligible(request.Weight, request.PackagingType))
            {
                result.IsFlatRate = true;
                result.FlatRateSize = DetermineFlatRateSize(request.Length, request.Width, request.Height);
                result.BasePrice = _pricingService.GetFlatRatePrice(
                    Enum.Parse<FlatRateSize>(result.FlatRateSize), 
                    request.ServiceType);
                result.CalculationSteps.Add($"Flat rate packaging: {result.FlatRateSize}");
            }
            else
            {
                result.IsFlatRate = false;
                result.BasePrice = _pricingService.GetBasePrice(
                    request.ServiceType.ToString(), 
                    result.WeightTier, 
                    zoneInfo.Zone);

                // For parcels over 5kg, add per kg charge
                if (result.ChargeableWeight > 5.0)
                {
                    var baseCharge = result.BasePrice;
                    var perKgCharge = _pricingService.GetPerKgCharge(zoneInfo.Zone);
                    var additionalKg = (decimal)Math.Ceiling(result.ChargeableWeight - 5.0);
                    result.BasePrice = baseCharge + (perKgCharge * additionalKg);
                    result.CalculationSteps.Add($"Base charge (first 5kg): ${baseCharge:F2}");
                    result.CalculationSteps.Add($"Additional {additionalKg}kg × ${perKgCharge:F2} = ${(perKgCharge * additionalKg):F2}");
                }
            }

            result.CalculationSteps.Add($"Base price: ${result.BasePrice:F2}");

            // Apply MyPost Business discount
            var discountRate = _pricingService.GetDiscountRate(request.MyPostBand, request.ServiceType.ToString());
            result.DiscountPercentage = discountRate * 100;
            result.DiscountAmount = result.BasePrice * discountRate;
            result.FinalPrice = result.BasePrice - result.DiscountAmount;

            result.CalculationSteps.Add($"MyPost Band {request.MyPostBand} discount: {result.DiscountPercentage:F1}% = -${result.DiscountAmount:F2}");
            result.CalculationSteps.Add($"Final price: ${result.FinalPrice:F2}");

            return await Task.FromResult(result);
        }

        public async Task<PostageResult> CalculateInternationalPostageAsync(ParcelRequest request)
        {
            var result = new PostageResult
            {
                PhysicalWeight = request.Weight,
                MyPostBand = request.MyPostBand,
                ServiceType = request.ServiceType.ToString()
            };

            // International parcels use physical weight only (no cubic weight)
            result.ChargeableWeight = result.PhysicalWeight;
            result.CubicWeight = 0; // Not used for international
            result.WeightTier = _weightCalculator.GetWeightTier(result.ChargeableWeight);

            result.CalculationSteps.Add($"Physical weight: {result.PhysicalWeight:F2}kg");
            result.CalculationSteps.Add($"International shipping - physical weight only");

            // Determine international zone
            result.DestinationZone = _zoneService.GetInternationalZone(request.DestinationCountry);
            result.CalculationSteps.Add($"Destination country: {request.DestinationCountry}");
            result.CalculationSteps.Add($"International zone: {result.DestinationZone}");

            // Get base price (simplified - would need actual international rates)
            result.BasePrice = GetInternationalBasePrice(request.ServiceType.ToString(), result.WeightTier, result.DestinationZone);
            result.CalculationSteps.Add($"Base international price: ${result.BasePrice:F2}");

            // Apply MyPost Business discount
            var discountRate = _pricingService.GetDiscountRate(request.MyPostBand, request.ServiceType.ToString());
            result.DiscountPercentage = discountRate * 100;
            result.DiscountAmount = result.BasePrice * discountRate;
            result.FinalPrice = result.BasePrice - result.DiscountAmount;

            result.CalculationSteps.Add($"MyPost Band {request.MyPostBand} discount: {result.DiscountPercentage:F1}% = -${result.DiscountAmount:F2}");
            result.CalculationSteps.Add($"Final price: ${result.FinalPrice:F2}");

            return await Task.FromResult(result);
        }

        public bool ValidateParcelDimensions(ParcelRequest request)
        {
            if (request.IsInternational)
            {
                // International limits
                return request.Weight <= 20.0 && 
                       request.Length <= 105 && 
                       (request.Length + 2 * (request.Width + request.Height)) <= 140; // Girth
            }
            else
            {
                // Domestic limits
                double cubicVolume = (request.Length / 100.0) * (request.Width / 100.0) * (request.Height / 100.0);
                return request.Weight <= 22.0 && 
                       request.Length <= 105 && 
                       cubicVolume <= 0.25; // 0.25 cubic meters
            }
        }

        public List<string> GetValidationErrors(ParcelRequest request)
        {
            var errors = new List<string>();

            if (request.Weight <= 0)
                errors.Add("Weight must be greater than 0");

            if (request.Length <= 0 || request.Width <= 0 || request.Height <= 0)
                errors.Add("All dimensions must be greater than 0");

            if (request.IsInternational)
            {
                if (string.IsNullOrEmpty(request.DestinationCountry))
                    errors.Add("Destination country is required for international shipping");

                if (request.Weight > 20.0)
                    errors.Add("International parcels cannot exceed 20kg");

                if (request.Length > 105)
                    errors.Add("International parcels cannot exceed 105cm in length");

                double girth = request.Length + 2 * (request.Width + request.Height);
                if (girth > 140)
                    errors.Add($"International parcels cannot exceed 140cm girth (current: {girth}cm)");
            }
            else
            {
                if (string.IsNullOrEmpty(request.FromPostcode) || string.IsNullOrEmpty(request.ToPostcode))
                    errors.Add("Both from and to postcodes are required for domestic shipping");

                if (!_zoneService.IsValidAustralianPostcode(request.FromPostcode))
                    errors.Add($"Invalid from postcode: {request.FromPostcode}");

                if (!_zoneService.IsValidAustralianPostcode(request.ToPostcode))
                    errors.Add($"Invalid to postcode: {request.ToPostcode}");

                if (request.Weight > 22.0)
                    errors.Add("Domestic parcels cannot exceed 22kg");

                if (request.Length > 105)
                    errors.Add("Domestic parcels cannot exceed 105cm in length");

                double cubicVolume = (request.Length / 100.0) * (request.Width / 100.0) * (request.Height / 100.0);
                if (cubicVolume > 0.25)
                    errors.Add($"Domestic parcels cannot exceed 0.25 cubic meters (current: {cubicVolume:F3}m³)");
            }

            if (request.PackagingType == PackagingType.FlatRate && request.Weight > 5.0)
                errors.Add("Flat rate packaging is only available for parcels up to 5kg");

            if (request.MyPostBand < 0 || request.MyPostBand > 5)
                errors.Add("MyPost band must be between 0 and 5");

            return errors;
        }

        private string DetermineFlatRateSize(double length, double width, double height)
        {
            // Simplified logic - would need actual Australia Post flat rate dimensions
            double maxDimension = Math.Max(Math.Max(length, width), height);
            
            if (maxDimension <= 22) return "ExtraSmall";
            if (maxDimension <= 30) return "Small";
            if (maxDimension <= 38) return "Medium";
            if (maxDimension <= 43) return "Large";
            return "ExtraLarge";
        }

        private decimal GetInternationalBasePrice(string serviceType, string weightTier, string zone)
        {
            // Simplified international pricing - would need actual rates
            var basePrice = zone switch
            {
                "Zone A" => 25.0m,
                "Zone B" => 35.0m,
                "Zone C" => 45.0m,
                "Zone D" => 55.0m,
                _ => 60.0m
            };

            // Adjust for weight tier
            var weightMultiplier = weightTier switch
            {
                "≤500g" => 1.0m,
                ">500g - ≤1kg" => 1.2m,
                ">1kg - ≤3kg" => 1.5m,
                ">3kg - ≤5kg" => 2.0m,
                ">5kg - ≤22kg" => 3.0m,
                _ => 1.0m
            };

            // Adjust for service type
            var serviceMultiplier = serviceType == "ExpressPost" ? 1.5m : 1.0m;

            return basePrice * weightMultiplier * serviceMultiplier;
        }
    }
}
