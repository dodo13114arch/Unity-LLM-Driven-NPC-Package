# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-01-27

### Fixed
- **Critical**: Added missing `VoiceScriptableObject.cs` script that was causing compilation errors
- **Critical**: Added missing `GoogleCloudSpeechToText.cs` dependency for Google STT service
- **Critical**: Added required Assembly Definition file (`AIServices.asmdef`) for proper Unity Package Manager integration
- **Compatibility**: Made Newtonsoft.Json dependency optional with conditional compilation
- **Package Structure**: Removed non-existent samples reference from package.json

### Added
- `JsonHelper` utility class for JSON serialization with fallback support
- `GoogleCloudSpeechToText` class with complete STT API support
- Conditional compilation support for Newtonsoft.Json dependency
- Proper GUID assignment for VoiceScriptableObject to match existing .asset files

### Technical Details
- Fixed broken script references in voice configuration assets
- Restored missing dependencies from original GoogleTextToSpeech and GeminiManager packages
- Improved package compatibility for users without Newtonsoft.Json
- Enhanced Assembly Definition with version defines for better dependency management

## [1.0.0] - 2025-01-27

### Added
- Initial release of AI NPC Services package
- Complete TTS (Text-to-Speech) service implementations:
  - Google Text-to-Speech with voice configuration support
  - OpenAI TTS service with multiple voice options
  - HuggingFace TTS integration (experimental)
- STT (Speech-to-Text) service implementations:
  - Google Speech-to-Text with real-time streaming
  - OpenAI Whisper integration
- LLM (Large Language Model) service implementations:
  - Google Gemini integration with conversation memory
  - OpenAI GPT integration with customizable models
  - Ollama local LLM support (experimental)
  - HuggingFace Transformers integration (experimental)
- Unified service interfaces (`ITtsService`, `ISttService`, `ILlmService`)
- Core interaction management (`NpcInteractionManager`)
- Pre-configured voice assets for 10 languages:
  - English (US & UK)
  - Chinese Traditional (Taiwan)
  - German, French, Spanish, Italian
  - Polish, Russian, Ukrainian
- Ready Player Me compatibility
- Debug UI for testing and development

### Features
- Plug-and-play architecture with interface-based design
- Real-time voice conversation capabilities
- Conversation memory and context management
- Error handling and retry mechanisms
- Caching support for improved performance
- Extensive logging and debugging features

### Technical Specifications
- Unity 2022.3+ compatibility
- MIT License
- Modular design allowing selective service usage
- Comprehensive API documentation

### Security
- ✅ **API密鑰安全** - 所有API密鑰均透過Unity Inspector設定，無硬編碼風險
- 🔒 **依賴安全** - 只依賴Unity官方的Newtonsoft.Json包

### Technical Details
- **最低Unity版本**: 2022.3
- **支援平台**: Windows, macOS, Linux (Standalone)
- **程式碼結構**: 基於介面的模組化設計
- **錯誤處理**: 完整的異常處理和重試機制
- **效能優化**: TTS音頻快取、連線池等最佳化

### Known Issues
- 部分TTS服務在WebGL平台可能有限制
- Ollama服務需要本地運行Ollama伺服器

---

## 版本說明

### 語義化版本控制
- **主版本號 (Major)**: 不相容的API變更
- **次版本號 (Minor)**: 向下相容的功能新增
- **修訂號 (Patch)**: 向下相容的錯誤修復

### 變更類型
- **Added**: 新功能
- **Changed**: 對現有功能的變更
- **Deprecated**: 即將移除的功能
- **Removed**: 已移除的功能
- **Fixed**: 錯誤修復
- **Security**: 安全性相關變更 