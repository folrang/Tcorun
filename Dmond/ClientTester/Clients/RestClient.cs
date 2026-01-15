// Clients/RestClient.cs
using System.Net.Http.Json;

namespace ClientTester.Clients
{
    public static class RestClient
    {
        public static async Task RunAsync(string baseAddress)
        {
            Console.WriteLine($"[REST] {baseAddress}");
            using var http = new HttpClient { BaseAddress = new Uri(baseAddress) };

            var recent = await http.GetStringAsync("/api/logs");
            Console.WriteLine($"[GET /api/logs] {recent}");

            var dto = new { Level="INFO", Message="ClientTester REST test", Source="ClientTester", JsonData="{\"hello\":\"world\"}", Tags="rest-test" };
            var resp = await http.PostAsJsonAsync("/api/logs", dto);
            Console.WriteLine($"[POST /api/logs] {(int)resp.StatusCode} {resp.StatusCode}");
        }
    }
}
