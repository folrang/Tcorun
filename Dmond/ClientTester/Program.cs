using Microsoft.Extensions.Configuration;
using ClientTester.Clients;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var grpcBase = config["Endpoints:GrpcBaseAddress"] ?? "https://localhost:8443";
        var restBase = config["Endpoints:RestBaseAddress"] ?? "https://localhost:8443";
        var tcpHost  = config["Endpoints:TcpHost"] ?? "127.0.0.1";
        var tcpPort  = int.TryParse(config["Endpoints:TcpPort"], out var p) ? p : 9000;

        bool loop = true;
        while (loop)
        {
            var selection = args.FirstOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(selection))
            {
                Console.WriteLine("Select client to run: 1) gRPC  2) TCP  3) REST 9) Exit");
                selection = Console.ReadLine()?.Trim().ToLowerInvariant();
            }

            try
            {
                switch (selection)
                {
                case "1": case "grpc": await GrpcClient.RunAsync(grpcBase); break;
                case "2": case "tcp": await TcpClientRunner.RunAsync(tcpHost, tcpPort); break;
                case "3": case "rest": await RestClient.RunAsync(restBase); break;
                case "9": case "exit": loop = false; break;
                default:
                    Console.WriteLine("Usage: ClientTester [grpc|tcp|rest]");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return 2;
            }
        }
        
        return 0;
    }
}

// NuGet 패키지 'Microsoft.Extensions.Configuration' 및 관련 확장 패키지들이 필요합니다.
// Visual Studio에서 NuGet 패키지 관리자 또는 CLI를 사용하여 다음 패키지들을 설치하세요:
// - Microsoft.Extensions.Configuration
// - Microsoft.Extensions.Configuration.Json
// - Microsoft.Extensions.Configuration.EnvironmentVariables
// - Microsoft.Extensions.Configuration.CommandLine
//
// 예시 (패키지 관리자 콘솔):
// Install-Package Microsoft.Extensions.Configuration
// Install-Package Microsoft.Extensions.Configuration.Json
// Install-Package Microsoft.Extensions.Configuration.EnvironmentVariables
// Install-Package Microsoft.Extensions.Configuration.CommandLine
//
// 또는 .csproj 파일에 직접 추가:
/*
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="7.0.0" />
  </ItemGroup>
*/
