namespace StreamPacketParser.Diagnostics;

/// <summary>
/// 解析输入时发现的、可以继续恢复的情况。
/// </summary>
public enum ParserDiagnosticCode
{
    /// <summary>
    /// 丢弃了有效帧头之前的噪声字节。
    /// </summary>
    NoiseDiscarded,

    /// <summary>
    /// 长度字段超出配置范围，或长度计算发生溢出。
    /// </summary>
    InvalidFrameLength,

    /// <summary>
    /// 一条已经收齐的候选帧校验失败。
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// 校验器处理候选帧时抛出了异常。
    /// </summary>
    ValidatorException,

    /// <summary>
    /// 缓存数据达到配置的上限。
    /// </summary>
    BufferLimitExceeded,

}
