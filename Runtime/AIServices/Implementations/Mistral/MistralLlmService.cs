using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AIServices.Interfaces;

namespace AIServices.Implementations.Mistral
{
    /// <summary>
    /// Mistral AI 大型語言模型服務實現
    /// 支援Mistral平台上的多種語言模型
    /// </summary>
    public class MistralLlmService : MonoBehaviour, ILlmService
    {
        [Header("Mistral API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string apiEndpoint = "https://api.mistral.ai/v1/chat/completions";
        [SerializeField] private string model = "mistral-small-latest";
        
        [Header("系統提示詞")]
        [SerializeField, TextArea(3, 10)] private string systemPrompt = "你是一位友善的虛擬助手，具有豐富的知識和良好的溝通能力。請用自然、親切的語氣與用戶對話，提供有用的資訊和建議。回應時請保持簡潔明瞭，避免過於冗長的說明。";
        
        [Header("生成設定")]
        [SerializeField, Range(0f, 2f)] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 150;
        [SerializeField, Range(0f, 1f)] private float topP = 1.0f;
        [SerializeField, Range(-2f, 2f)] private float presencePenalty = 0.0f;
        [SerializeField, Range(-2f, 2f)] private float frequencyPenalty = 0.0f;
        
        [Header("進階設定")]
        [SerializeField] private bool safePrompt = false;
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private int maxRetries = 3;
        
        private List<Message> _messageHistory = new List<Message>();
        
        private void Awake()
        {
            // 初始化對話歷史，添加系統提示詞
            _messageHistory.Add(new Message { role = "system", content = systemPrompt });
        }
        
        /// <summary>
        /// 發送訊息到 Mistral API
        /// </summary>
        public void SendMessage(string message, Action<string> onResult, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Mistral API密鑰未設置");
                return;
            }
            
            if (string.IsNullOrEmpty(message?.Trim()))
            {
                onError?.Invoke("消息內容不能為空");
                return;
            }
            
            // 添加用戶訊息到歷史
            _messageHistory.Add(new Message { role = "user", content = message.Trim() });
            
            // 發送請求
            StartCoroutine(SendChatRequest(onResult, onError));
        }
        
        /// <summary>
        /// 清除對話歷史
        /// </summary>
        public void ClearHistory()
        {
            _messageHistory.Clear();
            _messageHistory.Add(new Message { role = "system", content = systemPrompt });
            Debug.Log("Mistral LLM - 對話歷史已清除");
        }
        
        /// <summary>
        /// 設置系統提示詞
        /// </summary>
        public void SetSystemPrompt(string newSystemPrompt)
        {
            if (!string.IsNullOrEmpty(newSystemPrompt))
            {
                systemPrompt = newSystemPrompt;
                
                // 更新歷史中的系統提示詞
                if (_messageHistory.Count > 0 && _messageHistory[0].role == "system")
                {
                    _messageHistory[0].content = systemPrompt;
                }
                else
                {
                    _messageHistory.Insert(0, new Message { role = "system", content = systemPrompt });
                }
                
                Debug.Log($"Mistral LLM - 系統提示詞已更新: {newSystemPrompt}");
            }
        }
        
        /// <summary>
        /// 發送聊天請求到 Mistral API
        /// </summary>
        private IEnumerator SendChatRequest(Action<string> onResult, Action<string> onError)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                yield return StartCoroutine(AttemptChatRequest(onResult, onError, attempt == maxRetries - 1));
                
                if (_lastRequestSuccessful)
                {
                    yield break;
                }
                
                if (attempt < maxRetries - 1)
                {
                    Debug.LogWarning($"Mistral API 重試 {attempt + 1}/{maxRetries}");
                    yield return new WaitForSeconds(Mathf.Pow(2, attempt)); // 指數退避
                }
            }
        }
        
        private bool _lastRequestSuccessful = false;
        
