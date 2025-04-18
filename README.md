# StreamPacketParser

一个 C++17 写的二进制流拆包库，适合 Qt 上位机的 TCP、串口等接收场景。把收到的字节依次交给解析器，就能拿到完整帧，不需要假设一次 `readyRead` 就是一条消息。

核心库不依赖 Qt，也不负责连接设备、发命令或更新界面。仓库提供普通 C++、Qt TCP 和 Qt 串口示例。

## 功能

- 固定帧头，1、2、4 字节无符号长度字段，支持大小端。
- 长度字段可以表示 Payload 长度，也可以表示完整帧长度。
- 处理半包、粘包、连续多帧和帧头前的噪声。
- 可选 XOR、CRC16-Modbus 校验，也可以传入自己的校验函数。
- 返回完整帧、Payload 和诊断信息，支持缓存上限及手动重置。

这是 C++ 接口，暂不提供纯 C ABI。核心使用 C++17 标准库；Qt 示例面向 Qt 5.15 / Qt 6。

## 添加到项目

目前推荐把源码放进自己的工程，使用 CMake 引入：

```cmake
add_subdirectory(third_party/StreamPacketParser)
target_link_libraries(MyApp PRIVATE StreamPacketParser::StreamPacketParser)
```

也可以在准备好 C++17 编译器和 CMake 3.16 或以上版本的环境中安装：

```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=./install
cmake --build build --config Release
cmake --install build --config Release
```

使用安装后的库时，将安装目录加入 `CMAKE_PREFIX_PATH`，再写：

```cmake
find_package(StreamPacketParser 3.6 CONFIG REQUIRED)
target_link_libraries(MyApp PRIVATE StreamPacketParser::StreamPacketParser)
```

目前没有发布到包管理器，使用源码集成或本地安装即可。现有 qmake 工程的接入方法见 [Qt 接入说明](docs/qt-integration.md)。

## 简单使用

下面使用的示例帧格式是：

```text
AA 55 | Version(1) | Command(1) | PayloadLength(2, BE) | Payload(N) | CRC(2, LE)
```

```cpp
#include <StreamPacketParser/StreamPacketParser.hpp>

spp::PacketParserOptions options;
options.payload_offset = 6;
options.validator = spp::crc16_modbus_validator(2);
spp::PacketParser parser(options);

// 在每次收到数据时调用；parser 要在多次接收之间保留。
void on_bytes(const std::uint8_t* data, std::size_t size)
{
    auto result = parser.append(data, size);
    for (const auto& packet : result.packets) {
        // packet.raw_data 是完整帧，packet.payload 是数据区。
        handle_frame(packet.raw_data, packet.payload);
    }
    for (const auto& diagnostic : result.diagnostics) {
        // 按需记录 diagnostic.code、message 和 discarded_bytes。
    }
}
```

`handle_frame` 是你的业务函数，需要自行实现。默认配置的帧头是 `AA 55`，长度字段位于 offset 4，占 2 字节，固定开销为 8 字节，最大帧长为 4096，缓存上限为 8192。**默认不校验 CRC**，上例显式开启了校验。

CRC 从 offset 2 开始，到 Payload 末尾结束。具体字段见 [示例协议](docs/sample-protocol.md)。

## Qt 接收数据

在 `QTcpSocket::readyRead` 或 `QSerialPort::readyRead` 中，把收到的 `QByteArray` 交给同一个解析器：

```cpp
const QByteArray bytes = device.readAll();
auto result = parser.append(
    reinterpret_cast<const std::uint8_t*>(bytes.constData()),
    static_cast<std::size_t>(bytes.size()));
```

`device` 可以是 TCP socket 或串口对象。这只是接入片段；仓库中的完整示例按 64 KiB 分批读取，避免一次处理太多数据。

- 每条连接或每个串口使用独立的解析器。
- 断线、切换设备或重新打开串口时调用 `parser.reset()`。
- 同一个实例按顺序调用，不要跨线程同时访问，也不要在校验回调中重新调用它。
- 返回的数据包拥有自己的内存，下一次接收或重置不会修改之前的结果。

完整接入和构建命令见 [docs/qt-integration.md](docs/qt-integration.md)。

## 长度和校验配置

`payload_length` 模式下，总帧长 = 长度字段值 + `fixed_frame_overhead`。Payload 默认从长度字段后开始，也可以设置 `payload_offset`。

`total_frame_length` 模式下，长度字段就是总帧长，`fixed_frame_overhead` 必须为 0。不设置 `payload_offset` 时 Payload 为空；设置后会包含从该位置到帧尾的全部字节，包括尾部校验字段。

XOR 和 CRC 校验支持设置起始位置及校验字段距帧尾的位置。CRC 的字节序独立于长度字段。自定义校验器的参数是只读 `spp::ByteView`，返回 `spp::ValidationResult`；不要保留这个临时视图。

配置错误会抛出 `std::invalid_argument`。输入中的非法长度、校验失败和校验回调异常会作为诊断返回，解析器丢弃一个字节后继续寻找帧头。

## 缓存与恢复

不完整的帧会保留到下次接收。噪声末尾若有半个帧头，也会保留。合理但错误的长度值可能让解析器等待后续字节，因此超时判断应由上位机处理，必要时 `reset()`。

`max_buffered_bytes` 限制未解析的缓存字节数，不是整个调用的内存配额。一次传入很多帧，会得到很多结果；长噪声也可能产生多条诊断。高流量场景建议分批输入并及时处理结果。

详见 [恢复规则](docs/error-recovery.md) 和 [实现说明](docs/architecture.md)。

## 示例和测试

在已有开发环境中可使用以下命令：

```sh
cmake -S . -B build -DSPP_BUILD_TESTS=ON -DSPP_BUILD_EXAMPLES=ON
cmake --build build --config Debug
(cd build && ctest -C Debug --output-on-failure)
```

普通示例目标名是 `spp_console`。测试不依赖第三方测试框架，覆盖分片输入、连续帧、长度模式、校验、错误恢复、缓存边界和结果生命周期。Qt 示例默认不构建，也不会自动下载 Qt。

## 目录

```text
include/StreamPacketParser/  对外接口
src/                        解析与校验实现
tests/                      测试源码
examples/console/           普通 C++ 示例
examples/qt/                Qt TCP、串口示例
docs/                       协议与接入说明
```
