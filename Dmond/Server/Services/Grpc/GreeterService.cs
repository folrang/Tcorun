using Grpc.Core;

namespace Services.Grpc
{
    public class GreeterService : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
            => Task.FromResult(new HelloReply { Message = $"Hello, {request.Name}" });
    }
}

// gRPC는 HTTP/2 + TLS(HTTPS) 를 기본으로 합니다. Kestrel에서 REST와 병행 가능합니다.