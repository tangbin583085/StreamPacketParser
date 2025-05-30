# 示例协议

Console示例和单元测试只使用下面这套虚构协议，不对应任何真实厂商或商业设备。

| Offset | Size | Field |
| --- | ---: | --- |
| 0 | 2 | Header `AA 55` |
| 2 | 1 | Protocol version |
| 3 | 1 | Command |
| 4 | 2 | Payload length，unsigned big-endian |
| 6 | N | Payload |
| 6 + N | 2 | CRC16-Modbus，low byte first |

完整帧长度为 `8 + payload length`。

CRC从offset 2的版本字段开始计算，到Payload最后一个字节结束，不包含Header和最后两个CRC字节。
