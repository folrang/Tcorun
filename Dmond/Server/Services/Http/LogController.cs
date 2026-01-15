// Services/Http/LogController.cs
using Microsoft.AspNetCore.Mvc;
using Data;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly Db _db;
    private readonly Data.RedisClient _redis;
    public LogController(Db db, Data.RedisClient redis) { _db = db; _redis = redis; }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var cached = await _redis.GetAsync("logs:recent");
            if (cached is not null) return Content(cached, "application/json");
            var rows = await _db.QueryRecentLogsAsync(ct: ct);
            var json = System.Text.Json.JsonSerializer.Serialize(rows);
            await _redis.SetAsync("logs:recent", json, TimeSpan.FromSeconds(30));
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LogDto dto, CancellationToken ct)
    {
        try
        {
            var id = await _db.InsertLogAsync(dto.Level, dto.Message, dto.Source, dto.JsonData, dto.RequestId, dto.ErrorCode, HttpContext.Connection.RemoteIpAddress?.ToString(), dto.Tags, ct);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
public record LogDto(string Level, string Message, string? Source, string? JsonData, Guid? RequestId, int? ErrorCode, string? Tags);
