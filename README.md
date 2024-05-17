# StreamPacketParser.NET

一个用 C# 写的小型流式拆包库，主要处理半包、粘包、噪声和长度字段协议。

## 目前支持

- 固定帧头和长度字段
- 大端和小端长度
- XOR及CRC16-Modbus校验
- 非法数据后的重新同步

## English

StreamPacketParser.NET is a small C# library for splitting an incoming byte stream into complete protocol frames.

It supports partial frames, combined frames, configurable length fields, and basic checksum validation.
