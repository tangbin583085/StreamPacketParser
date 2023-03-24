namespace StreamPacketParser.Validation;

/// <summary>
/// 校验帧尾的一字节 XOR 校验值。
/// </summary>
public sealed class XorChecksumValidator : IFrameValidator
{
    /// <summary>
    /// 创建一个 XOR 校验器。
    /// </summary>
    /// <param name="dataStartOffset">参与计算的第一个帧字节位置。</param>
    /// <param name="checksumOffsetFromEnd">校验字节相对帧尾的位置，从1开始向前数。</param>
    public XorChecksumValidator(int dataStartOffset = 0, int checksumOffsetFromEnd = 1)
    {
        if (dataStartOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataStartOffset));
        }

        if (checksumOffsetFromEnd < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(checksumOffsetFromEnd));
        }

        DataStartOffset = dataStartOffset;
        ChecksumOffsetFromEnd = checksumOffsetFromEnd;
    }

    /// <summary>
    /// 获取参与计算的第一个帧字节位置。
    /// </summary>
    public int DataStartOffset { get; }

    /// <summary>
    /// 获取校验字节相对帧尾的位置。
    /// </summary>
    public int ChecksumOffsetFromEnd { get; }

    /// <inheritdoc />
    public FrameValidationResult Validate(ReadOnlySpan<byte> frame)
    {
        int checksumIndex = frame.Length - ChecksumOffsetFromEnd;
        if (checksumIndex < DataStartOffset || checksumIndex < 0 || checksumIndex >= frame.Length)
        {
            return FrameValidationResult.Invalid(
                FrameValidationErrorCode.FrameTooShort,
                "帧长度不足，无法按当前配置计算XOR校验。");
        }

        byte calculated = 0;
        foreach (byte value in frame[DataStartOffset..checksumIndex])
        {
            calculated ^= value;
        }

        byte expected = frame[checksumIndex];
        return calculated == expected
            ? FrameValidationResult.Valid()
            : FrameValidationResult.Invalid(
                FrameValidationErrorCode.ChecksumMismatch,
                $"XOR校验不匹配。帧中值为0x{expected:X2}，计算值为0x{calculated:X2}。");
    }
}
