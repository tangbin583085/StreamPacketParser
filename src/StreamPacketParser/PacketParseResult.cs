using StreamPacketParser.Diagnostics;

namespace StreamPacketParser;

/// <summary>
/// 保存一次 `Append` 产生的数据包和诊断信息。
/// </summary>
public sealed class PacketParseResult
{
    internal PacketParseResult(
        IReadOnlyList<ParsedPacket> packets,
        IReadOnlyList<ParserDiagnostic> diagnostics)
    {
        Packets = packets is List<ParsedPacket> packetList
            ? packetList.AsReadOnly()
            : packets;
        Diagnostics = diagnostics is List<ParserDiagnostic> diagnosticList
            ? diagnosticList.AsReadOnly()
            : diagnostics;
    }

    /// <summary>
    /// 获取本次解析出的完整数据包。
    /// </summary>
    public IReadOnlyList<ParsedPacket> Packets { get; }

    /// <summary>
    /// 获取本次解析过程中产生的诊断信息。
    /// </summary>
    public IReadOnlyList<ParserDiagnostic> Diagnostics { get; }
}
