using AustraliaPostCalculator.Models;

namespace AustraliaPostCalculator.Services
{
    public class ZoneService : IZoneService
    {
        private readonly Dictionary<string, string> _postcodeZones;
        private readonly Dictionary<string, string> _countryZones;

        public ZoneService()
        {
            _postcodeZones = InitializeDomesticZones();
            _countryZones = InitializeInternationalZones();
        }

        public ZoneInfo GetDomesticZone(string fromPostcode, string toPostcode)
        {
            if (!IsValidAustralianPostcode(fromPostcode) || !IsValidAustralianPostcode(toPostcode))
            {
                throw new ArgumentException("Invalid Australian postcode");
            }

            var fromState = GetStateFromPostcode(fromPostcode);
            var toState = GetStateFromPostcode(toPostcode);
            
            var fromZone = _postcodeZones.GetValueOrDefault(fromPostcode, GetDefaultZone(fromState));
            var toZone = _postcodeZones.GetValueOrDefault(toPostcode, GetDefaultZone(toState));

            bool isSameState = fromState == toState;
            bool isRemote = fromZone.Contains("R") || toZone.Contains("R");

            return new ZoneInfo
            {
                Zone = toZone,
                Description = GetZoneDescription(toZone, isSameState),
                IsSameState = isSameState,
                IsRemote = isRemote
            };
        }

        public string GetInternationalZone(string country)
        {
            return _countryZones.GetValueOrDefault(country.ToUpper(), "Zone D");
        }

        public bool IsValidPostcode(string postcode)
        {
            return IsValidAustralianPostcode(postcode);
        }

        public bool IsValidAustralianPostcode(string postcode)
        {
            if (string.IsNullOrEmpty(postcode) || postcode.Length != 4)
                return false;
            
            return int.TryParse(postcode, out _);
        }

        private Dictionary<string, string> InitializeDomesticZones()
        {
            return new Dictionary<string, string>
            {
                // NSW
                { "1000", "N1" }, { "2000", "N1" }, { "2001", "N1" },
                { "2264", "N2" }, { "2485", "N2/Q1" }, { "2640", "N2/V2" },
                
                // VIC
                { "3000", "V1" }, { "3001", "V1" }, { "8000", "V1" },
                { "3121", "V2" }, { "3825", "V2" },
                
                // QLD
                { "4000", "Q1" }, { "4001", "Q1" }, { "9000", "Q1" },
                { "4551", "Q2" }, { "4825", "Q2" },
                
                // SA
                { "5000", "S1" }, { "5001", "S1" },
                { "5254", "S2" }, { "5701", "S2" },
                
                // WA
                { "6000", "W1" }, { "6001", "W1" },
                { "6230", "W2" }, { "6701", "W2" },
                
                // TAS
                { "7000", "T1" }, { "7001", "T1" },
                { "7320", "T2" },
                
                // NT (all remote)
                { "0800", "NT1" }, { "0830", "NT1" }, { "0900", "NT1" },
                
                // ACT (treated as NSW)
                { "2600", "N2" }, { "2601", "N2" }, { "0200", "N2" }
            };
        }

        private Dictionary<string, string> InitializeInternationalZones()
        {
            return new Dictionary<string, string>
            {
                { "NEW ZEALAND", "Zone A" },
                { "UNITED KINGDOM", "Zone B" },
                { "UNITED STATES", "Zone B" },
                { "CANADA", "Zone B" },
                { "CHINA", "Zone C" },
                { "JAPAN", "Zone C" },
                { "SINGAPORE", "Zone C" },
                { "INDIA", "Zone D" },
                { "BRAZIL", "Zone D" },
                { "SOUTH AFRICA", "Zone D" }
            };
        }

        private string GetStateFromPostcode(string postcode)
        {
            if (string.IsNullOrEmpty(postcode) || postcode.Length != 4)
                return "Unknown";

            int code = int.Parse(postcode);

            if (code >= 1000 && code <= 2299) return "NSW";
            if (code >= 2600 && code <= 2619) return "ACT";
            if (code >= 2900 && code <= 2999) return "ACT";
            if (code >= 0200 && code <= 0299) return "ACT";
            if (code >= 3000 && code <= 3999) return "VIC";
            if (code >= 8000 && code <= 8999) return "VIC";
            if (code >= 4000 && code <= 4999) return "QLD";
            if (code >= 9000 && code <= 9999) return "QLD";
            if (code >= 5000 && code <= 5999) return "SA";
            if (code >= 6000 && code <= 6999) return "WA";
            if (code >= 7000 && code <= 7999) return "TAS";
            if (code >= 0800 && code <= 0999) return "NT";

            return "Unknown";
        }

        private string GetDefaultZone(string state)
        {
            return state switch
            {
                "NSW" => "N1",
                "ACT" => "N2",
                "VIC" => "V1",
                "QLD" => "Q1",
                "SA" => "S1",
                "WA" => "W1",
                "TAS" => "T1",
                "NT" => "NT1",
                _ => "N1"
            };
        }

        private string GetZoneDescription(string zone, bool isSameState)
        {
            if (isSameState)
            {
                return zone switch
                {
                    "N1" or "V1" or "Q1" or "S1" or "W1" or "T1" => "Same State - Metro",
                    "N2" or "V2" or "Q2" or "S2" or "W2" or "T2" => "Same State - Regional",
                    "NT1" => "Same State - Remote",
                    _ => "Same State"
                };
            }

            return zone switch
            {
                "N1" or "V1" or "Q1" or "S1" or "W1" or "T1" => "Interstate - Metro",
                "N2" or "V2" or "Q2" or "S2" or "W2" or "T2" => "Interstate - Regional", 
                "NT1" => "Interstate - Remote",
                _ => "Interstate"
            };
        }
    }
}
