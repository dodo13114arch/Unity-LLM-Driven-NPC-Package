# AI NPC Services for Unity

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-black.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/dodo13114arch/Unity-LLM-Driven-NPC-Package)

一個Unity AI對話系統，讓NPC能夠語音對話。支援Google、OpenAI、ElevenLabs、Mistral等多種AI服務，需搭配Ready Player Me使用。

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
- `MistralLlmService` - 使用Mistral AI

**語音合成（選一個）**：
- `GoogleTtsService` - Google語音
- `OpenAITtsService` - OpenAI語音
- `ElevenLabsTtsService` - ElevenLabs高品質語音

**語音識別（可選）**：
- `GoogleSttService` - Google語音轉文字
- `OpenAIWhisperSttService` - OpenAI Whisper高精度語音識別

### 步驟3：配置API密鑰
在Inspector中填入API密鑰：
- **OpenAI**：到 [OpenAI](https://platform.openai.com/) 取得
- **Google**：到 [Google Cloud](https://console.cloud.google.com/) 取得
- **Gemini**：到 [AI Studio](https://aistudio.google.com/apikey) 取得
- **ElevenLabs**：到 [ElevenLabs](https://elevenlabs.io/) 取得
- **Mistral**：到 [Mistral AI](https://console.mistral.ai/) 取得

### 步驟4：連接服務
在 `NpcInteractionManager` 的Inspector中，將對應服務拖入對應欄位。

## 支援的服務

| 服務 | TTS | STT | LLM | 狀態 |
|------|-----|-----|-----|------|
| Google | ✅ | ✅ | ✅ | 完整支援 |
| OpenAI | ✅ | ✅ | ✅ | 完整支援 |
| ElevenLabs | ✅ | ❌ | ❌ | TTS高品質語音 |
| Mistral | ❌ | ❌ | ✅ | 多語言LLM支援 |
| Ollama | ❌ | ❌ | ⚠️ | 尚未測試 |
| HuggingFace | ⚠️ | ❌ | ⚠️ | 尚未測試 |

## 新增功能

### ElevenLabs TTS 服務
- **高品質語音合成**：支援ElevenLabs的先進語音技術
- **多種聲音選項**：可配置不同的Voice ID
- **直接MP3處理**：原生支援MP3格式，高效率音頻處理
- **智能緩存系統**：減少重複請求，提升效能
- **完整錯誤處理**：重試機制與詳細錯誤回饋

### OpenAI Whisper STT 服務
- **高精度語音識別**：使用最新的Whisper模型
- **多語言支援**：支援繁體中文等多種語言
- **智能音頻驗證**：自動過濾無效音頻
- **WAV格式編碼**：完整的音頻處理管線
- **可配置參數**：支援語言、模型、溫度等參數調整

### Mistral LLM 服務
- **多種模型支援**：支援mistral-small-latest、mistral-large-latest等
- **對話歷史管理**：智能管理對話上下文
- **動態系統提示詞**：運行時可修改系統設定
- **安全模式**：內建內容過濾功能
- **完整參數控制**：溫度、token限制、懲罰參數等

## 系統需求

- Unity 2022.3+
- 網路連線（雲端AI服務）

## 授權

MIT - 詳見 [LICENSE.md](LICENSE.md)

---

如果有幫助請給個Star！ ⭐  