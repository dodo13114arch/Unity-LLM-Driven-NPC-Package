# AI NPC Services for Unity

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-black.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Ready Player Me](https://img.shields.io/badge/Ready%20Player%20Me-Compatible-blue.svg)](https://readyplayer.me/)

一個為Unity設計的AI驅動NPC對話包，完美整合Ready Player Me。只需三個簡單步驟就能讓您的NPC擁有語音對話能力。

## 主要功能

- **即插即用** - 三步驟完成AI NPC設置
- **多種語音服務** - Google TTS、OpenAI TTS、HuggingFace TTS
- **語音辨識** - Google Speech-to-Text
- **智能對話** - GPT-4、Gemini、Ollama本地模型
- **多語言支援** - 內建10種語言語音配置
- **Ready Player Me整合** - 完美支援RPM頭像系統

## 安裝

### Unity Package Manager (推薦)
1. 開啟Unity，進入 `Window` > `Package Manager`
2. 點擊 `+` → `Add package from git URL`
3. 輸入：`https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package.git`

### 手動安裝
下載ZIP檔案並解壓到專案的 `Packages/` 資料夾

## 快速設置

### 步驟1：準備Ready Player Me NPC
1. 前往 [Ready Player Me](https://readyplayer.me/) 創建或載入NPC模型
2. 將RPM頭像匯入到Unity場景中

### 步驟2：場景設置
1. **創建NPC Manager**：在場景中創建空GameObject，命名為 "NPC Manager"
2. **添加核心組件**：將 `NpcInteractionManager.cs` 拖到NPC Manager上
3. **設置AI服務**：根據需要添加以下服務：

**LLM服務 (選一個)**：
- `OpenAILlmService` (GPT-4)
- `GeminiLlmService` (Google Gemini)  
- `OllamaLlmService` (本地模型) *[未測試，僅供參考]*

**TTS服務 (選一個)**：
- `GoogleTtsService`
- `OpenAITtsService`
- `HuggingFaceTtsService` *[未測試，僅供參考]*

**STT服務 (可選)**：
- `GoogleSttService`

### 步驟3：配置API密鑰
在Inspector中輸入對應服務的API Key：
- **OpenAI**: 需要OpenAI API密鑰
- **Google**: 需要Google Cloud API密鑰
- **Ollama**: 確保本地Ollama服務運行

然後在 `NpcInteractionManager` 的Inspector中將對應的服務拖入對應欄位。

## 使用方法

```csharp
// 發送文字訊息給NPC
npcManager.ProcessUserInput("你好！");
```

## 支援的AI服務

| 服務類型 | 支援的服務商 | 描述 |
|---------|-------------|------|
| **LLM** | OpenAI GPT-4, Google Gemini, Ollama* | 智能對話生成 |
| **TTS** | Google TTS, OpenAI TTS, HuggingFace* | 文字轉語音 |
| **STT** | Google Speech-to-Text | 語音轉文字 |

*\*標註的服務未經測試，僅供參考*

## 多語言支援

內建語音配置：繁體中文、美式英語、英式英語、德語、西班牙語、法語、義大利語、波蘭語、俄語、烏克蘭語

## API密鑰設置

### OpenAI
1. 前往 [OpenAI Platform](https://platform.openai.com/)
2. 建立API密鑰
3. 在Inspector中貼上密鑰

### Google Cloud
1. 前往 [Google Cloud Console](https://console.cloud.google.com/)
2. 啟用Text-to-Speech和Speech-to-Text API
3. 建立服務帳戶密鑰
4. 在Inspector中貼上API密鑰

### Ollama (本地) *[未測試]*
1. 安裝 [Ollama](https://ollama.ai/)
2. 運行本地模型：`ollama run llama2`
3. 確保服務在 `http://localhost:11434` 運行

## 系統需求

- **Unity 2022.3+** (測試於Unity 6)
- **Ready Player Me SDK** (選用，用於頭像整合)
- **網路連線** (用於雲端AI服務)

## 自訂服務

所有服務都基於統一介面設計，支援自訂實現：

```csharp
public class MyTtsService : MonoBehaviour, ITtsService
{
    public void Speak(string text, Action<AudioClip> onReady, Action<string> onError)
    {
        // 您的實現
    }
}
```

## 授權

MIT授權 - 請查看 [LICENSE.md](LICENSE.md)

## 致謝

本專案受到 [@UnityGameStudio](https://github.com/UnityGameStudio) 的 [Gemini-Unity-Google-Cloud](https://github.com/UnityGameStudio/Gemini-Unity-Google-Cloud) 專案啟發。我們在其基礎架構上進行了重構和抽象化，設計出統一的介面系統，讓開發者能夠更靈活地整合多種AI服務。

### 從原專案到本Package的演進

1. **抽象化設計**: 將特定服務實現抽象為統一介面 (ITtsService, ILlmService, ISttService)
2. **多服務支援**: 擴展支援OpenAI、Ollama、HuggingFace等多種AI服務
3. **Package化**: 重構為標準Unity Package格式，便於分發和維護
4. **多語言優化**: 增強了多語言語音配置和國際化支援
5. **完整文檔**: 提供詳細的使用指南和API文檔

---

如果這個Package對您有幫助，請給我們一個Star！ 