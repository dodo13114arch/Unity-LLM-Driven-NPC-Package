using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIServices.Interfaces;
using Newtonsoft.Json;

namespace AIServices.Implementations.OpenAI
{
    /// <summary>
    /// OpenAI TTS 服務實現
    /// 使用 GPT-4o-mini-tts 模型
    /// </summary>
    public class OpenAITtsService : MonoBehaviour, ITtsService
    {
        [Header("OpenAI API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/audio/speech";
        
        [Header("語音設定")]
        [SerializeField] private string model = "gpt-4o-mini-tts";
        [SerializeField] private string voice = "alloy"; // 可選值: alloy, echo, fable, onyx, nova, shimmer
        [SerializeField, Range(0.25f, 4.0f)] private float speed = 1.0f;
        [SerializeField] private string responseFormat = "mp3"; // 支援: mp3, opus, aac, flac
        
        [Header("進階設定")]
        [SerializeField] private float requestTimeout = 10f;
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private bool enableCache = true;
        [SerializeField] private int maxCacheSize = 50;
        
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _audioCache = 
            new System.Collections.Generic.Dictionary<string, AudioClip>();
        
        public void Speak(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            if (string.IsNullOrEmpty(text))
            {
                onError?.Invoke("文字內容不能為空");
                return;
            }
            
            // 檢查快取
            if (enableCache && _audioCache.TryGetValue(text, out AudioClip cachedClip))
            {
                onAudioClipReady?.Invoke(cachedClip);
                return;
            }
            
            StartCoroutine(GenerateSpeech(text, onAudioClipReady, onError));
        }
        
        private IEnumerator GenerateSpeech(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            int retryCount = 0;
            bool success = false;
            
            while (retryCount < maxRetries && !success)
            {
                UnityWebRequest www = null;
                bool requestPrepared = false;
                bool needsRetry = false;
                string errorMessage = "";
                
                // 準備請求
                try
                {
                    var requestData = new
                    {
                        model = model,
                        input = text,
                        voice = voice,
                        speed = speed,
                        response_format = responseFormat
                    };
                    
                    string jsonPayload = JsonConvert.SerializeObject(requestData);
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    
                    AudioType audioType = AudioType.MPEG; // 默認 mp3
                    switch (responseFormat.ToLower())
                    {
                        case "mp3": 
                            audioType = AudioType.MPEG;
                            break;
                        case "opus":
                            audioType = AudioType.OGGVORBIS; // Unity 沒有 Opus 特定類型，使用 OGG
                            break;
                        case "aac":
                            audioType = AudioType.ACC;
                            break;
                        case "flac":
                            // Unity 不直接支持 FLAC，但我們仍然允許請求它
                            // 需要另外處理轉換
                            audioType = AudioType.UNKNOWN;
                            break;
                    }
                    
                    www = new UnityWebRequest(apiEndpoint, "POST");
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerAudioClip(apiEndpoint, audioType);
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    
                    Debug.Log($"發送 TTS 請求到 OpenAI: {text}");
                    
                    www.SendWebRequest();
                    requestPrepared = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"OpenAI TTS 請求準備失敗: {e.Message}");
                    if (www != null)
                    {
                        www.Dispose();
                    }
                    
                    retryCount++;
                    needsRetry = true;
                    errorMessage = $"TTS 請求準備失敗: {e.Message}";
                    
                    if (retryCount >= maxRetries)
                    {
                        onError?.Invoke(errorMessage);
                        yield break;
                    }
                    
                    Debug.LogWarning($"TTS 重試 {retryCount}/{maxRetries}: {e.Message}");
                }
                
                // 如果需要重試，等待後繼續
                if (needsRetry)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                // 如果請求未能成功準備
                if (!requestPrepared || www == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                // 等待請求完成
                bool isTimedOut = false;
                float startTime = Time.time;
                
                while (!www.isDone)
                {
                    if (Time.time - startTime > requestTimeout)
                    {
                        www.Abort();
                        Debug.LogError("OpenAI TTS 請求超時");
                        isTimedOut = true;
                        break;
                    }
                    yield return null;
                }
                
                // 如果請求超時
                if (isTimedOut)
                {
                    www.Dispose();
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        onError?.Invoke("TTS 請求超時，已達最大重試次數");
                        yield break;
                    }
                    
                    Debug.LogWarning($"TTS 重試 {retryCount}/{maxRetries}: 請求超時");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                // 處理響應
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"OpenAI TTS 錯誤: {www.error}");
                    string responseText = "";
                    
                    try
                    {
                        responseText = www.downloadHandler.text;
                        Debug.LogError($"響應內容: {responseText}");
                    }
                    catch (Exception) { /* 忽略錯誤 */ }
                    
                    string error = www.error;
                    www.Dispose();
                    
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        onError?.Invoke($"OpenAI TTS 錯誤: {error}");
                        yield break;
                    }
                    
                    Debug.LogWarning($"TTS 重試 {retryCount}/{maxRetries}: {error}");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                // 處理音頻
                AudioClip clip = null;
                bool audioProcessingError = false;
                string audioErrorMessage = "";
                
                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(www);
                }
                catch (Exception e)
                {
                    Debug.LogError($"音頻處理錯誤: {e.Message}");
                    audioProcessingError = true;
                    audioErrorMessage = e.Message;
                }
                
                www.Dispose();
                
                // 處理音頻處理錯誤
                if (audioProcessingError)
                {
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        onError?.Invoke($"音頻處理錯誤: {audioErrorMessage}");
                        yield break;
                    }
                    
                    Debug.LogWarning($"TTS 重試 {retryCount}/{maxRetries}: 音頻處理錯誤");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                if (clip != null)
                {
                    clip.name = $"OpenAI_TTS_{text.GetHashCode()}";
                    
                    // 更新快取
                    if (enableCache)
                    {
                        // 如果快取已滿，移除最舊的項目
                        if (_audioCache.Count >= maxCacheSize)
                        {
                            var oldestKey = System.Linq.Enumerable.First(_audioCache.Keys);
                            var oldestClip = _audioCache[oldestKey];
                            Destroy(oldestClip);
                            _audioCache.Remove(oldestKey);
                        }
                        
                        _audioCache[text] = clip;
                    }
                    
                    Debug.Log("OpenAI TTS 音頻準備就緒");
                    onAudioClipReady?.Invoke(clip);
                    success = true;
                }
                else
                {
                    Debug.LogError("無法創建 OpenAI TTS 音頻");
                    
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        onError?.Invoke("無法創建音頻");
                        yield break;
                    }
                    
                    Debug.LogWarning($"TTS 重試 {retryCount}/{maxRetries}: 無法創建音頻");
                    yield return new WaitForSeconds(1f);
                }
            }
            
            if (!success)
            {
                onError?.Invoke("所有 OpenAI TTS 請求嘗試都失敗了");
            }
        }
        
        private void OnDestroy()
        {
            // 清理快取
            if (enableCache)
            {
                foreach (var clip in _audioCache.Values)
                {
                    if (clip != null)
                    {
                        Destroy(clip);
                    }
                }
                _audioCache.Clear();
            }
        }
    }
}
