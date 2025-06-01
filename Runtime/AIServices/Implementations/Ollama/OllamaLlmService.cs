using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AIServices.Interfaces;

namespace AIServices.Implementations.Ollama
{
    /// <summary>
    /// Ollama 大型語言模型服務實現 (本地部署 LLM)
    /// </summary>
    public class OllamaLlmService : MonoBehaviour, ILlmService
    {
        [Header("Ollama 設定")]
        [SerializeField] private string endpoint = "http://localhost:11434/api/generate";
        [SerializeField] private string model = "llama3";
        
        [Header("系統提示詞")]
        [SerializeField, TextArea(3, 10)] private string systemPrompt = "你是一個智能對話機器人，擅長理解用戶需求並提供協助。請以溫和、專業的態度回應問題，用詞準確且易於理解。盡量給出實用的建議，並保持對話的流暢性。";
        
        [Header("生成設定")]
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int numPredict = 150;
        [SerializeField] private float topP = 0.9f;
        [SerializeField] private float topK = 40;
        
        private List<ChatMessage> _messageHistory = new List<ChatMessage>();
        
        private void Awake()
        {
            // 初始化對話歷史，添加系統提示詞
            _messageHistory.Add(new ChatMessage { role = "system", content = systemPrompt });
        }
        
        /// <summary>
        /// 發送訊息到 Ollama
        /// </summary>
        public void SendMessage(string message, Action<string> onResult, Action<string> onError)
        {
            // 添加用戶訊息到歷史
            _messageHistory.Add(new ChatMessage { role = "user", content = message });
            
            // 發送請求
            StartCoroutine(SendChatRequest(onResult, onError));
        }
        
        /// <summary>
        /// 發送聊天請求到 Ollama
        /// </summary>
        private IEnumerator SendChatRequest(Action<string> onResult, Action<string> onError)
        {
            // 構建提示詞
            string prompt = BuildPrompt();
            
            // 構建請求
            OllamaRequest request = new OllamaRequest
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new OllamaOptions
                {
                    temperature = temperature,
                    num_predict = numPredict,
                    top_p = topP,
                    top_k = (int)topK
                }
            };
            
            string jsonRequest = JsonConvert.SerializeObject(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            
            using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                Debug.Log($"發送請求到 Ollama: {jsonRequest}");
                
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Ollama 錯誤: {www.error}");
                    onError?.Invoke($"Ollama 錯誤: {www.error}");
                    yield break;
                }
                
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"Ollama 回應: {jsonResponse}");
                
                try
                {
                    OllamaResponse response = JsonConvert.DeserializeObject<OllamaResponse>(jsonResponse);
                    
                    if (response != null)
                    {
                        string reply = response.response;
                        
                        // 添加助手回應到歷史
                        _messageHistory.Add(new ChatMessage { role = "assistant", content = reply });
                        
                        Debug.Log($"Ollama 回覆: {reply}");
                        onResult?.Invoke(reply);
                    }
                    else
                    {
                        Debug.LogError("無法解析 Ollama 回應");
                        onError?.Invoke("無法解析 Ollama 回應");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析 Ollama 回應時出錯: {e.Message}");
                    onError?.Invoke($"解析 Ollama 回應時出錯: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 構建提示詞
        /// </summary>
        private string BuildPrompt()
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var message in _messageHistory)
            {
                switch (message.role)
                {
                    case "system":
                        sb.AppendLine($"<s>[INST] <<SYS>>\n{message.content}\n<</SYS>>\n\n");
                        break;
                    case "user":
                        if (sb.Length == 0)
                        {
                            sb.AppendLine($"<s>[INST] {message.content} [/INST]");
                        }
                        else
                        {
                            sb.AppendLine($"[INST] {message.content} [/INST]");
                        }
                        break;
                    case "assistant":
                        sb.AppendLine($"{message.content}</s>");
                        break;
                }
            }
            
            return sb.ToString();
        }
        
        #region Ollama API 資料結構
        
        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }
        
        [Serializable]
        private class OllamaRequest
        {
            public string model;
            public string prompt;
            public bool stream;
            public OllamaOptions options;
        }
        
        [Serializable]
        private class OllamaOptions
        {
            public float temperature;
            public int num_predict;
            public float top_p;
            public int top_k;
        }
        
        [Serializable]
        private class OllamaResponse
        {
            public string model;
            public string response;
            public bool done;
            public int context;
            public float total_duration;
            public float load_duration;
            public float prompt_eval_duration;
            public float eval_duration;
        }
        
        #endregion
    }
}
