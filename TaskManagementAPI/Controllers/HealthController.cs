using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using TaskManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    // 最简单的存活检查（Liveness）
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    // 就绪检查（Readiness）- 检查数据库和 Redis
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(
        [FromServices] AppDbContext dbContext,
        [FromServices] IConnectionMultiplexer redis)
    {
        var status = new { db = "ok", redis = "ok" };

        // 检查数据库
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
        }
        catch (Exception ex)
        {
            return StatusCode(503, $"数据库不可用: {ex.Message}");
        }

        // 检查 Redis
        try
        {
            await redis.GetDatabase().PingAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(503, $"Redis 不可用: {ex.Message}");
        }

        return Ok(status);
    }
}