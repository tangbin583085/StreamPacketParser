using StreamPacketParser.Diagnostics;

namespace StreamPacketParser.Tests;

public sealed class PacketParserCoreTests
{
    [Fact]
    public void Append_CompleteFrame_ReturnsOnePacket()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x11, 0x22, 0x33]);

        PacketParseResult result = parser.Append(frame);

        ParsedPacket packet = Assert.Single(result.Packets);
        Assert.Equal(frame, packet.RawData.ToArray());
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, packet.Payload.ToArray());
        Assert.Equal(frame.Length, packet.FrameLength);
        Assert.Equal(0, parser.BufferedByteCount);
    }

    [Fact]
    public void Append_FrameSplitAcrossTwoCalls_WaitsForCompletion()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x11, 0x22]);

        PacketParseResult first = parser.Append(frame[..4]);
        int bufferedAfterFirstAppend = parser.BufferedByteCount;
        PacketParseResult second = parser.Append(frame[4..]);

        Assert.Empty(first.Packets);
        Assert.Empty(first.Diagnostics);
        Assert.Equal(4, bufferedAfterFirstAppend);
        Assert.Single(second.Packets);
        Assert.Equal(frame, second.Packets[0].RawData.ToArray());
    }

    [Fact]
    public void Append_FrameOneByteAtATime_ReturnsSameFrame()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x01, 0x02, 0x03, 0x04]);
        List<ParsedPacket> packets = [];

        foreach (byte value in frame)
        {
            packets.AddRange(parser.Append([value]).Packets);
        }

        ParsedPacket packet = Assert.Single(packets);
        Assert.Equal(frame, packet.RawData.ToArray());
    }

    [Fact]
    public void Append_ConcatenatedFrames_ReturnsAllPacketsInOrder()
    {
        PacketParser parser = CreateParser();
        byte[] first = CreatePayloadLengthFrame([0x10]);
        byte[] second = CreatePayloadLengthFrame([0x20, 0x21]);
        byte[] third = CreatePayloadLengthFrame([]);

        PacketParseResult result = parser.Append([.. first, .. second, .. third]);

        Assert.Equal(3, result.Packets.Count);
        Assert.Equal(first, result.Packets[0].RawData.ToArray());
        Assert.Equal(second, result.Packets[1].RawData.ToArray());
        Assert.Equal(third, result.Packets[2].RawData.ToArray());
    }

    [Fact]
    public void Append_PartialFrameThenRemainderAndAnotherFrame_ReturnsBothPackets()
    {
        PacketParser parser = CreateParser();
        byte[] first = CreatePayloadLengthFrame([0x31, 0x32]);
        byte[] second = CreatePayloadLengthFrame([0x41]);

        PacketParseResult incomplete = parser.Append(first[..5]);
        PacketParseResult completed = parser.Append([.. first[5..], .. second]);

        Assert.Empty(incomplete.Packets);
        Assert.Equal(2, completed.Packets.Count);
        Assert.Equal(first, completed.Packets[0].RawData.ToArray());
        Assert.Equal(second, completed.Packets[1].RawData.ToArray());
    }

    [Fact]
    public void Append_NoiseBeforeHeader_DiscardsNoiseAndReportsDiagnostic()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x55]);

        PacketParseResult result = parser.Append([0xFF, 0x00, 0x12, .. frame]);

        Assert.Single(result.Packets);
        ParserDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ParserDiagnosticCode.NoiseDiscarded, diagnostic.Code);
        Assert.Equal(3, diagnostic.DiscardedByteCount);
    }

    [Fact]
    public void Append_NoiseEndingWithPartialHeader_RetainsHeaderPrefix()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x66]);

        PacketParseResult first = parser.Append([0xFF, 0x00, 0xAA]);
        int bufferedAfterFirstAppend = parser.BufferedByteCount;
        PacketParseResult second = parser.Append(frame[1..]);

        Assert.Empty(first.Packets);
        Assert.Equal(1, bufferedAfterFirstAppend);
        Assert.Single(second.Packets);
        Assert.Equal(frame, second.Packets[0].RawData.ToArray());
    }

    [Fact]
    public void Append_LengthAboveMaximum_RecoversToFollowingFrame()
    {
        PacketParser parser = CreateParser();
        byte[] invalidPrefix = [0xAA, 0x55, 0x01, 0x10, 0x01, 0x00];
        byte[] validFrame = CreatePayloadLengthFrame([0x77]);

        PacketParseResult result = parser.Append([.. invalidPrefix, .. validFrame]);

        ParsedPacket packet = Assert.Single(result.Packets);
        Assert.Equal(validFrame, packet.RawData.ToArray());
        Assert.Contains(result.Diagnostics, item => item.Code == ParserDiagnosticCode.InvalidFrameLength);
    }

    [Fact]
    public void Append_LengthBelowMinimum_RecoversToFollowingFrame()
    {
        PacketParser parser = CreateParser(minFrameLength: 10);
        byte[] invalidPrefix = [0xAA, 0x55, 0x01, 0x10, 0x00, 0x00];
        byte[] validFrame = CreatePayloadLengthFrame([0x01, 0x02]);

        PacketParseResult result = parser.Append([.. invalidPrefix, .. validFrame]);

        Assert.Single(result.Packets);
        Assert.Contains(result.Diagnostics, item => item.Code == ParserDiagnosticCode.InvalidFrameLength);
    }

    [Fact]
    public void Append_LittleEndianLengthField_ParsesFrame()
    {
        PacketParser parser = CreateParser(ByteOrder.LittleEndian);
        byte[] frame = CreatePayloadLengthFrame([0x01, 0x02, 0x03], ByteOrder.LittleEndian);

        PacketParseResult result = parser.Append(frame);

        Assert.Single(result.Packets);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result.Packets[0].Payload.ToArray());
    }

    [Fact]
    public void Append_TotalFrameLengthMode_UsesEncodedTotalLength()
    {
        PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 2,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.TotalFrameLength,
            fixedFrameOverhead: 0,
            minFrameLength: 4,
            maxFrameLength: 64,
            maxBufferedBytes: 128);
        var parser = new PacketParser(options);
        byte[] frame = [0xAA, 0x55, 0x00, 0x06, 0x11, 0x22];

        PacketParseResult result = parser.Append(frame);

        Assert.Equal(frame, Assert.Single(result.Packets).RawData.ToArray());
    }

    [Fact]
    public void Append_EmptyInput_ReturnsEmptyResult()
    {
        PacketParser parser = CreateParser();

        PacketParseResult result = parser.Append(ReadOnlySpan<byte>.Empty);

        Assert.Empty(result.Packets);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Reset_ClearsIncompleteInputAndCanBeRepeated()
    {
        PacketParser parser = CreateParser();
        byte[] frame = CreatePayloadLengthFrame([0x11, 0x22]);

        parser.Append(frame[..4]);
        Assert.Equal(4, parser.BufferedByteCount);

        parser.Reset();
        parser.Reset();

        Assert.Equal(0, parser.BufferedByteCount);
        Assert.Single(parser.Append(frame).Packets);
    }

    [Fact]
    public void Append_LargeNoiseInput_KeepsBufferBounded()
    {
        PacketParser parser = CreateParser(maxBufferedBytes: 64);
        byte[] noise = Enumerable.Repeat((byte)0xFF, 10_000).ToArray();

        PacketParseResult result = parser.Append(noise);

        Assert.Empty(result.Packets);
        Assert.InRange(parser.BufferedByteCount, 0, 64);
        Assert.Contains(result.Diagnostics, item => item.Code == ParserDiagnosticCode.NoiseDiscarded);
    }

    [Fact]
    public void Append_MaximumAllowedFrame_ParsesSuccessfully()
    {
        PacketParser parser = CreateParser();
        byte[] payload = Enumerable.Range(0, 56).Select(value => (byte)value).ToArray();
        byte[] frame = CreatePayloadLengthFrame(payload);

        PacketParseResult result = parser.Append(frame);

        Assert.Equal(64, Assert.Single(result.Packets).FrameLength);
    }

    [Fact]
    public void ParsedPacket_RemainsStableAfterLaterAppends()
    {
        PacketParser parser = CreateParser();
        byte[] firstFrame = CreatePayloadLengthFrame([0x11, 0x12]);
        byte[] secondFrame = CreatePayloadLengthFrame([0x21, 0x22]);

        ParsedPacket firstPacket = Assert.Single(parser.Append(firstFrame).Packets);
        parser.Append(secondFrame);

        Assert.Equal(firstFrame, firstPacket.RawData.ToArray());
        Assert.Equal(new byte[] { 0x11, 0x12 }, firstPacket.Payload.ToArray());
    }

    [Fact]
    public void Append_DeterministicRandomChunks_ReturnsSameFrames()
    {
        byte[][] expectedFrames =
        [
            CreatePayloadLengthFrame([0x01]),
            CreatePayloadLengthFrame([0x02, 0x03]),
            CreatePayloadLengthFrame([0x04, 0x05, 0x06]),
        ];
        byte[] stream = [.. expectedFrames[0], .. expectedFrames[1], .. expectedFrames[2]];

        for (int iteration = 0; iteration < 50; iteration++)
        {
            var random = new Random(0x5EED + iteration);
            PacketParser parser = CreateParser();
            List<ParsedPacket> actualPackets = [];

            int offset = 0;
            while (offset < stream.Length)
            {
                int count = Math.Min(random.Next(1, 8), stream.Length - offset);
                actualPackets.AddRange(parser.Append(stream.AsSpan(offset, count)).Packets);
                offset += count;
            }

            Assert.Equal(expectedFrames.Length, actualPackets.Count);
            for (int index = 0; index < expectedFrames.Length; index++)
            {
                Assert.Equal(expectedFrames[index], actualPackets[index].RawData.ToArray());
            }
        }
    }

    private static PacketParser CreateParser(
        ByteOrder byteOrder = ByteOrder.BigEndian,
        int minFrameLength = 8,
        int maxBufferedBytes = 128)
    {
        PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: byteOrder,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: minFrameLength,
            maxFrameLength: 64,
            maxBufferedBytes: maxBufferedBytes,
            payloadOffset: 6);

        return new PacketParser(options);
    }

    private static byte[] CreatePayloadLengthFrame(
        ReadOnlySpan<byte> payload,
        ByteOrder byteOrder = ByteOrder.BigEndian)
    {
        byte[] frame = new byte[8 + payload.Length];
        frame[0] = 0xAA;
        frame[1] = 0x55;
        frame[2] = 0x01;
        frame[3] = 0x10;

        if (byteOrder == ByteOrder.BigEndian)
        {
            frame[4] = (byte)(payload.Length >> 8);
            frame[5] = (byte)payload.Length;
        }
        else
        {
            frame[4] = (byte)payload.Length;
            frame[5] = (byte)(payload.Length >> 8);
        }

        payload.CopyTo(frame.AsSpan(6));
        return frame;
    }
}
