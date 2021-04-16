namespace StreamPacketParser;

/// <summary>
/// 多字节长度字段的字节序。
/// </summary>
public enum ByteOrder
{
    /// <summary>
    /// 低位字节在前。
    /// </summary>
    LittleEndian,

    /// <summary>
    /// 高位字节在前。
    /// </summary>
    BigEndian,
}
