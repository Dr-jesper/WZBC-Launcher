# WZBC-Launcher privacy and account-data notice

Updated 2026-10-23. Maintainers: Dr-jesper and Codex. Contact: [1175234170@qq.com](mailto:1175234170@qq.com).
NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.

本文件说明当前代码的数据处理事实，不代表 Minecraft API 已批准或真实登录已验收。

## Accounts and local storage

- Microsoft sign-in uses Microsoft's device authorization pages. The launcher requests OAuth access/refresh tokens, Xbox Live/XSTS tokens, Minecraft Services credentials, game-entitlement results and a Java profile. It does not ask for a Microsoft password in the launcher.
- Offline profiles hold a local name/UUID and do not prove game ownership. Third-party Yggdrasil/skin-site login sends the username and password you enter to the selected authentication provider; that provider has its own privacy and security terms.
- Stored profiles may contain access, refresh, ID and client tokens, profile identifiers, authentication-provider metadata and raw provider response data. Imported/legacy third-party profiles may retain a password for their first verification. Do not reuse a Microsoft password at a skin site.
- Profiles are persisted through SharedEncrypt in %APPDATA%/WZBC-Launcher/config.v1.json. A per-user key, protected with Windows DPAPI, is stored as UserKey.bin. The content cipher is selected from the supported AES-GCM/ChaCha providers. This is protection at rest, not protection from software running as your Windows user, a compromised machine, or copies already shared elsewhere.
- Debug builds use WZBC-Launcher_Debug. Caches also use %LOCALAPPDATA%/WZBC-Launcher and %TEMP%/WZBC-Launcher. The executable directory's PCL/ folder stores local settings and logs.
- This notice is specifically about profiles. Do not assume every setting is encrypted: for example, inherited custom proxy credential settings have different storage behavior. Review configuration before sharing it.

## Requests and other recipients

Network requests are caused by the features you use: Microsoft identity/Xbox/Minecraft services; WZBC Skin, MUA, LittleSkin or a custom provider; game/Java/mod downloads and selected mirrors; community websites; server-status probes; GitHub release checks; and DNS/DoH services. Providers receive network metadata such as your IP address and the data required for that request. A complete immutable domain allowlist is not possible for user-selected providers, mirrors and servers.

The launcher's own profile data is not intentionally sent to the project-maintainer mailbox. A support report sends only what you choose to include. Websites opened in a browser use the browser's own cookies and privacy settings.

## Telemetry and logs

Telemetry is an inherited optional feature, disabled by the configuration default. When enabled and a Sentry collector is configured, it can report exceptions and device/environment information, including a launcher identifier, OS/build, architecture, memory, NAT/IPv6 status and presence of certain other launchers. Current WZBC publication scripts do not embed a Sentry DSN. The repaired code stops before device/network probing if no collector is configured. Custom builds may differ; changing telemetry settings does not recall data already sent.

New source repairs fully mask supported credential fields in the central file logger and known launch credentials before UI log display, rather than keeping token prefixes/suffixes. This is best-effort redaction, not a guarantee that arbitrary game output, custom commands, third-party error text, UUIDs, paths, server addresses or manually pasted secrets are anonymous. Historical preview packages do not contain these repairs. Inspect logs and crash bundles before sharing; never upload account/config/key files.

## Removal, revocation and retention

Remove an account in the launcher's account list to delete that saved profile locally. Removal does not revoke a provider's tokens or erase its server-side data. Revoke application access or sessions with Microsoft/the selected provider if needed.

For a full local reset, close the launcher and remove its WZBC shared data/cache/temp folders and executable-local PCL/ settings/logs only after preserving files you need. Removing UserKey.bin makes old encrypted profile copies unreadable. Deletion of settings is not secure erasure and does not remove backups, cloud-sync copies, exported logs or third-party records. Do not delete Minecraft worlds or instance folders merely to remove a login.

The project does not promise automatic deletion from copies outside its control. For provider-held data, use that provider's account/privacy tools. For privacy questions or a report you voluntarily sent to maintainers, contact 1175234170@qq.com.
