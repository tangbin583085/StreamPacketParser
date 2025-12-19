# 发布检查清单

准备把 `StreamPacketParser.NET` 发布到GitHub或NuGet前，按下面顺序检查：

- [ ] 在具备.NET 10 SDK的环境中执行 `dotnet restore`。
- [ ] 执行 `dotnet build StreamPacketParser.sln --configuration Release`。
- [ ] 执行 `dotnet test StreamPacketParser.sln --configuration Release`。
- [ ] 执行 `dotnet pack src/StreamPacketParser/StreamPacketParser.csproj --configuration Release`。
- [ ] 确认NuGet包名 `StreamPacketParser` 尚未被占用。
- [x] 仓库地址已设置为 `tangbin583085/StreamPacketParser`。
- [x] NuGet作者已设置为 `tangbin`。
- [x] `LICENSE`中的版权所有者已设置为 `tangbin`。
- [ ] 最后再检查版权和许可证展示方式。
- [ ] 检查MIT License和仓库可见性。
- [ ] 解压生成的 `.nupkg`，确认README和项目元数据都在包内。
- [ ] 确认没有包含公司私有协议、账号密码、真实设备数据或构建产物。
- [ ] 创建与包版本一致的Git tag，初始版本为 `v0.1.0`。
