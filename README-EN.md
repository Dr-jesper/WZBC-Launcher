[简体中文](README.md) | **English** | [繁體中文](README-ZH_TW.md)

# WZBC-Launcher

**NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.**

WZBC-Launcher is an independent, source-available Windows desktop launcher derived from SHOU-PCL, PCL-CE and PCL for the WZBC community. **[Dr-jesper](https://github.com/Dr-jesper) and Codex** maintain this project and are responsible for its releases and support. The college name describes the community background; this project does not claim official college endorsement.

**Project support and private security contact: [1175234170@qq.com](mailto:1175234170@qq.com)**

[Privacy](PRIVACY.md) · [Security reporting](SECURITY.md) · [Build instructions](docs/wzbc/BUILD.md) · [AppID review status](docs/wzbc/APPID-REVIEW.md)

> **Mixed licensing: this repository is not wholly licensed under Apache-2.0.** The launcher/UI directory retains the PCL limited distribution license and accompanying guidelines. Other code and third-party assets retain their applicable terms. See [LICENSING.md](LICENSING.md).
>
> **New public binary distribution is on hold.** The maintainer has not obtained separate permission covering the current WZBC name and purple/frog themes. Publishing source or retaining notices does not resolve those permission questions.

## Features and current status — 2026-10-23

The project retains account management, game downloads and instance installation, mod dependency handling and game launch, with WZBC branding, campus screenshots, community links and selectable light/dark WZBC, Naiwa and Naiwa Yellow palettes. These are feature descriptions, not claims of real-account end-to-end acceptance.

- Microsoft application registration is complete; **Minecraft Services approval has not been granted**. The maintainer reports rejection of the AppID request on 2026-10-21. A specific rejection reason has not been established.
- The Microsoft account path checks game entitlements. Offline profiles and third-party skin-site authentication do not establish Microsoft game ownership. The current application does not enforce ownership verification on every launch path.
- [v1.2.0-wzbc.2](https://github.com/Dr-jesper/WZBC-Launcher/releases/tag/v1.2.0-wzbc.2) is a historical preview. It does not include the current source fixes and must not be interpreted as a remediated or Mojang-approved build.
- Current repair source: 1.2.0-wzbc.3. No new public binary is being published. CI still builds Windows x64 and runs offline tests, uploading test results only.
- Real sign-in, token refresh, game downloads, game launch and server admission remain unverified. See [validation](docs/wzbc/VALIDATION.md) and [remaining work](docs/wzbc/PENDING.md).

[Community website](https://wzbcmc.cn/) · [Skin site](https://skin.wzbcmc.cn/) · [WZBC repository](https://github.com/Dr-jesper/WZBC-Launcher)

Distribution archives do not bundle Minecraft itself, modified game files, player accounts or community modpacks. Downloads are initiated by users and remain subject to the applicable providers' terms.

## Attribution

PCL original author: [龙腾猫跃 / LTCat](https://github.com/Meloong-Git/PCL), with the original [donation link](https://ifdian.net/a/LTCat). Thanks to [PCL Community](https://github.com/PCL-Community/PCL-CE) and [Conduit-Club / SHOU-PCL](https://github.com/Conduit-Club/SHOU-PCL), including Moeary / Codex.

The initial source baseline is SHOU-PCL main commit 842393281c5ed8bb5745efc37ce775bd0a21f4a1 (1.2.0 / zyf). That source repository has since been renamed SHOU-Launcher; the old link redirects. This repair retains the original baseline. [Source history](docs/conduit/HISTORY.md) and original notices are preserved.
