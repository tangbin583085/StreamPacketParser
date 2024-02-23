# 示例协议 / Sample protocol

示例和测试使用这套演示协议，不对应具体厂商设备。

| Offset | Size | Field |
| --- | ---: | --- |
| 0 | 2 | Header: AA 55 |
| 2 | 1 | Version |
| 3 | 1 | Command |
| 4 | 2 | Payload length, unsigned big-endian |
| 6 | N | Payload |
| 6 + N | 2 | CRC16-Modbus, low byte first |

总帧长是 `8 + N`，示例的最大帧长为 4096，因此 Payload 最大为 4088 字节。
CRC 从 offset 2 开始，到 Payload 末尾结束，不包含帧头和 CRC 自身。

CRC 初值为 `0xFFFF`，反射多项式为 `0xA001`，无最终异或。
标准输入 ASCII `123456789` 的结果是 `0x4B37`。
长度字段字节序与 CRC 字节序独立，不要混用。

解析器不会验证 Version 和 Command 的业务意义。如需要，可在自定义校验器中检查，
或在收到完整帧后由业务层处理。TCP 和串口示例只接收符合此协议的帧。
