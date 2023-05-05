namespace StreamPacketParser.Validation;

/// <summary>
/// 校验完整帧中的 CRC16-Modbus 值。
/// </summary>
public sealed class Crc16ModbusValidator : IFrameValidator
{
    /// <summary>
    /// 创建一个 CRC16-Modbus 校验器。
    /// </summary>
    /// <param name="dataStartOffset">参与CRC计算的第一个帧字节位置。</param>
    /// <param name="checksumOffsetFromEnd">CRC第一个字节相对帧尾的位置，从2开始向前数。</param>
    /// <param name="checksumByteOrder">线上CRC值的字节序。</param>
    public Crc16ModbusValidator(
        int dataStartOffset = 0,
        int checksumOffsetFromEnd = 2,
        ByteOrder checksumByteOrder = ByteOrder.LittleEndian)
    {
        if (dataStartOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataStartOffset));
        }

        if (checksumOffsetFromEnd < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(checksumOffsetFromEnd));
        }

        if (!Enum.IsDefined(checksumByteOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(checksumByteOrder));
        }

        DataStartOffset = dataStartOffset;
        ChecksumOffsetFromEnd = checksumOffsetFromEnd;
        ChecksumByteOrder = checksumByteOrder;
    }

    /// <summary>
    /// 获取参与CRC计算的第一个帧字节位置。
    /// </summary>
    public int DataStartOffset { get; }

    /// <summary>
    /// 获取CRC第一个字节相对帧尾的位置。
    /// </summary>
    public int ChecksumOffsetFromEnd { get; }

    /// <summary>
    /// 获取线上CRC值的字节序。
    /// </summary>
    public ByteOrder ChecksumByteOrder { get; }

    /// <inheritdoc />
    public FrameValidationResult Validate(ReadOnlySpan<byte> frame)
    {
        int checksumIndex = frame.Length - ChecksumOffsetFromEnd;
        if (checksumIndex < DataStartOffset || checksumIndex < 0 || checksumIndex > frame.Length - 2)
        {
            return FrameValidationResult.Invalid(
                FrameValidationErrorCode.FrameTooShort,
                "帧长度不足，无法按当前配置计算CRC16-Modbus。");
        }

        ushort calculated = Calculate(frame[DataStartOffset..checksumIndex]);
        ushort expected = ChecksumByteOrder == ByteOrder.LittleEndian
            ? (ushort)(frame[checksumIndex] | (frame[checksumIndex + 1] << 8))
            : (ushort)((frame[checksumIndex] << 8) | frame[checksumIndex + 1]);

        return calculated == expected
            ? FrameValidationResult.Valid()
            : FrameValidationResult.Invalid(
                FrameValidationErrorCode.ChecksumMismatch,
                $"CRC16-Modbus校验不匹配。帧中值为0x{expected:X4}，计算值为0x{calculated:X4}。");
    }

    private static ushort Calculate(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x0001) != 0
                    ? (ushort)((crc >> 1) ^ 0xA001)
                    : (ushort)(crc >> 1);
            }
        }

        return crc;
    }
}
