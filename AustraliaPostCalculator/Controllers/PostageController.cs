using Microsoft.AspNetCore.Mvc;
using AustraliaPostCalculator.Models;
using AustraliaPostCalculator.Services;

namespace AustraliaPostCalculator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostageController : ControllerBase
    {
        private readonly IPostageCalculator _postageCalculator;
        private readonly ILogger<PostageController> _logger;

        public PostageController(IPostageCalculator postageCalculator, ILogger<PostageController> logger)
        {
            _postageCalculator = postageCalculator;
            _logger = logger;
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<PostageResult>> CalculatePostage([FromBody] ParcelRequest request)
        {
            try
            {
                // Validate request
                var validationErrors = _postageCalculator.GetValidationErrors(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { 
                        Success = false, 
                        Errors = validationErrors,
                        Message = "Validation failed"
                    });
                }

                // Calculate postage
                var result = await _postageCalculator.CalculatePostageAsync(request);
                
                return Ok(new { 
                    Success = true, 
                    Data = result,
                    Message = "Postage calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating postage for request: {@Request}", request);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "An error occurred while calculating postage"
                });
            }
        }

        [HttpPost("calculate-domestic")]
        public async Task<ActionResult<PostageResult>> CalculateDomesticPostage([FromBody] ParcelRequest request)
        {
            try
            {
                if (request.IsInternational)
                {
                    return BadRequest(new { 
                        Success = false, 
                        Message = "This endpoint is for domestic postage only"
                    });
                }

                var validationErrors = _postageCalculator.GetValidationErrors(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { 
                        Success = false, 
                        Errors = validationErrors,
                        Message = "Validation failed"
                    });
                }

                var result = await _postageCalculator.CalculateDomesticPostageAsync(request);
                
                return Ok(new { 
                    Success = true, 
                    Data = result,
                    Message = "Domestic postage calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating domestic postage for request: {@Request}", request);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "An error occurred while calculating domestic postage"
                });
            }
        }

        [HttpPost("calculate-international")]
        public async Task<ActionResult<PostageResult>> CalculateInternationalPostage([FromBody] ParcelRequest request)
        {
            try
            {
                if (!request.IsInternational)
                {
                    return BadRequest(new { 
                        Success = false, 
                        Message = "This endpoint is for international postage only"
                    });
                }

                var validationErrors = _postageCalculator.GetValidationErrors(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { 
                        Success = false, 
                        Errors = validationErrors,
                        Message = "Validation failed"
                    });
                }

                var result = await _postageCalculator.CalculateInternationalPostageAsync(request);
                
                return Ok(new { 
                    Success = true, 
                    Data = result,
                    Message = "International postage calculated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating international postage for request: {@Request}", request);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "An error occurred while calculating international postage"
                });
            }
        }

        [HttpGet("validate")]
        public ActionResult<ValidationResult> ValidateParcel([FromQuery] ParcelRequest request)
        {
            try
            {
                var isValid = _postageCalculator.ValidateParcelDimensions(request);
                var errors = _postageCalculator.GetValidationErrors(request);

                return Ok(new ValidationResult
                {
                    IsValid = isValid && !errors.Any(),
                    Errors = errors,
                    Message = isValid && !errors.Any() ? "Parcel is valid" : "Parcel validation failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating parcel: {@Request}", request);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "An error occurred while validating the parcel"
                });
            }
        }

        [HttpGet("zones/domestic")]
        public ActionResult<Dictionary<string, string>> GetDomesticZones()
        {
            // Return sample postcode to zone mapping for reference
            var zones = new Dictionary<string, string>
            {
                { "NSW Metro (1000-2263)", "N1" },
                { "NSW Regional (2264-2999)", "N2" },
                { "VIC Metro (3000-3999, 8000-8999)", "V1" },
                { "VIC Regional (3000-3999)", "V2" },
                { "QLD Metro (4000-4999, 9000-9999)", "Q1" },
                { "QLD Regional (4000-4999)", "Q2" },
                { "SA Metro (5000-5999)", "S1" },
                { "SA Regional (5000-5999)", "S2" },
                { "WA Metro (6000-6999)", "W1" },
                { "WA Regional (6000-6999)", "W2" },
                { "TAS Metro (7000-7999)", "T1" },
                { "TAS Regional (7000-7999)", "T2" },
                { "NT (0800-0999)", "NT1" },
                { "ACT (0200-0299, 2600-2639)", "N2" }
            };

            return Ok(zones);
        }

        [HttpGet("zones/international")]
        public ActionResult<Dictionary<string, string>> GetInternationalZones()
        {
            var zones = new Dictionary<string, string>
            {
                { "New Zealand", "Zone A" },
                { "United Kingdom", "Zone B" },
                { "United States", "Zone B" },
                { "Canada", "Zone B" },
                { "China", "Zone C" },
                { "Japan", "Zone C" },
                { "Singapore", "Zone C" },
                { "India", "Zone D" },
                { "Brazil", "Zone D" },
                { "South Africa", "Zone D" }
            };

            return Ok(zones);
        }

        [HttpGet("weight-tiers")]
        public ActionResult<List<string>> GetWeightTiers()
        {
            var tiers = new List<string>
            {
                "≤500g",
                ">500g - ≤1kg",
                ">1kg - ≤3kg", 
                ">3kg - ≤5kg",
                ">5kg - ≤22kg"
            };

            return Ok(tiers);
        }

        [HttpGet("mypost-bands")]
        public ActionResult<Dictionary<int, string>> GetMyPostBands()
        {
            var bands = new Dictionary<int, string>
            {
                { 0, "No discount (Standard retail)" },
                { 1, "5% discount" },
                { 2, "10% discount" },
                { 3, "12.5% discount" },
                { 4, "15% discount" },
                { 5, "17.5% discount" }
            };

            return Ok(bands);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string Message { get; set; }
    }
}
