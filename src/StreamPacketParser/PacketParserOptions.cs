using StreamPacketParser.Validation;

namespace StreamPacketParser;

/// <summary>
/// 描述一套带长度字段的二进制帧格式。
/// </summary>
public sealed class PacketParserOptions
{
    private readonly byte[] _header;

    private PacketParserOptions(
        byte[] header,
        int lengthFieldOffset,
        int lengthFieldSize,
        ByteOrder byteOrder,
        FrameLengthMode lengthMode,
        int fixedFrameOverhead,
        int minFrameLength,
        int maxFrameLength,
        int maxBufferedBytes,
        IFrameValidator validator,
        int? payloadOffset)
    {
        _header = header;
        LengthFieldOffset = lengthFieldOffset;
        LengthFieldSize = lengthFieldSize;
        ByteOrder = byteOrder;
        LengthMode = lengthMode;
        FixedFrameOverhead = fixedFrameOverhead;
        MinFrameLength = minFrameLength;
        MaxFrameLength = maxFrameLength;
        MaxBufferedBytes = maxBufferedBytes;
        Validator = validator;
        PayloadOffset = payloadOffset;
    }

    /// <summary>
    /// 获取每条帧开头固定出现的字节序列。
    /// </summary>
    public ReadOnlyMemory<byte> Header => _header;

    /// <summary>
    /// 获取长度字段相对于帧起始位置的零基偏移。
    /// </summary>
    public int LengthFieldOffset { get; }

    /// <summary>
    /// 获取长度字段占用的字节数。
    /// </summary>
    public int LengthFieldSize { get; }

    /// <summary>
    /// 获取长度字段使用的字节序。
    /// </summary>
    public ByteOrder ByteOrder { get; }

    /// <summary>
    /// 获取解析出的长度值的含义。
    /// </summary>
    public FrameLengthMode LengthMode { get; }

    /// <summary>
    /// 当 <see cref="LengthMode"/> 为 <see cref="FrameLengthMode.PayloadLength"/> 时，获取帧中固定的非Payload字节数。
    /// </summary>
    public int FixedFrameOverhead { get; }

    /// <summary>
    /// 获取允许的最小完整帧长度。
    /// </summary>
    public int MinFrameLength { get; }

    /// <summary>
    /// 获取允许的最大完整帧长度。
    /// </summary>
    public int MaxFrameLength { get; }

    /// <summary>
    /// 获取解析器最多保留的字节数。
    /// </summary>
    public int MaxBufferedBytes { get; }

    /// <summary>
    /// 获取用于校验完整候选帧的校验器。
    /// </summary>
    public IFrameValidator Validator { get; }

    /// <summary>
    /// 获取Payload相对于帧起始位置的可选零基偏移。
    /// </summary>
    public int? PayloadOffset { get; }

    /// <summary>
    /// 创建一套包含固定帧头和无符号长度字段的协议配置。
    /// </summary>
    /// <param name="header">固定帧头，内部会复制一份。</param>
    /// <param name="lengthFieldOffset">长度字段相对于帧起始位置的零基偏移。</param>
    /// <param name="lengthFieldSize">长度字段大小，目前支持1、2、4字节。</param>
    /// <param name="byteOrder">长度字段的字节序。</param>
    /// <param name="lengthMode">长度字段中数值的含义。</param>
    /// <param name="fixedFrameOverhead">在Payload长度模式下，帧中固定的非Payload字节数。</param>
    /// <param name="minFrameLength">允许的最小完整帧长度。</param>
    /// <param name="maxFrameLength">允许的最大完整帧长度。</param>
    /// <param name="maxBufferedBytes">解析器最多保留的字节数。</param>
    /// <param name="validator">完整帧校验器；传入 <see langword="null"/> 表示不校验。</param>
    /// <param name="payloadOffset">Payload相对于帧起始位置的可选零基偏移。</param>
    /// <returns>不可变的协议配置。</returns>
    /// <exception cref="ArgumentException">协议配置之间存在矛盾，或使用了不支持的组合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">某个数值参数超出允许范围。</exception>
    public static PacketParserOptions CreateLengthFieldProtocol(
        ReadOnlyMemory<byte> header,
        int lengthFieldOffset,
        int lengthFieldSize,
        ByteOrder byteOrder,
        FrameLengthMode lengthMode,
        int fixedFrameOverhead,
        int minFrameLength,
        int maxFrameLength,
        int maxBufferedBytes,
        IFrameValidator? validator = null,
        int? payloadOffset = null)
    {
        Validate(
            header,
            lengthFieldOffset,
            lengthFieldSize,
            byteOrder,
            lengthMode,
            fixedFrameOverhead,
            minFrameLength,
            maxFrameLength,
            maxBufferedBytes,
            payloadOffset);

        return new PacketParserOptions(
            header.ToArray(),
            lengthFieldOffset,
            lengthFieldSize,
            byteOrder,
            lengthMode,
            fixedFrameOverhead,
            minFrameLength,
            maxFrameLength,
            maxBufferedBytes,
            validator ?? NoValidation.Instance,
            payloadOffset);
    }

    private static void Validate(
        ReadOnlyMemory<byte> header,
        int lengthFieldOffset,
        int lengthFieldSize,
        ByteOrder byteOrder,
        FrameLengthMode lengthMode,
        int fixedFrameOverhead,
        int minFrameLength,
        int maxFrameLength,
        int maxBufferedBytes,
        int? payloadOffset)
    {
        if (header.IsEmpty)
        {
            throw new ArgumentException("帧头不能为空。", nameof(header));
        }

        if (lengthFieldOffset < header.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lengthFieldOffset),
                "长度字段不能与固定帧头重叠。");
        }

        if (lengthFieldSize is not (1 or 2 or 4))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lengthFieldSize),
                "长度字段大小只能是1、2或4字节。");
        }

        if (!Enum.IsDefined(byteOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(byteOrder));
        }

        if (!Enum.IsDefined(lengthMode))
        {
            throw new ArgumentOutOfRangeException(nameof(lengthMode));
        }

        int lengthFieldEnd;
        try
        {
            lengthFieldEnd = checked(lengthFieldOffset + lengthFieldSize);
        }
        catch (OverflowException exception)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lengthFieldOffset),
                lengthFieldOffset,
                $"长度字段位置超出了支持的帧索引范围。{exception.Message}");
        }

        if (minFrameLength < lengthFieldEnd)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minFrameLength),
                "最小帧长度必须能够覆盖完整的长度字段。");
        }

        if (maxFrameLength < minFrameLength)
        {
            throw new ArgumentException(
                "最大帧长度不能小于最小帧长度。",
                nameof(maxFrameLength));
        }

        if (maxBufferedBytes < maxFrameLength)
        {
            throw new ArgumentException(
                "最大缓存字节数不能小于最大帧长度。",
                nameof(maxBufferedBytes));
        }

        if (lengthMode == FrameLengthMode.PayloadLength)
        {
            if (fixedFrameOverhead < lengthFieldEnd || fixedFrameOverhead > maxFrameLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fixedFrameOverhead),
                    "FixedFrameOverhead必须包含长度字段，并且不能超过最大帧长度。");
            }
        }
        else if (fixedFrameOverhead != 0)
        {
            throw new ArgumentException(
                "当长度字段表示完整帧长度时，FixedFrameOverhead必须为0。",
                nameof(fixedFrameOverhead));
        }

        if (payloadOffset is < 0 || payloadOffset > minFrameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadOffset),
                "PayloadOffset必须位于最小帧边界之内。");
        }
    }
}
