namespace StreamPacketParser.Validation;

/// <summary>
/// 说明完整帧为什么没有通过校验。
/// </summary>
public enum FrameValidationErrorCode
{
    /// <summary>
    /// 没有校验错误。
    /// </summary>
    None,

    /// <summary>
    /// 帧长度不足，无法执行当前校验规则。
    /// </summary>
    FrameTooShort,

    /// <summary>
    /// 计算出的校验值与帧中携带的值不一致。
    /// </summary>
    ChecksumMismatch,

    /// <summary>
    /// 自定义校验器因为协议规则拒绝了这条帧。
    /// </summary>
    InvalidFrame,
}
