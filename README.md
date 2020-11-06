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
