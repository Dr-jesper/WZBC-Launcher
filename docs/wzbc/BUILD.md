# WZBC-Launcher 构建

需要 Windows 和 .NET 10 SDK。预览版使用 Beta 配置；首轮本地构建 SDK 为 10.0.402。来源为 SHOU-PCL main 842393281c5ed8bb5745efc37ce775bd0a21f4a1，来源版本 1.2.0 / zyf，WZBC 预览版为 1.2.0-wzbc.2。

```powershell
# 开发编译
dotnet build 'Plain Craft Launcher 2/Plain Craft Launcher 2.csproj' -c Beta -p:Platform=x64 -p:UseSharedCompilation=false
# 离线回归测试
dotnet test PCL.Core.Test/PCL.Core.Test.csproj -c Beta -p:Platform=x64 --filter 'TestCategory!=Integration' --blame-hang-timeout 2m
# Windows x64 自包含发行包；每次使用尚不存在的输出目录
./scripts/conduit/Publish.ps1 -OutputRoot artifacts/wzbc
```

自定义 SDK 路径可通过 -Dotnet 'path/to/dotnet.exe' 指定。脚本支持 -Architecture ARM64，但本预览版只验证 Windows x64。

输出 ZIP 和旁置 SHA-256。包内只有启动器 EXE、LICENSE、LICENCE、BUILD.md、PENDING.md 与 BUILD-INFO.txt；构建信息包含版本、源码提交、工作区状态、SDK、架构和配置。使用干净的已提交源码构建发行附件。源码中不提交构建输出、运行目录、账户、日志或原始参考图。

## 配置

- Microsoft 登录需要项目自己的公开应用 Client ID 和可用的 Minecraft API 权限。按 .env.example 设置 PCL_MS_CLIENT_ID 后重建，或使用现有配置项。当前 Client ID 申请中，未配置时界面明确不可用。
- 构建脚本仅接收公开 Microsoft Client ID 和源码提交信息，隔离其他 PCL_* 环境变量；不嵌入其他服务凭据，不执行 .env 文件内容。
- WZBC 发行源为 https://github.com/Dr-jesper/WZBC-Launcher，仅手动检查版本并打开下载页，不自动替换 EXE，不回退到 SHOU-PCL 发行包。
- 官网更新日志源尚未启用；接入时需同时核对 ClubCatalog.UpdateNoticesReady 与 ClubUpdateNoticeParser 的可信域名和格式。
- 服务器整合包、账户和外部服务授权不包含在发行包内。

## GitHub 构建

.github/workflows/wzbc-build.yml 在本仓库的 main、dev、WZBC-Launcher 推送或 PR 时构建，亦可手动运行。它使用只读仓库权限，构建 Windows x64 包并运行离线测试，上传 ZIP、SHA-256 和测试结果；不自动创建 Release。可在仓库 Actions 配置 CLIENT_ID，申请完成前留空。

历史 docs/conduit/ 描述 SHOU-PCL，不代表 WZBC 已配置对应服务。功能验收见 [VALIDATION.md](VALIDATION.md)，已知限制见 [PENDING.md](PENDING.md)。
