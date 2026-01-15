// Clients/TcpClient.cs
using System.Net.Sockets;
using System.Text;
using Common.Proto;

namespace ClientTester.Clients
{
    public static class TcpClientRunner
    {
        public static async Task RunAsync(string host, int port)
        {
            Console.WriteLine($"[TCP] {host}:{port}");
            using var client = new TcpClient(); await client.ConnectAsync(host, port);
            using var stream = client.GetStream();

            Console.WriteLine("Type lines to send. Ctrl+C to exit.");
            while (true)
            {
                var line = Console.ReadLine(); if (line is null) break;
                var frame = MessageCodec.Encode((ulong)Random.Shared.NextInt64(), MsgType.Data, Encoding.UTF8.GetBytes(line));
                await stream.WriteAsync(frame);

                var lenBuf = new byte[4]; await ReadExactlyAsync(stream, lenBuf, 0, 4);
                int totalLen = BitConverter.ToInt32(lenBuf, 0);
                var bodyBuf = new byte[totalLen]; await ReadExactlyAsync(stream, bodyBuf, 0, totalLen);

                var full = new byte[4 + totalLen]; BitConverter.GetBytes(totalLen).CopyTo(full, 0); Buffer.BlockCopy(bodyBuf, 0, full, 4, totalLen);
                var (hdr, plain) = MessageCodec.DecodeFrame(full);
                Console.WriteLine($"[TCP {hdr.MsgType}] {Encoding.UTF8.GetString(plain)}");
            }
        }
        private static async Task ReadExactlyAsync(NetworkStream s, byte[] buf, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                var read = await s.ReadAsync(buf.AsMemory(offset + total, count - total));
                if (read == 0) throw new IOException("Remote closed"); total += read;
            }
        }
    }
}
