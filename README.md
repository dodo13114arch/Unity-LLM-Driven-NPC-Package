# AI NPC Services for Unity

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-black.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

一個Unity AI對話系統，讓NPC能夠語音對話。支援Google、OpenAI等多種AI服務，需搭配Ready Player Me使用。

> **致謝**：基於 [@UnityGameStudio](https://github.com/UnityGameStudio) 的 [Gemini-Unity-Google-Cloud](https://github.com/UnityGameStudio/Gemini-Unity-Google-Cloud) 改寫

## 安裝

1. 開啟Unity Package Manager（`Window` → `Package Manager`）
2. 點擊 `+` → `Add package from git URL...`
3. 輸入：`https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package.git`

## 快速設置

### 步驟1：創建NPC Manager
1. 場景中創建空GameObject，命名為 "NPC Manager"
2. 添加 `NpcInteractionManager` 組件

### 步驟2：添加AI服務
根據需要添加以下組件到同一個GameObject：

**對話服務（選一個）**：
- `OpenAILlmService` - 使用GPT
- `GeminiLlmService` - 使用Google Gemini

**語音合成（選一個）**：
- `GoogleTtsService` - Google語音
- `OpenAITtsService` - OpenAI語音

**語音識別（可選）**：
- `GoogleSttService` - Google語音轉文字

### 步驟3：配置API密鑰
在Inspector中填入API密鑰：
- **OpenAI**：到 [OpenAI](https://platform.openai.com/) 取得
- **Google**：到 [Google Cloud](https://console.cloud.google.com/) 取得

### 步驟4：連接服務
在 `NpcInteractionManager` 的Inspector中，將對應服務拖入對應欄位。

## 支援的服務

| 服務 | TTS | STT | LLM | 狀態 |
|------|-----|-----|-----|------|
| Google | ✅ | ✅ | ✅ | 完整支援 |
| OpenAI | ✅ | ✅ | ✅ | 完整支援 |
| Ollama | ❌ | ❌ | ⚠️ | 尚未測試 |
| HuggingFace | ⚠️ | ❌ | ⚠️ | 尚未測試 |

## 常見問題

**編譯錯誤**：確保Unity 2022.3+，檢查是否缺少依賴
**API錯誤**：檢查API密鑰是否正確，確認網路連線
**沒有聲音**：檢查AudioSource設置，確認TTS服務配置

## 系統需求

- Unity 2022.3+
- 網路連線（雲端AI服務）

## 授權

MIT - 詳見 [LICENSE.md](LICENSE.md)

---

如果有幫助請給個Star！ ⭐ 