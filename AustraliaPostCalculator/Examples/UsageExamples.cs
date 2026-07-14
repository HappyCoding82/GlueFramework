using AustraliaPostCalculator.Models;
using AustraliaPostCalculator.Services;

namespace AustraliaPostCalculator.Examples
{
    public class UsageExamples
    {
        public static async Task Example1_DomesticParcel()
        {
            Console.WriteLine("=== Example 1: Domestic Parcel Calculation ===");
            
            var calculator = new PostageCalculator(
                new WeightCalculator(),
                new ZoneService(),
                new PricingService()
            );

            var request = new ParcelRequest
            {
                Length = 50.5,  // cm
                Width = 40.3,   // cm
                Height = 30.9,  // cm
                Weight = 10.0,  // kg
                FromPostcode = "2000", // Sydney
                ToPostcode = "3000",   // Melbourne
                PackagingType = PackagingType.OwnPackaging,
                ServiceType = ServiceType.ParcelPost,
                MyPostBand = 2,
                IsInternational = false
            };

            var result = await calculator.CalculateDomesticPostageAsync(request);
            
            Console.WriteLine($"Physical Weight: {result.PhysicalWeight}kg");
            Console.WriteLine($"Cubic Weight: {result.CubicWeight:F2}kg");
            Console.WriteLine($"Chargeable Weight: {result.ChargeableWeight:F2}kg");
            Console.WriteLine($"Weight Tier: {result.WeightTier}");
            Console.WriteLine($"Destination Zone: {result.DestinationZone}");
            Console.WriteLine($"Base Price: ${result.BasePrice:F2}");
            Console.WriteLine($"Discount ({result.DiscountPercentage:F1}%): -${result.DiscountAmount:F2}");
            Console.WriteLine($"Final Price: ${result.FinalPrice:F2}");
            
            Console.WriteLine("\nCalculation Steps:");
            foreach (var step in result.CalculationSteps)
            {
                Console.WriteLine($"  - {step}");
            }
        }

        public static async Task Example2_FlatRatePackaging()
        {
            Console.WriteLine("\n=== Example 2: Flat Rate Packaging ===");
            
            var calculator = new PostageCalculator(
                new WeightCalculator(),
                new ZoneService(),
                new PricingService()
            );

            var request = new ParcelRequest
            {
                Length = 20,
                Width = 15,
                Height = 5,
                Weight = 1.5,
                FromPostcode = "2000",
                ToPostcode = "2001",
                PackagingType = PackagingType.FlatRate,
                ServiceType = ServiceType.ParcelPost,
                MyPostBand = 1,
                IsInternational = false
            };

            var result = await calculator.CalculateDomesticPostageAsync(request);
            
            Console.WriteLine($"Flat Rate Size: {result.FlatRateSize}");
            Console.WriteLine($"Base Price: ${result.BasePrice:F2}");
            Console.WriteLine($"Final Price: ${result.FinalPrice:F2}");
        }

        public static async Task Example3_InternationalShipping()
        {
            Console.WriteLine("\n=== Example 3: International Shipping ===");
            
            var calculator = new PostageCalculator(
                new WeightCalculator(),
                new ZoneService(),
                new PricingService()
            );

            var request = new ParcelRequest
            {
                Length = 30,
                Width = 20,
                Height = 10,
                Weight = 2.5,
                DestinationCountry = "UNITED STATES",
                PackagingType = PackagingType.OwnPackaging,
                ServiceType = ServiceType.ExpressPost,
                MyPostBand = 3,
                IsInternational = true
            };

            var result = await calculator.CalculateInternationalPostageAsync(request);
            
            Console.WriteLine($"Destination Country: {request.DestinationCountry}");
            Console.WriteLine($"International Zone: {result.DestinationZone}");
            Console.WriteLine($"Weight: {result.ChargeableWeight}kg");
            Console.WriteLine($"Service Type: {result.ServiceType}");
            Console.WriteLine($"Base Price: ${result.BasePrice:F2}");
            Console.WriteLine($"Final Price: ${result.FinalPrice:F2}");
        }

