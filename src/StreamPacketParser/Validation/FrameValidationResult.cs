namespace StreamPacketParser.Validation;

/// <summary>
/// 一条完整候选帧的校验结果。
/// </summary>
public readonly struct FrameValidationResult
{
    private FrameValidationResult(
        bool isValid,
        FrameValidationErrorCode errorCode,
        string? errorMessage)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 获取这条帧是否通过校验。
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 获取校验错误代码。
    /// </summary>
    public FrameValidationErrorCode ErrorCode { get; }

    /// <summary>
    /// 获取可选的校验说明。
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 创建一个表示校验成功的结果。
    /// </summary>
    /// <returns>表示成功的结果。</returns>
    public static FrameValidationResult Valid()
    {
        return new FrameValidationResult(true, FrameValidationErrorCode.None, null);
    }

    /// <summary>
    /// 创建一个表示校验失败的结果。
    /// </summary>
    /// <param name="errorCode">拒绝这条帧的原因。</param>
    /// <param name="errorMessage">可选的文字说明。</param>
    /// <returns>表示失败的结果。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="errorCode"/> 为 <see cref="FrameValidationErrorCode.None"/>。</exception>
    public static FrameValidationResult Invalid(
        FrameValidationErrorCode errorCode,
        string? errorMessage = null)
    {
        if (errorCode == FrameValidationErrorCode.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(errorCode),
                "校验失败时必须提供具体的错误代码。");
        }

        return new FrameValidationResult(false, errorCode, errorMessage);
    }
}
