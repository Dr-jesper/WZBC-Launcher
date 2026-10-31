# WZBC-Launcher 构建与打包

更新：2026-10-27。Windows / .NET 10 SDK，版本 1.2.0-wzbc.4。

```powershell
# 离线回归（模拟认证服务）
dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Beta -p:Platform=x64 --filter 'TestCategory!=Integration' --blame-hang-timeout 2m
# 仅构建，不生成分发 ZIP
./scripts/conduit/Publish.ps1 -BuildOnly -OutputRoot artifacts/check
# 生成 Windows x64 自包含便携 ZIP 与 SHA-256
$env:PCL_MS_CLIENT_ID = 'b42a22d0-3ca4-450d-8389-a466092f3ff7'
./scripts/conduit/Publish.ps1 -OutputRoot artifacts/package
```

可用 `-Dotnet 'path/to/dotnet.exe'` 指定 SDK。输出目录必须尚不存在。ARM64 参数保留，当前发行目标为 x64。

## 分发内容

打包脚本严格限定 EXE、LICENSE、LICENCE、BUILD.md、PENDING.md、BUILD-INFO.txt、PRIVACY.md、SECURITY.md、LICENSING.md、TERMS.md。构建信息包含提交、源码状态、SDK、配置及公开 Client ID 是否注入。运行时账号、配置、缓存、日志、SDK 和原始素材不进入 ZIP。

脚本隔离其他 `PCL_*` 环境变量，只注入自有公开 Client ID 和源码提交；不嵌入其他项目凭据。版本说明见 [RELEASE-v1.2.0-wzbc.4.md](RELEASE-v1.2.0-wzbc.4.md)。

## CI 与手动发行

GitHub Actions 保留 BuildOnly 和完整离线测试，仅上传测试报告。公开发行由维护者手动执行：检查源码与测试、核对 ZIP 清单和 SHA-256，再上传到自己仓库的 Release 附件。自动构建不会自动发布。

Microsoft 应用已注册，Minecraft Services 尚未批准，当前 Microsoft 登录不可用。其他账号策略见 [ACCOUNT-ADMISSION.md](ACCOUNT-ADMISSION.md)。
