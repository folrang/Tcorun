using Microsoft.AspNetCore.Mvc;
using Services.Python;

namespace Services.Http;

[ApiController]
[Route("api/python")]
public class PythonController : ControllerBase
{
    private readonly PythonExecutorService _pythonService;
    private readonly ILogger<PythonController> _logger;

    public PythonController(PythonExecutorService pythonService, ILogger<PythonController> logger)
    {
        _pythonService = pythonService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/python/execute - 임의의 스크립트 실행
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteScript(
        [FromBody] ExecuteScriptRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pythonService.ExecuteScriptAsync(
                request.ScriptName,
                request.Parameters,
                request.TimeoutSeconds ?? 300,
                ct);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error, output = result.Output });
            }

            return Ok(new
            {
                success = true,
                output = result.Output,
                executionTimeMs = result.ExecutionTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteScript");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/python/chart - 차트 생성
    /// </summary>
    [HttpPost("chart")]
    public async Task<IActionResult> GenerateChart(
        [FromBody] GenerateChartRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pythonService.GenerateChartAsync(
                request.Symbol,
                request.StartDate,
                request.EndDate,
                request.ChartType ?? "candlestick",
                ct);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            // 차트 파일 경로 추출
            var outputPath = ExtractOutputPath(result.Output);
            
            return Ok(new
            {
                success = true,
                chartPath = outputPath,
                executionTimeMs = result.ExecutionTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateChart");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/python/chart/{filename} - 생성된 차트 이미지 반환
    /// </summary>
    [HttpGet("chart/{filename}")]
    public IActionResult GetChart(string filename)
    {
        var filePath = Path.Combine("scripts", "output", filename);
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Chart not found" });
        }

        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// POST /api/python/analyze - 기술적 분석
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeTechnicalIndicators(
        [FromBody] AnalyzeRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _pythonService.AnalyzeTechnicalIndicatorsAsync(
                request.Symbol,
                request.Indicators,
                ct);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new
            {
                success = true,
                analysis = result.Output,
                executionTimeMs = result.ExecutionTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeTechnicalIndicators");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/python/trading-data - 거래 데이터 가져오기
    /// </summary>
    [HttpGet("trading-data")]
    public async Task<IActionResult> FetchTradingData(
        [FromQuery] string exchange,
        [FromQuery] string symbol,
        [FromQuery] string timeframe = "1h",
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _pythonService.FetchTradingDataAsync(
                exchange,
                symbol,
                timeframe,
                limit,
                ct);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new
            {
                success = true,
                data = result.Output,
                executionTimeMs = result.ExecutionTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchTradingData");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string? ExtractOutputPath(string output)
    {
        // "Chart saved: output/..." 형식에서 경로 추출
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Chart saved:") || line.Contains("output/"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"output/[\w\-\.]+\.png");
                if (match.Success)
                {
                    return match.Value;
                }
            }
        }
        return null;
    }
}

// DTO 모델
public record ExecuteScriptRequest(
    string ScriptName,
    Dictionary<string, object>? Parameters = null,
    int? TimeoutSeconds = null);

public record GenerateChartRequest(
    string Symbol,
    DateTime StartDate,
    DateTime EndDate,
    string? ChartType = null);

public record AnalyzeRequest(
    string Symbol,
    string[] Indicators);