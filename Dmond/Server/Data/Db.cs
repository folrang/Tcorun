using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Data
{
    public class Db
    {
        private readonly Func<SqlConnection> _connFactory;
        public Db(Func<SqlConnection> connFactory) => _connFactory = connFactory;

        public async Task<long> InsertLogAsync(
            string level, string message, string? source = null, string? jsonData = null,
            Guid? requestId = null, int? errorCode = null, string? clientIp = null, string? tags = null,
            CancellationToken ct = default)
        {
            const string sql = @"
INSERT INTO dbo.Logs(Level, Source, Message, JsonData, RequestId, ErrorCode, ClientIp, Tags)
VALUES (@Level, @Source, @Message, @JsonData, @RequestId, @ErrorCode, @ClientIp, @Tags);
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            using var conn = _connFactory();
            await conn.OpenAsync(ct);
            return await conn.ExecuteScalarAsync<long>(sql, new
            {
                Level = level,
                Source = source,
                Message = message,
                JsonData = jsonData,
                RequestId = requestId,
                ErrorCode = errorCode,
                ClientIp = clientIp,
                Tags = tags
            });
        }

        public async Task<IEnumerable<dynamic>> QueryRecentLogsAsync(int top = 100, CancellationToken ct = default)
        {
            using var conn = _connFactory();
            await conn.OpenAsync(ct);
            return await conn.QueryAsync($"SELECT TOP {top} * FROM dbo.Logs ORDER BY CreatedUtc DESC");
        }
    }
}
