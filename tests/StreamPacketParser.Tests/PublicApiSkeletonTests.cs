using StreamPacketParser.Validation;

namespace StreamPacketParser.Tests;

public sealed class PublicApiSkeletonTests
{
    [Fact]
    public void CreateLengthFieldProtocol_CopiesHeaderAndExposesConfiguration()
    {
        byte[] header = [0xAA, 0x55];

        PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
            header,
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 4096,
            maxBufferedBytes: 8192);

        header[0] = 0x00;

        Assert.Equal(new byte[] { 0xAA, 0x55 }, options.Header.ToArray());
        Assert.Same(NoValidation.Instance, options.Validator);
    }

    [Fact]
    public void NoValidation_AcceptsCandidateFrame()
    {
        FrameValidationResult result = NoValidation.Instance.Validate([0xAA, 0x55]);

        Assert.True(result.IsValid);
        Assert.Equal(FrameValidationErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void CreateLengthFieldProtocol_EmptyHeader_Throws()
    {
        Assert.Throws<ArgumentException>(() => PacketParserOptions.CreateLengthFieldProtocol(
            header: ReadOnlyMemory<byte>.Empty,
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 64,
            maxBufferedBytes: 128));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void CreateLengthFieldProtocol_UnsupportedLengthSize_Throws(int lengthFieldSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 4,
            lengthFieldSize: lengthFieldSize,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 64,
            maxBufferedBytes: 128));
    }

    [Fact]
    public void CreateLengthFieldProtocol_PreservesConfiguredValidator()
    {
        var validator = new XorChecksumValidator();
        PacketParserOptions options = PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 4,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 64,
            maxBufferedBytes: 128,
            validator: validator);

        Assert.Same(validator, options.Validator);
    }

    [Fact]
    public void CreateLengthFieldProtocol_HeaderOverlapsLengthField_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PacketParserOptions.CreateLengthFieldProtocol(
            header: new byte[] { 0xAA, 0x55 },
            lengthFieldOffset: 1,
            lengthFieldSize: 2,
            byteOrder: ByteOrder.BigEndian,
            lengthMode: FrameLengthMode.PayloadLength,
            fixedFrameOverhead: 8,
            minFrameLength: 8,
            maxFrameLength: 64,
            maxBufferedBytes: 128));
    }
}
