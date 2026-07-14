using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public class PricingService : IPricingService
    {
        private readonly Dictionary<string, decimal> _basePrices;
        private readonly Dictionary<int, decimal> _discountRates;

        public PricingService()
        {
            _basePrices = InitializeBasePrices();
            _discountRates = InitializeDiscountRates();
        }

        public decimal GetBasePrice(string serviceType, string weightTier, string zone, bool isFlatRate = false)
        {
            if (isFlatRate)
                return 0; // Flat rates handled separately

            string key = $"{serviceType}_{weightTier}_{zone}";
            return _basePrices.GetValueOrDefault(key, 0);
        }

        public decimal GetDiscountRate(int myPostBand, string serviceType)
        {
            if (!_discountRates.ContainsKey(myPostBand))
                return 0;

            var baseDiscount = _discountRates[myPostBand];
            
            // Apply different discount rates based on service type and weight
            return serviceType switch
            {
                "ParcelPost" => baseDiscount,
                "ExpressPost" => baseDiscount * 0.8m, // Express Post typically has lower discounts
                _ => baseDiscount
            };
        }

        public decimal GetFlatRatePrice(FlatRateSize size, ServiceType serviceType)
        {
            string key = $"FlatRate_{size}_{serviceType}";
            return _basePrices.GetValueOrDefault(key, 0);
        }

        public bool IsFlatRateEligible(double weight, PackagingType packagingType)
        {
            return packagingType == PackagingType.FlatRate && weight <= 5.0;
        }

        private Dictionary<string, decimal> InitializeBasePrices()
        {
            return new Dictionary<string, decimal>
            {
                // Parcel Post prices (example rates - should be updated with current rates)
                // Up to 500g
                { "ParcelPost_≤500g_N1", 8.30m },
                { "ParcelPost_≤500g_N2", 9.70m },
                { "ParcelPost_≤500g_V1", 8.30m },
                { "ParcelPost_≤500g_V2", 9.70m },
                { "ParcelPost_≤500g_Q1", 8.30m },
                { "ParcelPost_≤500g_Q2", 9.70m },
                { "ParcelPost_≤500g_S1", 9.20m },
                { "ParcelPost_≤500g_S2", 10.60m },
                { "ParcelPost_≤500g_W1", 9.20m },
                { "ParcelPost_≤500g_W2", 10.60m },
                { "ParcelPost_≤500g_T1", 9.20m },
                { "ParcelPost_≤500g_T2", 10.60m },
                { "ParcelPost_≤500g_NT1", 12.50m },

                // >500g - ≤1kg
                { "ParcelPost_>500g - ≤1kg_N1", 10.80m },
                { "ParcelPost_>500g - ≤1kg_N2", 12.40m },
                { "ParcelPost_>500g - ≤1kg_V1", 10.80m },
                { "ParcelPost_>500g - ≤1kg_V2", 12.40m },
                { "ParcelPost_>500g - ≤1kg_Q1", 10.80m },
                { "ParcelPost_>500g - ≤1kg_Q2", 12.40m },
                { "ParcelPost_>500g - ≤1kg_S1", 11.90m },
                { "ParcelPost_>500g - ≤1kg_S2", 13.50m },
                { "ParcelPost_>500g - ≤1kg_W1", 11.90m },
                { "ParcelPost_>500g - ≤1kg_W2", 13.50m },
                { "ParcelPost_>500g - ≤1kg_T1", 11.90m },
                { "ParcelPost_>500g - ≤1kg_T2", 13.50m },
                { "ParcelPost_>500g - ≤1kg_NT1", 15.80m },

                // >1kg - ≤3kg
                { "ParcelPost_>1kg - ≤3kg_N1", 13.90m },
                { "ParcelPost_>1kg - ≤3kg_N2", 15.90m },
                { "ParcelPost_>1kg - ≤3kg_V1", 13.90m },
                { "ParcelPost_>1kg - ≤3kg_V2", 15.90m },
                { "ParcelPost_>1kg - ≤3kg_Q1", 13.90m },
                { "ParcelPost_>1kg - ≤3kg_Q2", 15.90m },
                { "ParcelPost_>1kg - ≤3kg_S1", 15.30m },
                { "ParcelPost_>1kg - ≤3kg_S2", 17.30m },
                { "ParcelPost_>1kg - ≤3kg_W1", 15.30m },
                { "ParcelPost_>1kg - ≤3kg_W2", 17.30m },
                { "ParcelPost_>1kg - ≤3kg_T1", 15.30m },
                { "ParcelPost_>1kg - ≤3kg_T2", 17.30m },
                { "ParcelPost_>1kg - ≤3kg_NT1", 20.30m },

                // >3kg - ≤5kg
                { "ParcelPost_>3kg - ≤5kg_N1", 16.80m },
                { "ParcelPost_>3kg - ≤5kg_N2", 19.20m },
                { "ParcelPost_>3kg - ≤5kg_V1", 16.80m },
                { "ParcelPost_>3kg - ≤5kg_V2", 19.20m },
                { "ParcelPost_>3kg - ≤5kg_Q1", 16.80m },
                { "ParcelPost_>3kg - ≤5kg_Q2", 19.20m },
                { "ParcelPost_>3kg - ≤5kg_S1", 18.50m },
                { "ParcelPost_>3kg - ≤5kg_S2", 20.90m },
                { "ParcelPost_>3kg - ≤5kg_W1", 18.50m },
                { "ParcelPost_>3kg - ≤5kg_W2", 20.90m },
                { "ParcelPost_>3kg - ≤5kg_T1", 18.50m },
                { "ParcelPost_>3kg - ≤5kg_T2", 20.90m },
                { "ParcelPost_>3kg - ≤5kg_NT1", 24.50m },

                // >5kg - ≤22kg (base charge + per kg charge)
                { "ParcelPost_>5kg - ≤22kg_N1", 23.30m }, // Base charge
                { "ParcelPost_>5kg - ≤22kg_N2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_V1", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_V2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_Q1", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_Q2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_S1", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_S2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_W1", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_W2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_T1", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_T2", 23.30m },
                { "ParcelPost_>5kg - ≤22kg_NT1", 23.30m },

                // Express Post prices (example rates)
                { "ExpressPost_≤500g_N1", 11.70m },
                { "ExpressPost_≤500g_N2", 13.20m },
                { "ExpressPost_>500g - ≤1kg_N1", 14.50m },
                { "ExpressPost_>500g - ≤1kg_N2", 16.20m },
                { "ExpressPost_>1kg - ≤3kg_N1", 18.90m },
                { "ExpressPost_>1kg - ≤3kg_N2", 21.10m },
                { "ExpressPost_>3kg - ≤5kg_N1", 22.80m },
                { "ExpressPost_>3kg - ≤5kg_N2", 25.50m },

                // Flat Rate prices
                { "FlatRate_ExtraSmall_ParcelPost", 8.30m },
                { "FlatRate_Small_ParcelPost", 11.70m },
                { "FlatRate_Medium_ParcelPost", 16.20m },
                { "FlatRate_Large_ParcelPost", 21.10m },
                { "FlatRate_ExtraLarge_ParcelPost", 28.90m },

                { "FlatRate_ExtraSmall_ExpressPost", 11.70m },
                { "FlatRate_Small_ExpressPost", 14.50m },
                { "FlatRate_Medium_ExpressPost", 18.90m },
                { "FlatRate_Large_ExpressPost", 24.50m },
                { "FlatRate_ExtraLarge_ExpressPost", 32.90m }
            };
        }

        private Dictionary<int, decimal> InitializeDiscountRates()
        {
            return new Dictionary<int, decimal>
            {
                { 0, 0.00m },    // No discount
                { 1, 0.05m },    // 5% discount
                { 2, 0.10m },    // 10% discount
                { 3, 0.125m },   // 12.5% discount
                { 4, 0.15m },    // 15% discount
                { 5, 0.175m }    // 17.5% discount
            };
        }

        public decimal GetPerKgCharge(string zone)
        {
            // Per kg charges for parcels over 5kg
            return zone switch
            {
                "N1" or "V1" or "Q1" or "S1" or "W1" or "T1" => 2.50m, // Same state metro
                "N2" or "V2" or "Q2" or "S2" or "W2" or "T2" => 3.20m, // Same state regional
                "NT1" => 4.80m, // Remote
                _ => 3.80m // Interstate average
            };
        }
    }
}
