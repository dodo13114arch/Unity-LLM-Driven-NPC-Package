using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIServices.Interfaces;

namespace AIServices.Core
{
    /// <summary>
    /// NPC 對話管理器 - 協調 STT、LLM 和 TTS 服務
    /// </summary>
    public class NpcInteractionManager : MonoBehaviour
    {
        [Header("服務組件")]
        [SerializeField] private MonoBehaviour sttServiceComponent;
        [SerializeField] private MonoBehaviour llmServiceComponent;
        [SerializeField] private MonoBehaviour ttsServiceComponent;
        
        [Header("Ready Player Me 設定")]
        [SerializeField] private ReadyPlayerMe.Core.VoiceHandler voiceHandler;
        
        [Header("Debug UI")]
        [SerializeField] private bool enableDebugUI = true;
        
        // 服務介面
        private ISttService _sttService;
        private ILlmService _llmService;
        private ITtsService _ttsService;
        
        // UI 元素（改為私有字段）
        private TMP_InputField _inputField;
        private Button _sendButton;
        private TextMeshProUGUI _outputText;
        private ScrollRect _scrollRect;
        
        // 狀態
        private bool _isProcessing = false;
        
        private void Awake()
        {
            // 獲取服務介面
            _sttService = sttServiceComponent as ISttService;
            _llmService = llmServiceComponent as ILlmService;
            _ttsService = ttsServiceComponent as ITtsService;
            
            // 檢查服務是否有效
            if (_sttService == null || _llmService == null || _ttsService == null)
            {
                Debug.LogError("一個或多個服務組件未實現所需的介面！");
                enabled = false;
                return;
            }
            
            // 訂閱 STT 事件
            _sttService.OnTranscriptionResult += HandleTranscriptionResult;
            _sttService.OnError += HandleSttError;
        }
        
        // 新增方法：設置 UI 元素
        public void SetUIElements(TMP_InputField inputField, Button sendButton, TextMeshProUGUI outputText, ScrollRect scrollRect)
        {
            // 清除舊的事件監聽器（如果存在）
            if (_inputField != null)
            {
                _inputField.onSubmit.RemoveAllListeners();
            }
            
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveAllListeners();
            }
            
            // 設置新的 UI 元素引用
            _inputField = inputField;
            _sendButton = sendButton;
            _outputText = outputText;
            _scrollRect = scrollRect;
            
            // 如果 UI 元素已設置，則立即設置 UI
            if (_inputField != null && _sendButton != null)
            {
                Debug.Log("UI 元素已設置，初始化 Debug UI");
                SetupDebugUI();
            }
            else
            {
                Debug.LogWarning("一個或多個 UI 元素未設置");
            }
        }
        
        private void SetupDebugUI()
        {
            if (!enableDebugUI) return;
            
            // 檢查 UI 元素是否存在
            if (_inputField == null || _sendButton == null)
            {
                Debug.LogWarning("Debug UI 元素未設置，文字輸入測試功能將不可用");
                return;
            }
            
            Debug.Log("設置 UI 事件監聽器");
            
            // 先移除所有可能的舊事件
            _sendButton.onClick.RemoveAllListeners();
            _inputField.onSubmit.RemoveAllListeners();
            _inputField.onEndEdit.RemoveAllListeners(); // 添加 onEndEdit 事件
            
            // 設置按鈕事件
            _sendButton.onClick.AddListener(() => {
                Debug.Log("發送按鈕被點擊");
                ProcessUserInput();
            });
            
            // 設置輸入欄位的提交事件
            _inputField.onSubmit.AddListener((text) => {
                Debug.Log("輸入欄位提交: " + text);
                ProcessUserInput();
            });
            
            // 添加 Enter 鍵處理
            _inputField.onEndEdit.AddListener((text) => {
                Debug.Log("輸入欄位編輯完成: " + text);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ProcessUserInput();
                }
            });
            
            // 輸出初始訊息
            AppendToOutput("<color=#0F9D58>System:</color> Debug UI initialized. Type a message and press Enter or click Send.");
            
