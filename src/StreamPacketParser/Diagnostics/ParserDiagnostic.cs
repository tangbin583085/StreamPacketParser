namespace StreamPacketParser.Diagnostics;

/// <summary>
/// 描述解析过程中可以恢复的输入或校验问题。
/// </summary>
public sealed class ParserDiagnostic
{
    internal ParserDiagnostic(
        ParserDiagnosticCode code,
        string message,
        int discardedByteCount = 0,
        Exception? exception = null)
    {
        Code = code;
        Message = message;
        DiscardedByteCount = discardedByteCount;
        Exception = exception;
    }

    /// <summary>
    /// 获取供程序判断的诊断代码。
    /// </summary>
    public ParserDiagnosticCode Code { get; }

    /// <summary>
    /// 获取给人阅读的诊断说明。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 获取这次操作丢弃的字节数。
    /// </summary>
    public int DiscardedByteCount { get; }

    /// <summary>
    /// 如果扩展组件执行失败，获取对应的异常；没有异常时为 `null`。
    /// </summary>
    public Exception? Exception { get; }
}
