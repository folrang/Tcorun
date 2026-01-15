// Clients/GrpcClient.cs
using Grpc.Net.Client;
using Services.Grpc;

namespace ClientTester.Clients
{
    public static class GrpcClient
    {
        public static async Task RunAsync(string baseAddress)
        {
            Console.WriteLine($"[gRPC] {baseAddress}");
            using var channel = GrpcChannel.ForAddress(baseAddress);
            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(new HelloRequest { Name = "ClientTester" });
            Console.WriteLine($"[gRPC] {reply.Message}");
        }
    }
}
