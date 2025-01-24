using StreamPacketParser.Diagnostics;
using StreamPacketParser.Validation;

namespace StreamPacketParser.Tests;

public sealed class FrameValidatorTests
{
    [Fact]
    public void XorChecksumValidator_MatchingChecksum_ReturnsValid()
    {
        var validator = new XorChecksumValidator(dataStartOffset: 1);
        byte[] frame = [0xAA, 0x10, 0x20, 0x30, 0x00];

        FrameValidationResult result = validator.Validate(frame);

        Assert.True(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void XorChecksumValidator_MismatchedChecksum_ReturnsFailure()
    {
        var validator = new XorChecksumValidator(dataStartOffset: 1);
        byte[] frame = [0xAA, 0x10, 0x20, 0x30, 0xFF];

        FrameValidationResult result = validator.Validate(frame);

        Assert.False(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.ChecksumMismatch, result.ErrorCode);
    }

    [Fact]
    public void XorChecksumValidator_FrameTooShort_ReturnsFailure()
    {
        var validator = new XorChecksumValidator(dataStartOffset: 2);

        FrameValidationResult result = validator.Validate([0x00, 0x00]);

        Assert.False(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.FrameTooShort, result.ErrorCode);
    }

    [Fact]
    public void Crc16ModbusValidator_KnownVectorLittleEndian_ReturnsValid()
    {
        var validator = new Crc16ModbusValidator();
        byte[] frame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD];

        FrameValidationResult result = validator.Validate(frame);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Crc16ModbusValidator_KnownVectorBigEndian_ReturnsValid()
    {
        var validator = new Crc16ModbusValidator(checksumByteOrder: ByteOrder.BigEndian);
        byte[] frame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xCD, 0xC5];

        FrameValidationResult result = validator.Validate(frame);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Crc16ModbusValidator_MismatchedChecksum_ReturnsFailure()
    {
        var validator = new Crc16ModbusValidator();
        byte[] frame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00];

        FrameValidationResult result = validator.Validate(frame);

        Assert.False(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.ChecksumMismatch, result.ErrorCode);
    }

    [Fact]
    public void Crc16ModbusValidator_FrameTooShort_ReturnsFailure()
    {
        var validator = new Crc16ModbusValidator(dataStartOffset: 2);

        FrameValidationResult result = validator.Validate([0x00, 0x00]);

        Assert.False(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.FrameTooShort, result.ErrorCode);
    }

    [Fact]
    public void Crc16ModbusValidator_InvalidChecksumOffset_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Crc16ModbusValidator(
            checksumOffsetFromEnd: 1));
    }

    [Fact]
    public void PacketParser_ValidCrcFrame_ReturnsPacket()
    {
        PacketParser parser = CreateCrcParser();
        byte[] frame = CreateCrcFrame([0x11, 0x22, 0x33]);

        PacketParseResult result = parser.Append(frame);

        Assert.Equal(frame, Assert.Single(result.Packets).RawData.ToArray());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void PacketParser_InvalidCrcFollowedByValidFrame_Recovers()
    {
        PacketParser parser = CreateCrcParser();
        byte[] invalidFrame = CreateCrcFrame([0x11, 0x22]);
        invalidFrame[^1] ^= 0xFF;
        byte[] validFrame = CreateCrcFrame([0x33, 0x44]);

        PacketParseResult result = parser.Append([.. invalidFrame, .. validFrame]);

        ParsedPacket packet = Assert.Single(result.Packets);
        Assert.Equal(validFrame, packet.RawData.ToArray());
        Assert.Contains(result.Diagnostics, item => item.Code == ParserDiagnosticCode.ValidationFailed);
    }

    [Fact]
    public void PacketParser_ValidatorThrowsOnce_ReportsExceptionAndParsesFollowingFrame()
    {
        PacketParserOptions options = CreateOptions(new ThrowOnceValidator());
        var parser = new PacketParser(options);
        byte[] firstFrame = CreateCrcFrame([0x11]);
        byte[] secondFrame = CreateCrcFrame([0x22]);

        PacketParseResult result = parser.Append([.. firstFrame, .. secondFrame]);

        Assert.Equal(secondFrame, Assert.Single(result.Packets).RawData.ToArray());
        Assert.Contains(result.Diagnostics, item =>
            item.Code == ParserDiagnosticCode.ValidatorException &&
            item.Exception is InvalidOperationException);
        Assert.True(parser.BufferedByteCount < options.MaxBufferedBytes);
    }

    [Fact]
    public void PacketParser_ValidXorFrame_ReturnsPacket()
    {
        PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 7,
            minFrameLength: 7,
            maxFrameLength: 64,
            maxBufferedBytes: 128,
            validator: new XorChecksumValidator(dataStartOffset: 2),
            payloadOffset: 6);
        var parser = new PacketParser(options);
        byte[] frame = CreateXorFrame([0x11, 0x22]);

        PacketParseResult result = parser.Append(frame);

        Assert.Equal(frame, Assert.Single(result.Packets).RawData.ToArray());
    }

    private static PacketParser CreateCrcParser()
    {
        return new PacketParser(CreateOptions(new Crc16ModbusValidator(
            dataStartOffset: 2,
            checksumOffsetFromEnd: 2,
            checksumByteOrder: ByteOrder.LittleEndian)));
    }

    private static PacketParserOptions CreateOptions(IFrameValidator validator)
    {
        return PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 64,
            maxBufferedBytes: 128,
            validator: validator,
            payloadOffset: 6);
    }

    private static byte[] CreateCrcFrame(ReadOnlySpan<byte> payload)
    {
        byte[] frame = new byte[8 + payload.Length];
        frame[0] = 0xAA;
        frame[1] = 0x55;
        frame[2] = 0x01;
        frame[3] = 0x10;
        frame[4] = (byte)(payload.Length >> 8);
        frame[5] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(6));

        ushort crc = CalculateCrc16Modbus(frame.AsSpan(2, frame.Length - 4));
        frame[^2] = (byte)crc;
        frame[^1] = (byte)(crc >> 8);
        return frame;
    }

    private static ushort CalculateCrc16Modbus(ReadOnlySpan<byte> data)
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

    private static byte[] CreateXorFrame(ReadOnlySpan<byte> payload)
    {
        byte[] frame = new byte[7 + payload.Length];
        frame[0] = 0xAA;
        frame[1] = 0x55;
        frame[2] = 0x01;
        frame[3] = 0x10;
        frame[4] = (byte)(payload.Length >> 8);
        frame[5] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(6));

        byte checksum = 0;
        foreach (byte value in frame.AsSpan(2, frame.Length - 3))
        {
            checksum ^= value;
        }

        frame[^1] = checksum;
        return frame;
    }

    private sealed class ThrowOnceValidator : IFrameValidator
    {
        private bool _hasThrown;

        public FrameValidationResult Validate(ReadOnlySpan<byte> frame)
        {
            _ = frame;
            if (!_hasThrown)
            {
                _hasThrown = true;
                throw new InvalidOperationException("Test validator failure.");
            }

            return FrameValidationResult.Valid();
        }
    }
}
