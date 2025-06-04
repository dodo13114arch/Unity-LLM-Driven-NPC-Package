# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-01-22

### Added
- **ElevenLabs TTS服務** - 高品質語音合成服務
  - 支援ElevenLabs先進語音技術
  - 直接MP3格式處理，高效率音頻轉換
  - 智能緩存系統減少重複請求
  - 可配置Voice ID、模型、穩定性等參數
  - 完整錯誤處理與重試機制

- **OpenAI Whisper STT服務** - 高精度語音識別
  - 使用最新Whisper模型進行語音轉文字
  - 多語言支援（繁體中文、英文等）
  - 智能音頻驗證過濾無效輸入
  - WAV格式編碼完整音頻處理管線
  - 可配置語言、模型、溫度等參數

- **Mistral AI LLM服務** - 多語言對話模型
  - 支援多種Mistral模型（mistral-small-latest、mistral-large-latest等）
  - 智能對話歷史管理與上下文控制
  - 動態系統提示詞運行時修改
  - 內建安全模式與內容過濾
  - 完整參數控制（溫度、token限制、懲罰參數等）

### Enhanced
- **服務支援表** - 更新支援的AI服務矩陣
- **文檔完善** - 新增詳細的服務配置說明
- **錯誤處理** - 針對新服務添加專門的故障排除指南

### Technical Details
- 新增 `ElevenLabsTtsService` 實現 ITtsService 介面
- 新增 `OpenAIWhisperSttService` 實現 ISttService 介面  
- 新增 `MistralLlmService` 實現 ILlmService 介面
- 所有新服務都支援協程錯誤處理機制
- 統一的Unity Inspector配置介面

## [1.0.0] - 2025-06-01

### Added
- **統一介面設計** - ITtsService、ISttService、ILlmService抽象介面
- **Google AI服務** - 完整支援TTS、STT、Gemini LLM
- **OpenAI服務** - 完整支援TTS、STT、GPT LLM  
- **核心管理系統** - NpcInteractionManager統一互動管理
- **多語言語音配置** - 預設10種語言語音資產
- **Ready Player Me整合** - 專為RPM NPC設計
- **實驗性服務** - HuggingFace和Ollama基礎支援

### Dependencies
- Unity 2022.3+
- TextMeshPro 3.0.6 (自動安裝)
- Newtonsoft.Json 3.2.1 (自動安裝)

### Technical Details
- 基於UnityGameStudio/Gemini-Unity-Google-Cloud重構
- 統一的服務抽象架構
- 完整的錯誤處理和回調系統
- Unity Package Manager標準格式

[1.1.0]: https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package/releases/tag/v1.1.0
[1.0.0]: https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package/releases/tag/v1.0.0 