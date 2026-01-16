using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Services.Python;

/// <summary>
/// Python 스크립트 실행을 담당하는 서비스
/// </summary>
public class PythonExecutorService
{
    private readonly ILogger<PythonExecutorService> _logger;
    private readonly IConfiguration _config;
    private readonly string _pythonPath;
    private readonly string _scriptsPath;

    public PythonExecutorService(ILogger<PythonExecutorService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _pythonPath = config["Python:ExecutablePath"] ?? "python";
        _scriptsPath = config["Python:ScriptsPath"] ?? "scripts";
        
        EnsureScriptsDirectory();
    }

    private void EnsureScriptsDirectory()
    {
        if (!Directory.Exists(_scriptsPath))
        {
            Directory.CreateDirectory(_scriptsPath);
            _logger.LogInformation("Created scripts directory: {Path}", _scriptsPath);
        }
    }

    /// <summary>
    /// Python 스크립트를 실행하고 결과를 반환
    /// </summary>
    public async Task<PythonExecutionResult> ExecuteScriptAsync(
        string scriptName, 
        Dictionary<string, object>? parameters = null,
        int timeoutSeconds = 300,
        CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(_scriptsPath, scriptName);
        
        if (!File.Exists(scriptPath))
        {
            return new PythonExecutionResult
            {
                Success = false,
                Error = $"Script not found: {scriptPath}"
            };
        }

        var startTime = DateTime.UtcNow;
        var args = BuildArguments(scriptPath, parameters);

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _scriptsPath
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            _logger.LogInformation("Executing Python: {FileName} {Arguments}", _pythonPath, args);
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var executionTime = DateTime.UtcNow - startTime;
            var result = new PythonExecutionResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = output.ToString(),
                Error = error.ToString(),
                ExecutionTimeMs = (long)executionTime.TotalMilliseconds
            };

            if (result.Success)
            {
                _logger.LogInformation("Python script completed successfully in {Ms}ms", result.ExecutionTimeMs);
            }
            else
            {
                _logger.LogError("Python script failed with exit code {Code}: {Error}", 
                    result.ExitCode, result.Error);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Python script execution timeout or cancelled: {Script}", scriptName);
            return new PythonExecutionResult
            {
                Success = false,
                Error = "Script execution timeout or cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Python script: {Script}", scriptName);
            return new PythonExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string BuildArguments(string scriptPath, Dictionary<string, object>? parameters)
    {
        var args = new StringBuilder($"\"{scriptPath}\"");
        
        if (parameters != null && parameters.Any())
        {
            // JSON으로 직렬화하여 전달
            var json = JsonSerializer.Serialize(parameters);
            args.Append($" --params \"{json.Replace("\"", "\\\"")}\"");
        }

        return args.ToString();
    }

    /// <summary>
    /// 차트 생성 (matplotlib)
    /// </summary>
    public async Task<PythonExecutionResult> GenerateChartAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string chartType = "candlestick",
        CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["start_date"] = startDate.ToString("yyyy-MM-dd"),
            ["end_date"] = endDate.ToString("yyyy-MM-dd"),
            ["chart_type"] = chartType,
            ["output_path"] = $"output/{symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png"
        };

        return await ExecuteScriptAsync("chart_generator.py", parameters, ct: ct);
    }

    /// <summary>
    /// 기술적 분석 수행
    /// </summary>
    public async Task<PythonExecutionResult> AnalyzeTechnicalIndicatorsAsync(
        string symbol,
        string[] indicators,
        CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["indicators"] = indicators
        };

        return await ExecuteScriptAsync("technical_analysis.py", parameters, ct: ct);
    }

    /// <summary>
    /// 실시간 거래 데이터 가져오기
    /// </summary>
    public async Task<PythonExecutionResult> FetchTradingDataAsync(
        string exchange,
        string symbol,
        string timeframe = "1h",
        int limit = 100,
        CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["exchange"] = exchange,
            ["symbol"] = symbol,
            ["timeframe"] = timeframe,
            ["limit"] = limit
        };

        return await ExecuteScriptAsync("fetch_trading_data.py", parameters, ct: ct);
    }
}

public class PythonExecutionResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    
    public T? ParseOutput<T>()
    {
        if (string.IsNullOrWhiteSpace(Output)) return default;
        
        try
        {
            return JsonSerializer.Deserialize<T>(Output);
        }
        catch
        {
            return default;
        }
    }
}