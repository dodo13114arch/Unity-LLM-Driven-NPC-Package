using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AIServices.Interfaces;

namespace AIServices.Implementations.OpenAI
{
    /// <summary>
    /// OpenAI 大型語言模型服務實現
    /// </summary>
    public class OpenAILlmService : MonoBehaviour, ILlmService
    {
        [Header("OpenAI API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private string model = "gpt-4";
        
        [Header("系統提示詞")]
        [SerializeField, TextArea(3, 10)] private string systemPrompt = "你是一位友善的虛擬助手，具有豐富的知識和良好的溝通能力。請用自然、親切的語氣與用戶對話，提供有用的資訊和建議。回應時請保持簡潔明瞭，避免過於冗長的說明。";
        
        [Header("生成設定")]
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 150;
        [SerializeField] private float topP = 1.0f;
        [SerializeField] private float presencePenalty = 0.0f;
        [SerializeField] private float frequencyPenalty = 0.0f;
        
        private List<Message> _messageHistory = new List<Message>();
        
        private void Awake()
        {
            // 初始化對話歷史，添加系統提示詞
            _messageHistory.Add(new Message { role = "system", content = systemPrompt });
        }
        
        /// <summary>
        /// 發送訊息到 OpenAI API
        /// </summary>
        public void SendMessage(string message, Action<string> onResult, Action<string> onError)
        {
            // 添加用戶訊息到歷史
            _messageHistory.Add(new Message { role = "user", content = message });
            
            // 發送請求
            StartCoroutine(SendChatRequest(onResult, onError));
        }
        
        /// <summary>
        /// 發送聊天請求到 OpenAI API
        /// </summary>
        private IEnumerator SendChatRequest(Action<string> onResult, Action<string> onError)
        {
            // 構建請求
            ChatRequest request = new ChatRequest
            {
                model = model,
                messages = _messageHistory.ToArray(),
                temperature = temperature,
                max_tokens = maxTokens,
                top_p = topP,
                presence_penalty = presencePenalty,
                frequency_penalty = frequencyPenalty
            };
            
            string jsonRequest = JsonConvert.SerializeObject(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            
            using (UnityWebRequest www = new UnityWebRequest(apiEndpoint, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                Debug.Log($"發送請求到 OpenAI: {jsonRequest}");
                
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"OpenAI API 錯誤: {www.error}");
                    onError?.Invoke($"OpenAI API 錯誤: {www.error}");
                    yield break;
                }
                
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"OpenAI 回應: {jsonResponse}");
                
                try
                {
                    ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(jsonResponse);
                    
                    if (response != null && response.choices != null && response.choices.Length > 0)
                    {
                        string reply = response.choices[0].message.content;
                        
                        // 添加助手回應到歷史
                        _messageHistory.Add(new Message { role = "assistant", content = reply });
                        
                        Debug.Log($"OpenAI 回覆: {reply}");
                        onResult?.Invoke(reply);
                    }
                    else
                    {
                        Debug.LogError("無法解析 OpenAI 回應");
                        onError?.Invoke("無法解析 OpenAI 回應");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析 OpenAI 回應時出錯: {e.Message}");
                    onError?.Invoke($"解析 OpenAI 回應時出錯: {e.Message}");
                }
            }
        }
        
        #region OpenAI API 資料結構
        
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
        
        #endregion
    }
}
