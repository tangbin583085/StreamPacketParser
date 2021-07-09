namespace StreamPacketParser;

/// <summary>
/// 一条已经完成拆包、并拥有独立内存的数据帧。
/// </summary>
public sealed class ParsedPacket
{
    private readonly byte[] _rawData;

    internal ParsedPacket(byte[] rawData, int payloadOffset, int payloadLength)
    {
        _rawData = rawData;
        Payload = rawData.AsMemory(payloadOffset, payloadLength);
    }

    /// <summary>
    /// 获取完整帧的原始字节。
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _rawData;

    /// <summary>
    /// 获取Payload字节。如果配置没有明确Payload区域，这里可能为空。
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// 获取完整帧长度，单位为字节。
    /// </summary>
    public int FrameLength => _rawData.Length;
}
