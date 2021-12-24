namespace StreamPacketParser.Validation;

/// <summary>
/// 校验一条已经收齐的候选帧，不负责拆包和缓存管理。
/// </summary>
public interface IFrameValidator
{
    /// <summary>
    /// 校验一条完整候选帧。
    /// </summary>
    /// <param name="frame">完整候选帧。</param>
    /// <returns>校验结果。</returns>
    FrameValidationResult Validate(ReadOnlySpan<byte> frame);
}