        public static void Example4_Validation()
        {
            Console.WriteLine("\n=== Example 4: Validation Examples ===");
            
            var calculator = new PostageCalculator(
                new WeightCalculator(),
                new ZoneService(),
                new PricingService()
            );

            // Valid parcel
            var validRequest = new ParcelRequest
            {
                Length = 30,
                Width = 20,
                Height = 10,
                Weight = 5.0,
                FromPostcode = "2000",
                ToPostcode = "3000",
                PackagingType = PackagingType.OwnPackaging,
                ServiceType = ServiceType.ParcelPost,
                MyPostBand = 0,
                IsInternational = false
            };

            var isValid = calculator.ValidateParcelDimensions(validRequest);
            var errors = calculator.GetValidationErrors(validRequest);
            
            Console.WriteLine($"Valid parcel: {isValid}");
            Console.WriteLine($"Errors: {(errors.Any() ? string.Join(", ", errors) : "None")}");

            // Invalid parcel
            var invalidRequest = new ParcelRequest
            {
                Length = 150, // Over limit
                Width = 20,
                Height = 10,
                Weight = 25.0, // Over limit
                FromPostcode = "invalid",
                ToPostcode = "123",
                PackagingType = PackagingType.FlatRate,
                ServiceType = ServiceType.ParcelPost,
                MyPostBand = 6, // Invalid band
                IsInternational = false
            };

            isValid = calculator.ValidateParcelDimensions(invalidRequest);
            errors = calculator.GetValidationErrors(invalidRequest);
            
            Console.WriteLine($"\nInvalid parcel: {isValid}");
            Console.WriteLine("Errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        public static void Example5_ZoneInformation()
        {
            Console.WriteLine("\n=== Example 5: Zone Information ===");
            
            var zoneService = new ZoneService();

            // Domestic zones
            Console.WriteLine("Domestic Zone Examples:");
            var domesticExamples = new[]
            {
                ("2000", "3000", "Sydney to Melbourne"),
                ("2000", "2001", "Sydney to Sydney"),
                ("0800", "3000", "Darwin to Melbourne"),
                ("6000", "7000", "Perth to Tasmania")
            };

            foreach (var (from, to, description) in domesticExamples)
            {
                var zone = zoneService.GetDomesticZone(from, to);
                Console.WriteLine($"  {description}: {zone.Zone} - {zone.Description}");
            }

            // International zones
            Console.WriteLine("\nInternational Zone Examples:");
            var countries = new[] { "NEW ZEALAND", "UNITED KINGDOM", "CHINA", "BRAZIL" };
            foreach (var country in countries)
            {
                var zone = zoneService.GetInternationalZone(country);
                Console.WriteLine($"  {country}: {zone}");
            }
        }

        public static void Example6_WeightCalculations()
        {
            Console.WriteLine("\n=== Example 6: Weight Calculations ===");
            
            var weightCalculator = new WeightCalculator();

            var examples = new[]
            {
                (50.5, 40.3, 30.9, 10.0, "Australia Post example"),
                (30, 20, 10, 2.0, "Small parcel"),
                (100, 50, 40, 5.0, "Large parcel")
            };

            foreach (var (length, width, height, physicalWeight, description) in examples)
            {
                var cubicWeight = weightCalculator.CalculateCubicWeight(length, width, height);
                var chargeableWeight = weightCalculator.GetChargeableWeight(physicalWeight, cubicWeight);
                var weightTier = weightCalculator.GetWeightTier(chargeableWeight);

                Console.WriteLine($"\n{description}:");
                Console.WriteLine($"  Dimensions: {length}×{width}×{height}cm");
                Console.WriteLine($"  Physical weight: {physicalWeight}kg");
                Console.WriteLine($"  Cubic weight: {cubicWeight:F2}kg");
                Console.WriteLine($"  Chargeable weight: {chargeableWeight:F2}kg");
                Console.WriteLine($"  Weight tier: {weightTier}");
            }
        }
    }
}
