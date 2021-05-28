namespace StreamPacketParser;

/// <summary>
/// 说明长度字段中的数值代表什么。
/// </summary>
public enum FrameLengthMode
{
    /// <summary>
    /// 长度字段表示Payload长度，最终帧长度还要加上 `FixedFrameOverhead`。
    /// </summary>
    PayloadLength,

    /// <summary>
    /// 长度字段直接表示完整帧长度。
    /// </summary>
    TotalFrameLength,
}
