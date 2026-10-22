# Security reporting

Maintainers: **Dr-jesper and Codex**. Private contact: **[1175234170@qq.com](mailto:1175234170@qq.com)**.

Please report account, token, authentication, local-storage, unsafe-download or credential-disclosure issues privately. Include the source commit or version, operating system, reproduction steps, expected behavior and a minimal example using dummy values. Maintainers have no guaranteed response-time or security-support SLA.

**Do not put live passwords, tokens, device/user authorization codes, account databases, config.v1.json, UserKey.bin, unredacted logs or personal account identifiers in public issues, pull requests or screenshots.** Do not send such secrets even to the security mailbox; describe the issue with synthetic data instead.

The published v1.2.0-wzbc.1 / .2 previews do not include the 2026-10-23 source repairs. Current source adds full supported-field redaction, a log-drain fix, a no-collector telemetry guard and authentication regression tests. Those tests are not an independent security audit or proof of real-account acceptance.

If a credential was exposed, revoke the affected sessions/tokens with the provider and change a compromised password through the provider's official site. Deleting a local profile alone does not revoke credentials. Review local logs and backups; see [PRIVACY.md](PRIVACY.md).

Minecraft Services authorization remains unapproved. Offline and third-party accounts are distinct from Microsoft entitlement-verified sign-in. Current behavior and remaining licensing/ownership issues are documented in [APPID-REVIEW.md](docs/wzbc/APPID-REVIEW.md) and [LICENSING.md](LICENSING.md).
