using Grpc.Core;
using Services.Python;
using System.Text.Json;

namespace Services.Grpc;

public class PythonService : Python.PythonBase
{
    private readonly PythonExecutorService _pythonService;
    private readonly ILogger<PythonService> _logger;

    public PythonService(PythonExecutorService pythonService, ILogger<PythonService> logger)
    {
        _pythonService = pythonService;
        _logger = logger;
    }

    public override async Task<ExecuteScriptResponse> ExecuteScript(
        ExecuteScriptRequest request,
        ServerCallContext context)
    {
        try
        {
            var parameters = request.Parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value);

            var result = await _pythonService.ExecuteScriptAsync(
                request.ScriptName,
                parameters,
                request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 300,
                context.CancellationToken);

            return new ExecuteScriptResponse
            {
                Success = result.Success,
                ExitCode = result.ExitCode,
                Output = result.Output,
                Error = result.Error,
                ExecutionTimeMs = result.ExecutionTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteScript gRPC");
            return new ExecuteScriptResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<GenerateChartResponse> GenerateChart(
        GenerateChartRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!DateTime.TryParse(request.StartDate, out var startDate))
            {
                return new GenerateChartResponse
                {
                    Success = false,
                    Error = "Invalid start_date format. Use yyyy-MM-dd"
                };
            }

            if (!DateTime.TryParse(request.EndDate, out var endDate))
            {
                return new GenerateChartResponse
                {
                    Success = false,
                    Error = "Invalid end_date format. Use yyyy-MM-dd"
                };
            }

            var result = await _pythonService.GenerateChartAsync(
                request.Symbol,
                startDate,
                endDate,
                request.ChartType ?? "candlestick",
                context.CancellationToken);

            // 차트 경로 추출
            var chartPath = ExtractChartPath(result.Output);

            return new GenerateChartResponse
            {
                Success = result.Success,
                ChartPath = chartPath ?? result.Output,
                Error = result.Error,
                ExecutionTimeMs = result.ExecutionTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateChart gRPC");
            return new GenerateChartResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<AnalyzeResponse> AnalyzeTechnicalIndicators(
        AnalyzeRequest request,
        ServerCallContext context)
    {
        try
        {
            var result = await _pythonService.AnalyzeTechnicalIndicatorsAsync(
                request.Symbol,
                request.Indicators.ToArray(),
                context.CancellationToken);

            return new AnalyzeResponse
            {
                Success = result.Success,
                AnalysisJson = result.Output,
                Error = result.Error,
                ExecutionTimeMs = result.ExecutionTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeTechnicalIndicators gRPC");
            return new AnalyzeResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<FetchTradingDataResponse> FetchTradingData(
        FetchTradingDataRequest request,
        ServerCallContext context)
    {
        try
        {
            var result = await _pythonService.FetchTradingDataAsync(
                request.Exchange,
                request.Symbol,
                request.Timeframe ?? "1h",
                request.Limit > 0 ? request.Limit : 100,
                context.CancellationToken);

            return new FetchTradingDataResponse
            {
                Success = result.Success,
                DataJson = result.Output,
                Error = result.Error,
                ExecutionTimeMs = result.ExecutionTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchTradingData gRPC");
            return new FetchTradingDataResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string? ExtractChartPath(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;

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