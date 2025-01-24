# Qt 接入 / Qt integration

## CMake 工程

```cmake
add_subdirectory(third_party/StreamPacketParser)
target_link_libraries(MyApp PRIVATE StreamPacketParser::StreamPacketParser)
```

核心库与 Qt 版本无关。现成示例使用 Qt 5.15 / Qt 6 的 API。
在装有 Qt 的机器上，按需构建：

```sh
cmake -S . -B build-qt -DCMAKE_PREFIX_PATH=/path/to/Qt -DSPP_BUILD_QT_TCP_EXAMPLE=ON -DSPP_BUILD_QT_SERIAL_EXAMPLE=ON
cmake --build build-qt --config Debug
```

没有 SerialPort 模块时，只开启 TCP 选项即可。这里不会下载或安装 Qt。

## qmake 工程

把库源码复制到 `third_party/StreamPacketParser` 后，在应用的 `.pro` 中添加：

```qmake
CONFIG += c++17
SPP_ROOT = $$PWD/third_party/StreamPacketParser
INCLUDEPATH += $$SPP_ROOT/include
SOURCES += $$SPP_ROOT/src/PacketParser.cpp \
           $$SPP_ROOT/src/PacketParserOptions.cpp \
           $$SPP_ROOT/src/Checksums.cpp \
           $$SPP_ROOT/src/Validators.cpp
```

TCP 应用需要 `QT += network`；串口应用需要 `QT += serialport`。
核心库使用 C++ 异常，工程不要关闭异常支持。

## 运行示例

TCP 示例接收参数：`spp_qt_tcp <host> <port>`。
串口示例接收参数：`spp_qt_serial <port-name> [baud-rate]`。
串口默认 115200、8N1、无流控，示例以只读方式打开。
可执行文件位于构建目录的 `examples/qt` 下；多配置生成器还会有 Debug/Release 子目录。

两者都是命令行接收程序，不含 GUI、不主动发送命令。先配置一个发送
[示例协议](sample-protocol.md) 数据的设备或服务。

## 接收与重连

每条连接持有一个解析器，放在 `readyRead` 外创建。
示例按不超过 64 KiB 的块读取，结果在回调中处理。Qt 的读取缓冲上限也设为 64 KiB，
但应用层应继续考虑事件循环响应和消费速度。

设备连接、读取和解析应在同一个线程中完成。需要更新 GUI 时使用队列信号或其他
线程安全方式传递拥有独立内存的数据，不传递临时 ByteView。

示例在断线或设备错误时重置并退出，没有自动重连。应用自己实现重连时，
必须在新会话开始前清空解析器缓存。接收超时也由应用决定，
不要把前一台设备的残留数据传给下一台设备。
