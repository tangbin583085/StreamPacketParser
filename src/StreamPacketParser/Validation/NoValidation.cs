namespace StreamPacketParser.Validation;

/// <summary>
/// 不做校验，直接接受每一条已经收齐的候选帧。
/// </summary>
public sealed class NoValidation : IFrameValidator
{
    private NoValidation()
    {
    }

    /// <summary>
    /// 获取无状态的共享实例。
    /// </summary>
    public static NoValidation Instance { get; } = new();

    /// <inheritdoc />
    public FrameValidationResult Validate(ReadOnlySpan<byte> frame)
    {
        _ = frame;
        return FrameValidationResult.Valid();
    }
}
