# 本地检查清单 / Local checks

在已有开发环境中检查：

- 核心库以 C++17 构建，不依赖 Qt。
- Debug 和 Release 均运行 CTest；测试检查不依赖 assert。
- 普通示例能从两段输入得到一帧。
- 安装到独立目录后，通过 find_package 构建一个使用方。
- Qt 5.15 和 Qt 6 工程分别检查 TCP 示例；安装 SerialPort 模块后检查串口示例。
- 使用演示设备或回环服务，检查分片、多帧、断线与重连后的状态。
- README、示例字段、版本号和 CMake 导出目标保持一致。

该清单是验证步骤，不代表已经执行或通过。

## English

Build the core with C++17, run CTest in Debug and Release, try the console example,
and verify an installed-package consumer. Check Qt TCP and serial examples in the
Qt environments you support, including reconnect/session reset behavior.

These are checks to perform, not a claim that they have passed.
