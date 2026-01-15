using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
//using MySql.Data.MySqlClient;
using Services;             // TcpServerService
using Services.Grpc;        // GreeterService
using StackExchange.Redis;
using System.Net;

namespace Dmond
{
    // Kestrel 멀티 엔드포인트 & 서비스 등록
    // HTTP/3는 HttpProtocols.Http1AndHttp2AndHttp3로 활성화하며 HTTPS(= TLS 1.3) 가 필수입니다.
    // Windows 11/Server 2022 또는 Linux에서 MsQuic/libmsquic 환경을 준비해야 하며,
    // 엔드포인트/제한/옵션들은 Kestrel에서 코드/설정으로 구성할 수 있습니다.

    // Windows 11에서는 QUIC/MsQuic가.NET 런타임에 포함되어 있어, UseHttps + HttpProtocols.Http1AndHttp2AndHttp3로 바로 HTTP/3 활성화가 가능합니다.

    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // --- Kestrel 멀티 엔드포인트 ---
            // 8080: HTTP/1.1 (REST)
            // 80443: HTTPS(HTTP/1.1 + HTTP/2 + HTTP/3) — QUIC(MsQuic) 필요
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, 8080, o =>
                {
                    o.Protocols = HttpProtocols.Http1;
                });

                // HTTPS: HTTP/1.1 + HTTP/2 + HTTP/3
                options.Listen(IPAddress.Any, 8443, o =>
                {
                    o.UseHttps("certs/server.pfx", "pfx_password"); // 아래 3)에서 dev cert 내보내기
                    o.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                });

                // Kestrel은 HTTP/ 1.1·HTTP / 2·HTTP / 3 동시 활성화가 가능하며, HTTP / 3는 QUIC(UDP) 기반으로 TLS 1.3 + UseHttps가 필수입니다.Windows 11 / Server 2022에서는 MsQuic가 .NET 런타임에 동봉되어 별도 설치 없이 동작합니다.
                // gRPC는 ASP.NET Core에서 HTTPS(HTTP / 2) 를 권장하며, 템플릿/ 문서도 해당 구성을 기본으로 안내합니다.
            });

            // --- MVC(REST) + gRPC ---
            builder.Services.AddControllers();
            builder.Services.AddGrpc();

            // --- DB 팩토리 (둘 중 실제 사용할 것을 중심으로) ---
            builder.Services.AddSingleton<Func<SqlConnection>>(_ =>
                () => new SqlConnection(builder.Configuration.GetConnectionString("dmond_log")));
            //builder.Services.AddSingleton<Func<MySqlConnection>>(_ =>
            //    () => new MySqlConnection(builder.Configuration.GetConnectionString("Mysql")));

            // --- Dapper Repo/Redis 래퍼 ---
            builder.Services.AddSingleton<Data.Db>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var cs = builder.Configuration.GetConnectionString("Redis");
                return ConnectionMultiplexer.Connect(cs!);
            });
            builder.Services.AddSingleton<Data.RedisClient>();

            // --- 고성능 TCP 서버 (SocketAsyncEventArgs) ---
            builder.Services.AddHostedService<TcpServerService>();

            var app = builder.Build();

            app.MapControllers();                 // REST
            app.MapGrpcService<GreeterService>(); // gRPC

            // TEST
            //{
            //    // http://localhost:8080/api/logs 를 테스트 목적으로 호출하는 코드

            //    using var httpClient = new HttpClient();
            //    var response = httpClient.GetAsync("http://localhost:8080/api/logs").Result;
            //    var content = response.Content.ReadAsStringAsync().Result;
            //    Console.WriteLine("Test GET /api/logs response:");
            //    Console.WriteLine(content);
            //}

            app.Run();

        }
    }
}

/*

 * 3) 개발용 인증서(Windows 11)
Windows에서는 dotnet dev-certs https --trust 가 자동 신뢰까지 수행됩니다. 이후 PFX 내보내기로 Kestrel에서 참조하세요. [learn.microsoft.com]

PowerShell
# PowerShell에서 실행
dotnet dev-certs https --clean      # (옵션) 기존 개발용 인증서 삭제
dotnet dev-certs https --trust      # 개발용 인증서 생성+신뢰
dotnet dev-certs https --export-path ./certs/server.pfx --password pfx_password


dotnet dev-certs는 로컬 HTTPS 개발을 위한 공식 CLI입니다. Windows/macOS는 --trust가 사용자 루트 스토어에 자동 등록되며, PFX로 내보내 Kestrel에서 바로 사용할 수 있습니다. [learn.microsoft.com]

 */