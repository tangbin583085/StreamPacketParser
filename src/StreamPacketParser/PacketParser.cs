using StreamPacketParser.Diagnostics;
using StreamPacketParser.Internal;
using StreamPacketParser.Validation;

namespace StreamPacketParser;

/// <summary>
/// 按顺序接收字节片段，并逐步解析出完整的二进制协议帧。
/// </summary>
/// <remarks>
/// 解析器实例不是线程安全的。一条有序数据流使用一个实例，并按顺序调用。
/// </remarks>
public sealed class PacketParser
{
    private readonly PacketParserOptions _options;
    private readonly ByteBuffer _buffer;

    /// <summary>
    /// 根据协议配置创建解析器。
    /// </summary>
    /// <param name="options">已经通过检查的协议配置。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/>。</exception>
    public PacketParser(PacketParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _buffer = new ByteBuffer(Math.Min(options.MaxBufferedBytes, Math.Max(options.Header.Length, 64)));
    }

    /// <summary>
    /// 获取当前尚未消费的缓存字节数。
    /// </summary>
    public int BufferedByteCount => _buffer.Count;

    /// <summary>
    /// 追加下一段有序字节，并解析当前已经收齐的所有帧。
    /// </summary>
    /// <param name="data">同一条有序数据流中的下一段字节。</param>
    /// <returns>本次追加产生的数据包和诊断信息。</returns>
    public PacketParseResult Append(ReadOnlySpan<byte> data)
    {
        List<ParsedPacket> packets = [];
        List<ParserDiagnostic> diagnostics = [];

        while (!data.IsEmpty)
        {
            ParseAvailable(packets, diagnostics);

            if (_buffer.Count >= _options.MaxBufferedBytes)
            {
                _buffer.Discard(1);
                diagnostics.Add(new ParserDiagnostic(
                    ParserDiagnosticCode.BufferLimitExceeded,
                    "缓存已达到上限，解析器丢弃了一个字节以继续向前处理。",
                    discardedByteCount: 1));
                continue;
            }

            int room = _options.MaxBufferedBytes - _buffer.Count;
            int count = Math.Min(room, data.Length);
            _buffer.Append(data[..count], _options.MaxBufferedBytes);
            data = data[count..];
        }

        ParseAvailable(packets, diagnostics);
        return new PacketParseResult(packets, diagnostics);
    }

    /// <summary>
    /// 清空尚未完成的数据和当前解析状态。
    /// </summary>
    public void Reset()
    {
        _buffer.Clear();
    }

    private void ParseAvailable(
        List<ParsedPacket> packets,
        List<ParserDiagnostic> diagnostics)
    {
        ReadOnlySpan<byte> header = _options.Header.Span;
        int lengthFieldEnd = checked(_options.LengthFieldOffset + _options.LengthFieldSize);

        while (_buffer.Count > 0)
        {
            ReadOnlySpan<byte> buffered = _buffer.Span;
            int headerIndex = buffered.IndexOf(header);
            if (headerIndex < 0)
            {
                int retained = GetPartialHeaderLength(buffered, header);
                int discarded = buffered.Length - retained;
                if (discarded > 0)
                {
                    _buffer.Discard(discarded);
                    diagnostics.Add(new ParserDiagnostic(
                        ParserDiagnosticCode.NoiseDiscarded,
                        "已丢弃下一个可能帧头之前的噪声字节。",
                        discarded));
                }

                return;
            }

            if (headerIndex > 0)
            {
                _buffer.Discard(headerIndex);
                diagnostics.Add(new ParserDiagnostic(
                    ParserDiagnosticCode.NoiseDiscarded,
                    "已丢弃帧头之前的噪声字节。",
                    headerIndex));
                continue;
            }

            if (_buffer.Count < lengthFieldEnd)
            {
                return;
            }

            uint encodedLength = ReadUnsignedLength(buffered, _options.LengthFieldOffset, _options.LengthFieldSize, _options.ByteOrder);
            if (!TryGetFrameLength(encodedLength, out int frameLength))
            {
                _buffer.Discard(1);
                diagnostics.Add(new ParserDiagnostic(
                    ParserDiagnosticCode.InvalidFrameLength,
                    "长度字段计算出的帧长度超出了配置范围。",
                    discardedByteCount: 1));
                continue;
            }

            if (_buffer.Count < frameLength)
            {
                return;
            }

            byte[] rawData = _buffer.CopyToArray(frameLength);
            FrameValidationResult validationResult;
#pragma warning disable CA1031 // 校验器异常属于输入或扩展组件问题，统一转成诊断返回。
            try
            {
                validationResult = _options.Validator.Validate(rawData);
            }
            catch (Exception exception)
            {
                _buffer.Discard(1);
                diagnostics.Add(new ParserDiagnostic(
                    ParserDiagnosticCode.ValidatorException,
                    "配置的帧校验器执行时抛出了异常。",
                    discardedByteCount: 1,
                    exception: exception));
                continue;
            }
#pragma warning restore CA1031

            if (!validationResult.IsValid)
            {
                _buffer.Discard(1);
                diagnostics.Add(new ParserDiagnostic(
                    ParserDiagnosticCode.ValidationFailed,
                    validationResult.ErrorMessage ?? "候选帧没有通过校验。",
                    discardedByteCount: 1));
                continue;
            }

            _buffer.Discard(frameLength);

            ParsedPacket packet = CreatePacket(rawData, encodedLength);
            packets.Add(packet);
        }
    }

    private bool TryGetFrameLength(uint encodedLength, out int frameLength)
    {
        try
        {
            long calculated = _options.LengthMode == FrameLengthMode.PayloadLength
                ? checked((long)encodedLength + _options.FixedFrameOverhead)
                : encodedLength;

            if (calculated < _options.MinFrameLength || calculated > _options.MaxFrameLength)
            {
                frameLength = 0;
                return false;
            }

            frameLength = checked((int)calculated);
            return true;
        }
        catch (OverflowException)
        {
            frameLength = 0;
            return false;
        }
    }

    private ParsedPacket CreatePacket(byte[] rawData, uint encodedLength)
    {
        int payloadOffset = _options.PayloadOffset
            ?? (_options.LengthMode == FrameLengthMode.PayloadLength
                ? _options.LengthFieldOffset + _options.LengthFieldSize
                : 0);

        int payloadLength = _options.LengthMode == FrameLengthMode.PayloadLength
            ? checked((int)encodedLength)
            : _options.PayloadOffset.HasValue
                ? rawData.Length - payloadOffset
                : 0;

        if (payloadOffset < 0 || payloadLength < 0 || payloadOffset > rawData.Length - payloadLength)
        {
            payloadOffset = 0;
            payloadLength = 0;
        }

        return new ParsedPacket(rawData, payloadOffset, payloadLength);
    }

    private static uint ReadUnsignedLength(
        ReadOnlySpan<byte> data,
        int offset,
        int size,
        ByteOrder byteOrder)
    {
        uint result = 0;
        if (byteOrder == ByteOrder.LittleEndian)
        {
            for (int index = 0; index < size; index++)
            {
                result |= (uint)data[offset + index] << (index * 8);
            }
        }
        else
        {
            for (int index = 0; index < size; index++)
            {
                result = (result << 8) | data[offset + index];
            }
        }

        return result;
    }

    private static int GetPartialHeaderLength(ReadOnlySpan<byte> data, ReadOnlySpan<byte> header)
    {
        int maximum = Math.Min(data.Length, header.Length - 1);
        for (int length = maximum; length > 0; length--)
        {
            if (data[^length..].SequenceEqual(header[..length]))
            {
                return length;
            }
        }

        return 0;
    }
}
