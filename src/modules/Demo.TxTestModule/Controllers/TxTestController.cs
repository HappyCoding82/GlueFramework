using Demo.TxTestModule.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Demo.TxTestModule.Controllers;

[Route("api/tx-test")]
[IgnoreAntiforgeryToken]
[ApiController]
public sealed class TxTestController : ControllerBase
{
    private readonly ITxTestService _service;

    public TxTestController(ITxTestService service)
    {
        _service = service;
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count()
    {
        var count = await _service.GetCountAsync();
        return Ok(new { count });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _service.ResetAsync();
        var count = await _service.GetCountAsync();
        return Ok(new { count });
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] TxTestCase testCase, [FromQuery] bool shouldSucceed = false)
    {
        try
        {
            await _service.ResetAsync();
            switch (testCase)
            {
                case TxTestCase.NoTransactional_SingleService_TwoWrites_Throw:
                    await _service.NoTransactional_SingleService_TwoWritesAsync(shouldSucceed);
                    break;
                case TxTestCase.Transactional_SingleService_TwoWrites_Throw:
                    await _service.Transactional_SingleService_TwoWritesAsync(shouldSucceed);
                    break;
                case TxTestCase.Transactional_CrossService_TwoWrites_Throw:
                    await _service.Transactional_CrossService_TwoWritesAsync(shouldSucceed);
                    break;
                case TxTestCase.Transactional_Nested_OuterTransactional_InnerTransactional_Throw:
                    await _service.Transactional_Nested_OuterTransactional_InnerTransactionalAsync(shouldSucceed);
                    break;
                case TxTestCase.Transactional_CrossService_BothTransactional_Throw:
                    await _service.Transactional_CrossService_BothTransactionalAsync(shouldSucceed);
                    break;
                default:
                    return BadRequest(new { error = "Unknown testCase" });
            }

            var count = await _service.GetCountAsync();
            return Ok(new { ok = true, testCase, shouldSucceed, count });
        }
        catch (Exception ex)
        {
            var count = await _service.GetCountAsync();
            return Ok(new { ok = false, testCase, shouldSucceed, count, error = ex.Message, type = ex.GetType().Name });
        }
    }
}
