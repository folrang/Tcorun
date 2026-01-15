
using System.Buffers.Binary;

namespace Common.Proto
{
    public enum MsgType : ushort { Handshake = 0, Data = 1, Ack = 2, Error = 3 }

    public readonly struct MessageHeader
    {
        public readonly ushort Magic;      // 0x4D50 ("MP")
        public readonly byte Version;    // 1
        public readonly byte Flags;      // 0 (no compression/encryption)
        public readonly ushort MsgType;
        public readonly ushort ErrorCode;
        public readonly ulong RequestId;
        public readonly uint PayloadLen;

        public MessageHeader(ushort magic, byte ver, byte flags, ushort msgType, ushort err,
                             ulong reqId, uint payloadLen)
        {
            Magic = magic; Version = ver; Flags = flags; MsgType = msgType; ErrorCode = err;
            RequestId = reqId; PayloadLen = payloadLen;
        }

        public static int Size => 20;

        public static MessageHeader Read(ReadOnlySpan<byte> span)
        {
            ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(span[0..2]);
            byte ver = span[2];
            byte flags = span[3];
            ushort msgType = BinaryPrimitives.ReadUInt16LittleEndian(span[4..6]);
            ushort err = BinaryPrimitives.ReadUInt16LittleEndian(span[6..8]);
            ulong reqId = BinaryPrimitives.ReadUInt64LittleEndian(span[8..16]);
            uint payloadLen = BinaryPrimitives.ReadUInt32LittleEndian(span[16..20]);
            return new MessageHeader(magic, ver, flags, msgType, err, reqId, payloadLen);
        }

        public void Write(Span<byte> span)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span[0..2], Magic);
            span[2] = Version;
            span[3] = Flags; // 항상 0
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..6], MsgType);
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..8], ErrorCode);
            BinaryPrimitives.WriteUInt64LittleEndian(span[8..16], RequestId);
            BinaryPrimitives.WriteUInt32LittleEndian(span[16..20], PayloadLen);
        }
    }

    public static class MessageCodec
    {
        private const ushort Magic = 0x4D50;  // 'MP'
        private const byte Version = 1;

        // Encode: plain payload -> 4B length + header + payload
        public static byte[] Encode(ulong requestId, MsgType msgType, byte[] payload, ushort errorCode = 0)
        {
            var header = new MessageHeader(Magic, Version, 0 /*Flags*/, (ushort)msgType, errorCode,
                                           requestId, (uint)payload.Length);

            var headerBuf = new byte[MessageHeader.Size];
            header.Write(headerBuf);

            int totalLen = headerBuf.Length + payload.Length;
            var frame = new byte[4 + totalLen];
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), totalLen);
            Buffer.BlockCopy(headerBuf, 0, frame, 4, headerBuf.Length);
            Buffer.BlockCopy(payload, 0, frame, 4 + headerBuf.Length, payload.Length);
            return frame;
        }

        // DecodeFrame([4B length + header + payload]) — 클라이언트용
        public static (MessageHeader header, byte[] payload) DecodeFrame(ReadOnlySpan<byte> full)
        {
            int totalLen = BinaryPrimitives.ReadInt32LittleEndian(full[..4]);
            var body = full.Slice(4, totalLen);
            return DecodeBody(body);
        }

        // DecodeBody([header + payload]) — 서버 핫패스용
        public static (MessageHeader header, byte[] payload) DecodeBody(ReadOnlySpan<byte> headerPlusPayload)
        {
            var header = MessageHeader.Read(headerPlusPayload[..MessageHeader.Size]);
            var payload = headerPlusPayload.Slice(MessageHeader.Size, (int)header.PayloadLen).ToArray();
            return (header, payload);
        }
    }
}

/*
TCP 메시지는 [4바이트 총길이] + [고정 헤더 20바이트] + [Payload] 로 구성합니다.

길이‑프레임은 TCP 스트림에서 메시지 경계를 명확히 하기 위한 표준적 해결책으로 널리 권장됩니다. 단일 Send가 단일 Receive로 매핑되지 않는 TCP의 특성을 고려해야 합니다. [stackoverflow.com], [thecodeblogger.com]
*/