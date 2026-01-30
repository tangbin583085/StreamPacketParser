# StreamPacketParser.NET

一个用 C# 写的小型二进制流拆包库，主要用来处理串口、TCP、USB、BLE 等通信中常见的半包和粘包问题。

项目只负责把连续收到的字节整理成完整的数据帧，不负责建立连接，也不包含设备业务逻辑。

当前项目使用 `.NET 10`，包版本为 `3.6.1`。

## 安装

项目已经按 NuGet 包的方式配置。包发布到 NuGet 后，可以这样安装：

```shell
dotnet add package StreamPacketParser --version 3.6.1
```

也可以在项目文件中添加：

```xml
<ItemGroup>
  <PackageReference Include="StreamPacketParser" Version="3.6.1" />
</ItemGroup>
```

如果 NuGet 上还没有这个版本，可以先把源码克隆下来，然后引用项目文件：

```shell
git clone https://github.com/tangbin583085/StreamPacketParser.git
dotnet add reference ../StreamPacketParser/src/StreamPacketParser/StreamPacketParser.csproj
```

## 主要功能

- 支持半包、粘包和一次解析多帧
- 根据固定帧头和长度字段拆包
- 支持大端和小端长度字段
- 自动跳过帧头前的噪声数据
- 遇到非法长度或校验失败时继续寻找下一帧
- 支持 XOR 和 CRC16-Modbus 校验
- 可以限制最大帧长度和缓存大小
- 返回解析过程中的简单诊断信息

## 简单用法

下面的例子使用两字节帧头，长度字段表示 Payload 的长度：

```csharp
using StreamPacketParser;
using StreamPacketParser.Validation;

var options = PacketParserOptions.CreateLengthFieldProtocol(
    header: new byte[] { 0xAA, 0x55 },
    lengthFieldOffset: 4,
    lengthFieldSize: 2,
    byteOrder: ByteOrder.BigEndian,
    lengthMode: FrameLengthMode.PayloadLength,
    fixedFrameOverhead: 8,
    minFrameLength: 8,
    maxFrameLength: 4096,
    maxBufferedBytes: 8192,
    validator: NoValidation.Instance,
    payloadOffset: 6);

var parser = new PacketParser(options);
PacketParseResult result = parser.Append(receivedBytes);

foreach (ParsedPacket packet in result.Packets)
{
    Console.WriteLine(Convert.ToHexString(packet.RawData.Span));
}
```

同一个 `PacketParser` 实例对应一条有顺序的字节流。收到新数据后按顺序调用 `Append` 即可，未完成的数据会保留到下一次继续处理。

如果输入中有噪声、非法长度或校验失败，解析器会把这类情况放到 `Diagnostics` 中，然后继续尝试寻找后面的帧：

```csharp
foreach (ParserDiagnostic diagnostic in result.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
}
```

## 校验器

不需要校验时可以使用 `NoValidation.Instance`。如果协议使用一字节 XOR 校验：

```csharp
var validator = new XorChecksumValidator(dataStartOffset: 2);
```

如果协议使用 CRC16-Modbus：

```csharp
var validator = new Crc16ModbusValidator(
    dataStartOffset: 2,
    checksumOffsetFromEnd: 2,
    checksumByteOrder: ByteOrder.LittleEndian);
```

校验器只检查已经收齐的候选帧，不负责缓存和拆包。校验失败不会直接让整个字节流停止。

## 使用时注意

- 一个解析器实例只对应一条有顺序的数据流。
- `PacketParser` 是同步类型，不保证线程安全。
- 多条连接或多台设备应分别创建解析器实例。
- `MaxFrameLength` 和 `MaxBufferedBytes` 用来限制异常输入占用的内存。
- 库不负责串口、TCP、BLE 或 USB 连接，也不负责重试、请求应答和设备状态机。

## 构建和测试

```shell
dotnet restore StreamPacketParser.sln
dotnet build StreamPacketParser.sln --configuration Release
dotnet test StreamPacketParser.sln --configuration Release
```

仓库中带有一个不依赖真实设备的 Console 示例：

```shell
dotnet run --project samples/StreamPacketParser.ConsoleSample
```

更具体的协议和错误恢复说明可以查看 `docs` 目录。

## English

StreamPacketParser.NET is a small C# library for splitting a continuous byte stream into complete binary protocol frames. It is useful for data received from serial ports, TCP connections, USB, BLE, or similar transports.

The library only handles packet parsing. Opening connections, sending commands, retries, and device-specific logic are outside its scope.

The current project targets `.NET 10`. The package version is `3.6.1`.

### Installation

After the package is published on NuGet, install it with:

```shell
dotnet add package StreamPacketParser --version 3.6.1
```

Or add this to the project file:

```xml
<PackageReference Include="StreamPacketParser" Version="3.6.1" />
```

Before the package is published, clone the repository and add a project reference to `src/StreamPacketParser/StreamPacketParser.csproj`.

### Features

- Handles partial frames and multiple frames in one input chunk
- Finds frames by a fixed header and a configurable length field
- Supports big-endian and little-endian lengths
- Skips noise before a valid frame header
- Recovers after invalid lengths or checksum failures
- Includes XOR and CRC16-Modbus validators
- Keeps frame and buffer sizes within configured limits
- Reports recoverable parsing problems through diagnostics

### Basic example

```csharp
var options = PacketParserOptions.CreateLengthFieldProtocol(
    header: new byte[] { 0xAA, 0x55 },
    lengthFieldOffset: 4,
    lengthFieldSize: 2,
    byteOrder: ByteOrder.BigEndian,
    lengthMode: FrameLengthMode.PayloadLength,
    fixedFrameOverhead: 8,
    minFrameLength: 8,
    maxFrameLength: 4096,
    maxBufferedBytes: 8192,
    payloadOffset: 6);

var parser = new PacketParser(options);
PacketParseResult result = parser.Append(receivedBytes);
```

Use one parser instance for each ordered byte stream. The parser keeps incomplete data until more bytes are appended. A parser instance should not be used by multiple threads at the same time.

See the `docs` directory and the Console sample for more details.

## License

MIT
