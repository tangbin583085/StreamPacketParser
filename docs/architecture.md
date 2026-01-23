# 实现说明 / Architecture

## 数据路径

调用方按流顺序调用 `append`。解析器复制未处理的字节，寻找帧头，
读取无符号长度字段，等待完整候选帧，执行校验，再返回数据包。

`PacketParserOptions` 是配置值；构造解析器时复制并校验。
之后修改原配置不会修改该解析器。校验函数按值复制，但它捕获的指针或共享状态
仍由调用方管理。

核心仅依赖 C++17 标准库。Qt 与平台通信代码放在示例中。
公开头文件不包含 Qt 类型。

## 内存与生命周期

缓存用连续字节数组和起始偏移实现。丢弃前缀时移动起始位置，
需要尾部空间时才压紧数据，避免每次坏帧都移动整个数组。

返回的 `raw_data` 和 `payload` 各自持有数据副本，不引用内部缓存。
这样复制、保存、跨接收事件使用结果都不依赖解析器的生命周期。

`ByteView` 不拥有内存。传入的数据只需在 `append` 调用期间有效；
校验器收到的视图只在回调期间有效，不能保存供之后使用。

`max_buffered_bytes` 限制待处理数据的逻辑长度。容器预留容量、
帧与 Payload 副本、返回结果和诊断列表不计入这个限制。一次调用的返回数量不设上限，
长时间接收时应分块输入、及时消费结果。分配失败会以标准异常传播，
调用方可停止当前会话并重置解析器。

## 线程模型

每个解析器处理一条有序字节流。不允许并发或重入调用；
包括校验回调内再次调用该解析器。Qt 中在设备所属线程内接收和解析，
再把结果复制或移动到业务线程。

`reset()` 清空残留帧，配置不变。断线重连或更换协议时，
分别使用重置或新建解析器。

## English

Input is consumed in stream order: find the header, decode length, wait for a complete
candidate, validate it, and return an owned packet. Configuration is copied and validated
at construction. Callback captures may still refer to caller-owned state.

A sliding byte buffer avoids moving all remaining input after every rejected byte.
Both raw frame and payload are copied into the result. Input views and validator views
are borrowed only for the duration of their calls.

The pending-byte limit is not a hard process-memory limit: vector capacity, returned
packets, payload copies, and diagnostics use additional memory. Allocation failures
propagate as standard exceptions. Stop/reset the session if the application cannot continue.

Use one instance per stream, without concurrent or reentrant calls. Parse on the Qt
device's thread and transfer owned results to other threads. Reset on a new session;
construct a new parser when changing protocol settings.
