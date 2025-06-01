using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AIServices.Interfaces;

namespace AIServices.Implementations.Google
{
    /// <summary>
    /// Gemini 大型語言模型服務實現
    /// </summary>
    public class GeminiLlmService : MonoBehaviour, ILlmService
    {
        [Header("Gemini API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        
        [Header("系統提示詞")]
        [SerializeField, TextArea(3, 10)] private string systemPrompt = "你是一位樂於助人的AI助理，致力於為用戶提供準確、有用的資訊。請用清晰、友好的方式表達，避免使用過於技術性的術語。當回答問題時，請考慮用戶的背景和需求。";
        
        [Header("生成設定")]
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxOutputTokens = 30;
        [SerializeField] private float topP = 0.95f;
        [SerializeField] private int topK = 40;
        
        private List<Content> chatHistory;
        private SystemInstruction systemInstruction;
        
        private void Awake()
        {
            // 初始化系統提示詞
            systemInstruction = new SystemInstruction
            {
                parts = new Part[]
                {
                    new Part { text = systemPrompt }
                }
            };
            
            // 初始化聊天歷史
            chatHistory = new List<Content>();
        }
        
        /// <summary>
        /// 發送訊息到 Gemini API
        /// </summary>
        public void SendMessage(string message, Action<string> onResult, Action<string> onError)
        {
            StartCoroutine(SendChatRequestToGemini(message, onResult, onError));
        }
        
        /// <summary>
        /// 發送聊天請求到 Gemini API
        /// </summary>
        private IEnumerator SendChatRequestToGemini(string newMessage, Action<string> onResult, Action<string> onError)
        {
            string url = $"{apiEndpoint}?key={apiKey}";
            
            // 使用者輸入
            Content userContent = new Content
            {
                role = "user",
                parts = new Part[]
                {
                    new Part { text = newMessage }
                }
            };
            
            chatHistory.Add(userContent);
            
            // 建立請求資料
            ChatRequest chatRequest = new ChatRequest
            {
                systemInstruction = systemInstruction,
                contents = chatHistory,
                generationConfig = new GenerationConfig
                {
                    temperature = temperature,
                    maxOutputTokens = maxOutputTokens,
                    topP = topP,
                    topK = topK
                }
            };
            
            string jsonData = "";
            try
            {
                jsonData = JsonConvert.SerializeObject(chatRequest);
                Debug.Log("📤 Sending JSON:\n" + jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError("❌ JSON Serialization Failed: " + ex.Message);
                onError?.Invoke("JSON 序列化失敗: " + ex.Message);
                yield break;
            }
            
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(jsonToSend);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                    onError?.Invoke(www.error);
                }
                else
                {
                    string rawResponse = www.downloadHandler.text;
                    Debug.Log("📥 Gemini Response:\n" + rawResponse);
                    
                    ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(rawResponse);
                    if (response != null && response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        string reply = response.candidates[0].content.parts[0].text;
                        
                        // 加入模型回應
                        Content botContent = new Content
                        {
                            role = "model",
                            parts = new Part[]
                            {
                                new Part { text = reply }
                            }
                        };
                        chatHistory.Add(botContent);
                        
                        Debug.Log("🗨️ " + reply);
                        onResult?.Invoke(reply);
                    }
                    else
                    {
                        Debug.Log("No valid response from Gemini.");
                        onError?.Invoke("Gemini 未返回有效回應");
                    }
                }
            }
        }
        
        #region Gemini API 資料結構
        
        [Serializable]
        private class ChatResponse
        {
            public Candidate[] candidates;
        }
        
        [Serializable]
        private class Candidate
        {
            public Content content;
            public string finishReason;
        }
        
        [Serializable]
        private class Content
        {
            public string role; // "user" or "model"
            public Part[] parts;
        }
        
        [Serializable]
        private class Part
        {
            public string text;
        }
        
        private class ChatRequest
        {
            [JsonProperty("system_instruction")]
            public SystemInstruction systemInstruction;
            
            [JsonProperty("contents")]
            public List<Content> contents;
            
            [JsonProperty("generationConfig")]
            public GenerationConfig generationConfig;
        }
        
        private class SystemInstruction
        {
            public Part[] parts;
        }
        
        private class GenerationConfig
        {
            public float temperature = 0.7f;
            public int maxOutputTokens = 30;
            public float topP = 0.95f;
            public int topK = 40;
        }
        
        #endregion
    }
}
