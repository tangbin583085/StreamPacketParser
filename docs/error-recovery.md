# 错误恢复 / Recovery

| 情况 | 处理 |
| --- | --- |
| 帧头前存在噪声 | 丢弃噪声，返回 noise_discarded |
| 末尾只有部分帧头 | 保留最长的帧头前缀，等待下一次输入 |
| 长度字段尚未收齐 | 保留缓存 |
| 长度超出配置范围 | 丢弃一个字节，返回 invalid_frame_length，重新搜索 |
| 帧尚未收齐 | 等待更多数据 |
| 校验不通过 | 丢弃一个字节，返回 validation_failed，重新搜索 |
| 校验函数抛异常 | 保存 exception_ptr，返回 validator_exception，继续搜索 |
| 缓存满且仍不能推进 | 防御性丢弃一个字节并返回 buffer_limit_exceeded |

对被拒绝的候选帧只丢弃一个字节，而不是丢弃整段声明长度，
以保留在坏帧内部出现有效帧头的恢复机会。诊断中的 `discarded_bytes`
是该次操作实际丢弃的字节数，不是坏帧的声明长度。

合法配置要求最大缓存不小于最大帧，因此正常等待完整帧不应触发缓存满保护。
一次输入可以超过缓存上限，解析器会分段接收并消费。

长度看起来合法时，无法仅凭后面出现了帧头就认定前面是坏帧，
因为 Payload 本身也可能包含帧头。对于没有后续字节的截断帧，
由上位机设置接收超时，再调用 `reset()`。这里不内置计时器或设备重连逻辑。

## English

Noise is discarded while the longest possible header suffix is retained. Incomplete
headers, fields, and frames wait for more input. A rejected length, failed checksum,
or throwing validator advances the search by one byte, preserving potential nested headers.

The buffer-full case is a defensive fallback: valid settings already ensure the largest
frame fits in the pending buffer. Large input calls are consumed in chunks.

A plausible length cannot be rejected just because another header appears later:
payloads may contain that same byte sequence. The application owns timeouts and should
reset an abandoned partial frame when starting a new session.
