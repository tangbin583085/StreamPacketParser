using StreamPacketParser;
using StreamPacketParser.Diagnostics;
using StreamPacketParser.Validation;

PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
    header: new byte[] { 0xAA, 0x55 },
    lengthFieldOffset: 4,
    lengthFieldSize: 2,
    byteOrder: ByteOrder.BigEndian,
    lengthMode: FrameLengthMode.PayloadLength,
    fixedFrameOverhead: 8,
    minFrameLength: 8,
    maxFrameLength: 4096,
    maxBufferedBytes: 8192,
    validator: new Crc16ModbusValidator(
        dataStartOffset: 2,
        checksumOffsetFromEnd: 2,
        checksumByteOrder: ByteOrder.LittleEndian),
    payloadOffset: 6);

var parser = new PacketParser(options);
byte[] firstFrame = CreateFrame(command: 0x10, payload: [0x11, 0x22, 0x33]);
byte[] secondFrame = CreateFrame(command: 0x20, payload: [0x44, 0x55]);
byte[][] incomingChunks =
[
    [0xFF, 0x00, .. firstFrame[..4]],
    [.. firstFrame[4..], .. secondFrame[..3]],
    secondFrame[3..],
];

Console.WriteLine("StreamPacketParser.NET Console示例");
Console.WriteLine("同一条字节流会被拆成带噪声的片段、半帧和粘包片段依次送入解析器。");

for (int index = 0; index < incomingChunks.Length; index++)
{
    PacketParseResult result = parser.Append(incomingChunks[index]);
    Console.WriteLine($"Chunk {index + 1}: {Convert.ToHexString(incomingChunks[index])}");

    foreach (ParserDiagnostic diagnostic in result.Diagnostics)
    {
        Console.WriteLine($"  Diagnostic: {diagnostic.Code} - {diagnostic.Message}");
    }

    foreach (ParsedPacket packet in result.Packets)
    {
        Console.WriteLine($"  Packet: {Convert.ToHexString(packet.RawData.Span)}");
        Console.WriteLine($"  Payload: {Convert.ToHexString(packet.Payload.Span)}");
    }
}

static byte[] CreateFrame(byte command, ReadOnlySpan<byte> payload)
{
    byte[] frame = new byte[8 + payload.Length];
    frame[0] = 0xAA;
    frame[1] = 0x55;
    frame[2] = 0x01;
    frame[3] = command;
    frame[4] = (byte)(payload.Length >> 8);
    frame[5] = (byte)payload.Length;
    payload.CopyTo(frame.AsSpan(6));

    ushort crc = CalculateCrc16Modbus(frame.AsSpan(2, frame.Length - 4));
    frame[^2] = (byte)crc;
    frame[^1] = (byte)(crc >> 8);
    return frame;
}

static ushort CalculateCrc16Modbus(ReadOnlySpan<byte> data)
{
    ushort crc = 0xFFFF;
    foreach (byte value in data)
    {
        crc ^= value;
        for (int bit = 0; bit < 8; bit++)
        {
            crc = (crc & 1) != 0
                ? (ushort)((crc >> 1) ^ 0xA001)
                : (ushort)(crc >> 1);
        }
    }

    return crc;
}
