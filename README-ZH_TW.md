[简体中文](README.md) | [English](README-EN.md) | **繁體中文**

# WZBC-Launcher

**NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.**

本專案是基於 SHOU-PCL / PCL-CE / PCL 的獨立第三方啟動器，由 **[Dr-jesper](https://github.com/Dr-jesper) 與 Codex** 共同維護，維護者負責發布與支援。校名僅說明社群背景，本專案未聲稱獲得溫州商學院官方背書。

**專案支援及私人安全聯絡：[1175234170@qq.com](mailto:1175234170@qq.com)** · [隱私說明](PRIVACY.md) · [使用條款](TERMS.md) · [安全回報](SECURITY.md)

> **混合授權，整個儲存庫並非全部適用 Apache-2.0。** 啟動器與介面目錄保留 PCL 分發有限許可及附帶指南；其他程式碼及第三方素材按各自條款處理。詳見 [LICENSING.md](LICENSING.md)。
>
> **新的二進位公開發行暫緩。** 名稱、主題及發行方式的單獨核對集中在[授權核對紀錄](docs/wzbc/PCL-LICENSE-REVIEW.md)。

## 功能與目前狀態（2026-10-25）

保留帳號管理、遊戲下載與實例安裝、模組依賴及啟動能力，加入 WZBC 標誌、校園復建輪播與社團入口。WZBC、奶蛙配色、奶蛙黃支援深淺模式，更新保留使用者選擇。功能清單不代表所有真實操作均已驗收。

- Microsoft 應用程式註冊及 Azure 首頁、隱私、條款、徽標資料已完成；**尚未取得 Minecraft Services 授權**。兩輪申請未通過，具體原因未確認。自有 Client ID 實測至 XSTS 成功，Minecraft 換票回傳 HTTP 403。
- [統一帳號准入](docs/wzbc/ACCOUNT-ADMISSION.md)沿用上游三條身分路徑，不新增 Microsoft 綁定。Microsoft 路徑檢查遊戲權益；皮膚站及離線角色不證明 Microsoft 遊戲所有權，不能據此聲稱滿足全部 AppID 要求。
- [v1.2.0-wzbc.2](https://github.com/Dr-jesper/WZBC-Launcher/releases/tag/v1.2.0-wzbc.2) 為歷史預覽版，未包含本輪修復，不代表已完成整改或通過 Mojang 審核。
- 目前審核原始碼版本為 1.2.0-wzbc.3，已納入認證階段提示、失效工作階段阻斷、帳號快取隔離及啟動身分複核。408 項本地離線測試通過，Windows x64 Beta/Debug 編譯通過。Actions 使用自有公開 Client ID 進行 BuildOnly，僅上傳測試報告。
- 完整真實登入、權杖更新、遊戲下載、啟動與入服仍待驗收。[審核版本入口](docs/wzbc/REVIEW.md)集中列出證據與待辦；同步原始碼不代表再次提交申請。

[社團官網](https://wzbcmc.cn/) · [皮膚站](https://skin.wzbcmc.cn/) · [建置說明](docs/wzbc/BUILD.md)

發行套件不附帶 Minecraft 遊戲、修改後的遊戲檔案、玩家帳號或社團整合包。使用者自行操作下載，並遵守相關服務條款。

## 來源與致謝

PCL 原作者：[龍騰貓躍](https://github.com/Meloong-Git/PCL)（[贊助](https://ifdian.net/a/LTCat)）。感謝 [PCL Community](https://github.com/PCL-Community/PCL-CE) 及 [SHOU-PCL / Conduit-Club](https://github.com/Conduit-Club/SHOU-PCL)（原社團版貢獻 Moeary / Codex）。

原始基線為 SHOU-PCL main 的 842393281c5ed8bb5745efc37ce775bd0a21f4a1（v1.2.0 / zyf）。來源儲存庫已更名為 SHOU-Launcher，本輪維持原基線；保留[歷史與原始聲明](docs/conduit/HISTORY.md)。