            // 確保輸入欄位可以接收輸入
            _inputField.ActivateInputField();
            _inputField.Select();
        }
        
        // 添加新方法處理用戶輸入
        private void ProcessUserInput()
        {
            if (string.IsNullOrEmpty(_inputField.text)) return;
            
            string userInput = _inputField.text;
            _inputField.text = "";
            
            // 顯示用戶輸入
            AppendToOutput($"<color=#4285F4>User:</color> {userInput}");
            
            // 處理用戶輸入
            HandleUserInput(userInput);
            
            // 防止輸入欄位失去焦點
            StartCoroutine(RefocusInputField());
        }
        
        private IEnumerator RefocusInputField()
        {
            yield return new WaitForEndOfFrame();
            if (_inputField != null)
            {
                _inputField.ActivateInputField();
                _inputField.Select();
                Debug.Log("重新聚焦輸入欄位");
            }
            else
            {
                Debug.LogWarning("輸入欄位為空，無法重新聚焦");
            }
        }
        
        private void AppendToOutput(string message)
        {
            if (_outputText == null) return;
            
            _outputText.text += (string.IsNullOrEmpty(_outputText.text) ? "" : "\n") + message;
            
            // 滾動到底部
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void Update()
        {
            // 檢測按鍵輸入
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!_sttService.IsListening && !_isProcessing)
                {
                    _sttService.StartListening();
                    Debug.Log("開始錄音...");
                    if (enableDebugUI && _outputText != null)
                    {
                        AppendToOutput("<color=#FFA500>系統:</color> 開始錄音...");
                    }
                }
            }
            
            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (_sttService.IsListening)
                {
                    _sttService.StopListening();
                    Debug.Log("停止錄音，處理中...");
                    if (enableDebugUI && _outputText != null)
                    {
                        AppendToOutput("<color=#FFA500>系統:</color> 停止錄音，處理中...");
                    }
                }
            }
        }
        
        /// <summary>
        /// 處理用戶輸入（從 UI 或控制台）
        /// </summary>
        public void HandleUserInput(string userInput)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("系統正在處理中，請稍後再試");
                if (enableDebugUI && _outputText != null)
                {
                    AppendToOutput("<color=#FFA500>系統:</color> 正在處理中，請稍後再試");
                }
                return;
            }
            
            _isProcessing = true;
            
            // 發送到 LLM
            _llmService.SendMessage(userInput, HandleLlmResult, HandleLlmError);
        }
        
        /// <summary>
        /// 處理 STT 結果
        /// </summary>
        private void HandleTranscriptionResult(string transcript)
        {
            Debug.Log($"STT 結果: {transcript}");
            
            if (enableDebugUI && _outputText != null)
            {
                AppendToOutput($"<color=#4285F4>用戶:</color> {transcript}");
            }
            
            // 檢查轉錄結果是否有效
            if (string.IsNullOrWhiteSpace(transcript))
            {
                Debug.Log("忽略空的語音識別結果");
                _isProcessing = false; // 重置處理狀態
                return;
            }
            
            // 處理用戶輸入
            HandleUserInput(transcript);
        }
        
        /// <summary>
        /// 處理 LLM 結果
        /// </summary>
        private void HandleLlmResult(string llmResponse)
        {
            Debug.Log($"LLM 回應: {llmResponse}");
            
            if (enableDebugUI && _outputText != null)
            {
                AppendToOutput($"<color=#0F9D58>AI:</color> {llmResponse}");
            }
            
            // 發送到 TTS
            _ttsService.Speak(llmResponse, HandleTtsAudioClip, HandleTtsError);
        }
        
        /// <summary>
        /// 處理 TTS 結果
        /// </summary>
        private void HandleTtsAudioClip(AudioClip clip)
        {
            Debug.Log("TTS 音頻準備就緒");
            
            // 播放音頻
            if (voiceHandler != null && voiceHandler.AudioSource != null)
            {
                voiceHandler.AudioSource.Stop();
                voiceHandler.AudioSource.clip = clip;
                voiceHandler.AudioSource.Play();
            }
            else
            {
                Debug.LogWarning("VoiceHandler 或其 AudioSource 未設置");
            }
            
            // 重置處理狀態
            _isProcessing = false;
        }
        
        /// <summary>
        /// 處理 STT 錯誤
        /// </summary>
        private void HandleSttError(string error)
        {
            Debug.LogError($"STT 錯誤: {error}");
            
            if (enableDebugUI && _outputText != null)
            {
                AppendToOutput($"<color=#DB4437>錯誤:</color> STT 錯誤: {error}");
            }
            
            _isProcessing = false;
        }
        
        /// <summary>
        /// 處理 LLM 錯誤
        /// </summary>
        private void HandleLlmError(string error)
        {
            Debug.LogError($"LLM 錯誤: {error}");
            
            if (enableDebugUI && _outputText != null)
            {
                AppendToOutput($"<color=#DB4437>錯誤:</color> LLM 錯誤: {error}");
            }
            
            _isProcessing = false;
        }
        
        /// <summary>
        /// 處理 TTS 錯誤
        /// </summary>
        private void HandleTtsError(string error)
        {
            Debug.LogError($"TTS 錯誤: {error}");
            
            if (enableDebugUI && _outputText != null)
            {
                AppendToOutput($"<color=#DB4437>錯誤:</color> TTS 錯誤: {error}");
            }
            
            _isProcessing = false;
        }
        
        private void OnDestroy()
        {
            // 取消訂閱事件
            if (_sttService != null)
            {
                _sttService.OnTranscriptionResult -= HandleTranscriptionResult;
                _sttService.OnError -= HandleSttError;
            }
            
            // 清理 UI 事件
            if (enableDebugUI && _sendButton != null)
            {
                _sendButton.onClick.RemoveAllListeners();
            }
            
            if (enableDebugUI && _inputField != null)
            {
                _inputField.onSubmit.RemoveAllListeners();
            }
        }
    }
}
