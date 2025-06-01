# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-01

### Added
- 🎯 **統一介面設計** - ITtsService、ISttService、ILlmService抽象介面
- 🗣️ **文字轉語音服務**
  - Google Text-to-Speech integration
  - OpenAI TTS integration
  - HuggingFace TTS integration
- 👂 **語音轉文字服務**
  - Google Speech-to-Text integration
- 🤖 **大型語言模型服務**
  - OpenAI GPT-4 integration
  - Google Gemini integration
  - Ollama local model integration
- 🌍 **多語言語音配置** - 10種語言的預設語音配置
  - 繁體中文 (zh-TW)
  - 美式英語 (en-US)
  - 英式英語 (en-GB)
  - 德語 (de-DE)
  - 西班牙語 (es-ES)
  - 法語 (fr-FR)
  - 義大利語 (it-IT)
  - 波蘭語 (pl-PL)
  - 俄語 (ru-RU)
  - 烏克蘭語 (uk-UA)
- 🔧 **核心管理系統**
  - NpcInteractionManager - 統一的NPC互動管理器
  - 模組化設計支援服務熱插拔
- 📦 **Unity Package格式** - 標準的Unity Package Manager支援
- 📚 **完整文檔** - README、API文檔、使用範例

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