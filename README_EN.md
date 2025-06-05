# AI NPC Services for Unity

> **Language**: [English](README_EN.md) | [繁體中文](README.md)

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-black.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/dodo13114arch/Unity-LLM-Driven-NPC-Package)

A Unity AI dialogue system that enables NPCs to have voice conversations. Supports multiple AI services including Google, OpenAI, ElevenLabs, Mistral, and more. Requires Ready Player Me integration.

> **Acknowledgments**: Based on [@UnityGameStudio](https://github.com/UnityGameStudio)'s [Gemini-Unity-Google-Cloud](https://github.com/UnityGameStudio/Gemini-Unity-Google-Cloud)

## Installation

> Please ensure you have [ReadyPlayerMePackage](https://assetstore.unity.com/packages/tools/game-toolkits/ready-player-me-avatar-and-character-creator-259814) installed first

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter: `https://github.com/dodo13114arch/Unity-LLM-Driven-NPC-Package.git`

## Quick Setup

### Step 1: Create NPC Manager
1. Create an empty GameObject in your scene, name it "NPC Manager"
2. Add the `NpcInteractionManager` component

### Step 2: Add AI Services
Add the following components to the same GameObject as needed:

**Dialogue Services (choose one)**:
- `OpenAILlmService` - Use GPT
- `GeminiLlmService` - Use Google Gemini
- `MistralLlmService` - Use Mistral AI

**Text-to-Speech (choose one)**:
- `GoogleTtsService` - Google Speech
- `OpenAITtsService` - OpenAI Speech
- `ElevenLabsTtsService` - ElevenLabs High-Quality Speech

**Speech-to-Text (optional)**:
- `GoogleSttService` - Google Speech-to-Text
- `OpenAIWhisperSttService` - OpenAI Whisper High-Accuracy Speech Recognition

### Step 3: Configure API Keys
Fill in the API keys in the Inspector:
- **OpenAI**: Get from [OpenAI](https://platform.openai.com/)
- **Google**: Get from [Google Cloud](https://console.cloud.google.com/)
- **Gemini**: Get from [AI Studio](https://aistudio.google.com/apikey)
- **ElevenLabs**: Get from [ElevenLabs](https://elevenlabs.io/)
- **Mistral**: Get from [Mistral AI](https://console.mistral.ai/)

### Step 4: Connect Services
In the `NpcInteractionManager` Inspector, drag the corresponding services into their respective fields.

## Supported Services

| Service | TTS | STT | LLM | Status |
|---------|-----|-----|-----|--------|
| Google | ✅ | ✅ | ✅ | Full Support |
| OpenAI | ✅ | ✅ | ✅ | Full Support |
| ElevenLabs | ✅ | ❌ | ❌ | High-Quality TTS |
| Mistral | ❌ | ❌ | ✅ | Multilingual LLM |
| Ollama | ❌ | ❌ | ⚠️ | Not Tested |
| HuggingFace | ⚠️ | ❌ | ⚠️ | Not Tested |

## New Features

### ElevenLabs TTS Service
- **High-Quality Speech Synthesis**: Supports ElevenLabs' advanced speech technology
- **Multiple Voice Options**: Configurable Voice IDs
- **Direct MP3 Processing**: Native MP3 format support for efficient audio processing
- **Smart Caching System**: Reduces duplicate requests and improves performance
- **Complete Error Handling**: Retry mechanisms with detailed error feedback

### OpenAI Whisper STT Service
- **High-Accuracy Speech Recognition**: Uses the latest Whisper models
- **Multilingual Support**: Supports Traditional Chinese and many other languages
- **Smart Audio Validation**: Automatically filters invalid audio
- **WAV Format Encoding**: Complete audio processing pipeline
- **Configurable Parameters**: Supports language, model, temperature, and other parameter adjustments

### Mistral LLM Service
- **Multiple Model Support**: Supports mistral-small-latest, mistral-large-latest, etc.
- **Conversation History Management**: Intelligent conversation context management
- **Dynamic System Prompts**: Runtime modifiable system settings
- **Safe Mode**: Built-in content filtering features
- **Complete Parameter Control**: Temperature, token limits, penalty parameters, etc.

## System Requirements

- Unity 2022.3+
- Internet Connection (for cloud AI services)

## License

MIT - See [LICENSE.md](LICENSE.md) for details

---

If this helps, please give it a Star! ⭐ 