        /// <summary>
        /// 嘗試發送聊天請求
        /// </summary>
        private IEnumerator AttemptChatRequest(Action<string> onResult, Action<string> onError, bool isLastAttempt)
        {
            _lastRequestSuccessful = false;
            
            // 構建請求
            ChatRequest request = new ChatRequest
            {
                model = model,
                messages = _messageHistory.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens,
                top_p = topP,
                presence_penalty = presencePenalty,
                frequency_penalty = frequencyPenalty,
                safe_prompt = safePrompt
            };
            
            string jsonRequest = null;
            byte[] bodyRaw = null;
            
            try
            {
                jsonRequest = JsonConvert.SerializeObject(request);
                bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            }
            catch (Exception e)
            {
                Debug.LogError($"Mistral API 序列化請求錯誤: {e.Message}");
                if (isLastAttempt)
                {
                    onError?.Invoke($"請求序列化錯誤: {e.Message}");
                }
                yield break;
            }
            
            using (UnityWebRequest www = new UnityWebRequest(apiEndpoint, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                www.timeout = (int)requestTimeout;
                
                Debug.Log($"發送請求到 Mistral API: {jsonRequest}");
                
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    string errorMsg = $"Mistral API 錯誤: {www.error}";
                    string responseText = www.downloadHandler?.text ?? "";
                    
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                            if (errorResponse?.error != null)
                            {
                                errorMsg += $" - {errorResponse.error.message}";
                            }
                            else
                            {
                                errorMsg += $" - 回應: {responseText}";
                            }
                        }
                        catch
                        {
                            errorMsg += $" - 回應: {responseText}";
                        }
                    }
                    
                    Debug.LogError(errorMsg);
                    
                    if (isLastAttempt)
                    {
                        onError?.Invoke($"Mistral API 請求失敗: {www.error}");
                    }
                }
                else
                {
                    string jsonResponse = www.downloadHandler.text;
                    Debug.Log($"Mistral 回應: {jsonResponse}");
                    
                    try
                    {
                        ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(jsonResponse);
                        
                        if (response != null && response.choices != null && response.choices.Length > 0)
                        {
                            string reply = response.choices[0].message.content;
                            
                            // 添加助手回應到歷史
                            _messageHistory.Add(new Message { role = "assistant", content = reply });
                            
                            // 限制歷史長度，保留系統提示詞
                            if (_messageHistory.Count > 21) // 系統提示詞 + 10對對話
                            {
                                // 保留系統提示詞和最近的對話
                                var systemMessage = _messageHistory[0];
                                var recentMessages = _messageHistory.GetRange(_messageHistory.Count - 20, 20);
                                _messageHistory.Clear();
                                _messageHistory.Add(systemMessage);
                                _messageHistory.AddRange(recentMessages);
                            }
                            
                            Debug.Log($"Mistral 回覆: {reply}");
                            onResult?.Invoke(reply);
                            _lastRequestSuccessful = true;
                        }
                        else
                        {
                            Debug.LogError("無法解析 Mistral 回應或回應為空");
                            if (isLastAttempt)
                            {
                                onError?.Invoke("無法解析 Mistral 回應");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"解析 Mistral 回應時出錯: {e.Message}");
                        if (isLastAttempt)
                        {
                            onError?.Invoke($"解析 Mistral 回應時出錯: {e.Message}");
                        }
                    }
                }
            }
        }
        
        #region Mistral API 資料結構
        
        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }
        
        [Serializable]
        private class ChatRequest
        {
            public string model;
            public Message[] messages;
            public float temperature;
            public int max_tokens;
            public float top_p;
            public float presence_penalty;
            public float frequency_penalty;
            public bool safe_prompt;
        }
        
        [Serializable]
        private class ChatResponse
        {
            public string id;
            public string @object;
            public long created;
            public string model;
            public Choice[] choices;
            public Usage usage;
        }
        
        [Serializable]
        private class Choice
        {
            public int index;
            public Message message;
            public string finish_reason;
        }
        
        [Serializable]
        private class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
        
        [Serializable]
        private class ErrorResponse
        {
            public ErrorDetail error;
        }
        
        [Serializable]
        private class ErrorDetail
        {
            public string message;
            public string type;
            public string param;
            public string code;
        }
        
        #endregion
    }
} 