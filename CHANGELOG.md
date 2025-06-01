# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.0]: https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package/releases/tag/v1.0.0 