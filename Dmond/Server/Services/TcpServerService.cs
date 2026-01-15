// Services/TcpServerService.cs
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Common.Proto;

namespace Services
{
    internal class BufferManager
    {
        private readonly byte[] _buffer;
        private readonly int _chunk;
        private readonly Stack<int> _free = new();
        private int _next;
        public BufferManager(int totalBytes, int chunk) { _buffer = new byte[totalBytes]; _chunk = chunk; }
        public bool SetBuffer(SocketAsyncEventArgs e)
        {
            if (_free.Count > 0) { var off = _free.Pop(); e.SetBuffer(_buffer, off, _chunk); return true; }
            if (_next + _chunk > _buffer.Length) return false;
            e.SetBuffer(_buffer, _next, _chunk); _next += _chunk; return true;
        }
        public void FreeBuffer(SocketAsyncEventArgs e) { _free.Push(e.Offset); e.SetBuffer(null!, 0, 0); }
    }

    public class TcpServerService : IHostedService
    {
        private readonly ILogger<TcpServerService> _log;
        private Socket? _listen;
        private readonly ConcurrentStack<SocketAsyncEventArgs> _pool = new();
        private BufferManager? _bm;
        private readonly SemaphoreSlim _limit;
        private const int BufferSize = 4096;
        private const int MaxConnections = 2000;
        private readonly int _port = 9000;

        public TcpServerService(ILogger<TcpServerService> log) { _log = log; _limit = new SemaphoreSlim(MaxConnections, MaxConnections); }

        public Task StartAsync(CancellationToken ct)
        {
            _bm = new BufferManager(BufferSize * MaxConnections, BufferSize);
            for (int i=0; i<MaxConnections; i++)
            {
                var saea = new SocketAsyncEventArgs();
                if (!_bm.SetBuffer(saea)) throw new OutOfMemoryException();
                saea.Completed += IO_Completed;
                _pool.Push(saea);
            }

            _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listen.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listen.Listen(1024);
            _log.LogInformation("TCP listening {Port}", _port);

            StartAccept();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct) { try { _listen?.Close(); } catch { } _listen = null; return Task.CompletedTask; }

        private void StartAccept()
        {
            var acc = new SocketAsyncEventArgs(); acc.Completed += Accept_Completed;
            if (!(_listen?.AcceptAsync(acc) ?? true)) ProcessAccept(acc);
        }
        private void Accept_Completed(object? s, SocketAsyncEventArgs e) => ProcessAccept(e);

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            var client = e.AcceptSocket; e.AcceptSocket = null;
            if (_listen != null) StartAccept();
            if (client == null) return;

            if (!_limit.Wait(0)) { SafeClose(client); return; }
            if (!_pool.TryPop(out var io)) { SafeClose(client); _limit.Release(); return; }

            io.UserToken = new Conn(client);
            if (!client.ReceiveAsync(io)) ProcessReceive(io);
        }

        private void IO_Completed(object? s, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive) ProcessReceive(e);
            else if (e.LastOperation == SocketAsyncOperation.Send) ProcessSend(e);
        }

        private class Conn
        {
            public Socket Socket { get; }
            public int Expected = -1;
            public MemoryStream Acc = new();
            public Conn(Socket s) => Socket = s;
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            var st = (Conn?)e.UserToken; var cli = st?.Socket;
            if (st is null || cli is null || e.SocketError != SocketError.Success || e.BytesTransferred == 0) { Close(e); return; }

            var span = new ReadOnlySpan<byte>(e.Buffer!, e.Offset, e.BytesTransferred);
            var pos = 0;

            while (pos < span.Length)
            {
                if (st.Expected < 0)
                {
                    if (span.Length - pos < 4) { st.Acc.Write(span[pos..].ToArray()); pos = span.Length; break; }
                    st.Expected = BitConverter.ToInt32(span.Slice(pos, 4)); pos += 4; st.Acc.SetLength(0);
                }
                var remain = st.Expected - (int)st.Acc.Length;
                var take = Math.Min(remain, span.Length - pos);
                st.Acc.Write(span.Slice(pos, take).ToArray()); pos += take;

                if (st.Acc.Length == st.Expected)
                {
                    // [header+payload] 완성
                    var headerPlusPayload = st.Acc.ToArray();
                    st.Expected = -1;

                    var respFrame = HandleMessage(headerPlusPayload);
                    e.SetBuffer(respFrame, 0, respFrame.Length);
                    if (!cli.SendAsync(e)) ProcessSend(e);
                    return;
                }
            }
            if (!cli.ReceiveAsync(e)) ProcessReceive(e);
        }

        private byte[] HandleMessage(byte[] headerPlusPayload)
        {
            try
            {
                var (hdr, plain) = MessageCodec.DecodeBody(headerPlusPayload);
                var text = Encoding.UTF8.GetString(plain);
                var result = Encoding.UTF8.GetBytes($"Echo: {text}".ToUpperInvariant());
                return MessageCodec.Encode(hdr.RequestId, MsgType.Ack, result, errorCode: 0);
            }
            catch (Exception)
            {
                var err = Encoding.UTF8.GetBytes("DECODE_ERROR");
                return MessageCodec.Encode(0, MsgType.Error, err, errorCode: 1);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            var st = (Conn?)e.UserToken; var cli = st?.Socket;
            if (cli is null) { Close(e); return; }
            _bm!.SetBuffer(e);
            if (!cli.ReceiveAsync(e)) ProcessReceive(e);
        }

        private void Close(SocketAsyncEventArgs e)
        {
            var st = (Conn?)e.UserToken; var cli = st?.Socket;
            if (cli != null) SafeClose(cli);
            st?.Acc.Dispose(); e.UserToken = null;
            try { _bm?.FreeBuffer(e); _bm?.SetBuffer(e); } catch { }
            _pool.Push(e); _limit.Release();
        }

        private static void SafeClose(Socket s) { try { s.Shutdown(SocketShutdown.Both); } catch { } try { s.Close(); } catch { } }
    }
}

// .NET의 TCP 고급 시나리오에서는 Socket/SocketAsyncEventArgs 를 통해 고성능 IOCP 기반 처리가 가능합니다. 간단한 경우엔 TcpClient/TcpListener 를 사용할 수 있으나, 저수준 제어·성능 최적화는 Socket 사용을 권장합니다