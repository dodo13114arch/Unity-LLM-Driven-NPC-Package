using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIServices.Interfaces;
using Newtonsoft.Json;
using System.IO;

namespace AIServices.Implementations.ElevenLabs
{
    /// <summary>
    /// ElevenLabs TTS 服務實現
    /// 支援高品質的語音合成，直接處理MP3格式回應
    /// </summary>
    public class ElevenLabsTtsService : MonoBehaviour, ITtsService
    {
        [Header("ElevenLabs API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string voiceId = "JBFqnCBsd6RMkjVDRZzb"; // Rachel voice (預設)
        [SerializeField] private string apiEndpoint = "https://api.elevenlabs.io/v1/text-to-speech";
        
        [Header("語音設定")]
        [SerializeField] private string modelId = "eleven_flash_v2_5";
        [SerializeField, Range(0f, 1f)] private float stability = 0.5f;
        [SerializeField, Range(0f, 1f)] private float similarityBoost = 0.5f;
        [SerializeField, Range(0f, 1f)] private float style = 0.0f;
        [SerializeField] private bool useSpeakerBoost = true;
        
        [Header("進階設定")]
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private bool enableCache = true;
        [SerializeField] private int maxCacheSize = 50;
        
        // 音頻快取
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _audioCache = 
            new System.Collections.Generic.Dictionary<string, AudioClip>();
        
        // 臨時檔案處理
        private string _tempDirectory;
        
        private void Awake()
        {
            // 設置臨時目錄
            _tempDirectory = Path.Combine(Application.temporaryCachePath, "ElevenLabsTemp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }
        
        /// <summary>
        /// 將文字轉換為語音
        /// </summary>
        public void Speak(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            if (string.IsNullOrEmpty(text))
            {
                onError?.Invoke("文字內容不能為空");
                return;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("ElevenLabs API密鑰未設置");
                return;
            }
            
            if (string.IsNullOrEmpty(voiceId))
            {
                onError?.Invoke("語音ID未設置");
                return;
            }
            
            // 檢查快取
            if (enableCache && _audioCache.TryGetValue(text, out AudioClip cachedClip))
            {
                Debug.Log("從快取中獲取音頻");
                onAudioClipReady?.Invoke(cachedClip);
                return;
            }
            
            StartCoroutine(GenerateSpeech(text, onAudioClipReady, onError));
        }
        
        /// <summary>
        /// 產生語音的協程
        /// </summary>
        private IEnumerator GenerateSpeech(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                yield return StartCoroutine(AttemptSpeechGeneration(text, onAudioClipReady, onError, retryCount == maxRetries - 1));
                
                // 如果成功，退出重試循環
                if (_lastRequestSuccessful)
                {
                    yield break;
                }
                
                retryCount++;
                if (retryCount < maxRetries)
                {
                    Debug.LogWarning($"ElevenLabs TTS 重試 {retryCount}/{maxRetries}");
                    yield return new WaitForSeconds(Mathf.Pow(2, retryCount)); // 指數退避
                }
            }
        }
        
        private bool _lastRequestSuccessful = false;
        
        /// <summary>
        /// 嘗試產生語音
        /// </summary>
        private IEnumerator AttemptSpeechGeneration(string text, Action<AudioClip> onAudioClipReady, Action<string> onError, bool isLastAttempt)
        {
            _lastRequestSuccessful = false;
            
            // 構建請求數據
            var requestData = new ElevenLabsRequest
            {
                text = text,
                model_id = modelId,
                voice_settings = new VoiceSettings
                {
                    stability = stability,
                    similarity_boost = similarityBoost,
                    style = style,
                    use_speaker_boost = useSpeakerBoost
                }
            };
            
            string jsonData = "";
            byte[] jsonToSend = null;
            
            try
            {
                jsonData = JsonConvert.SerializeObject(requestData);
                jsonToSend = Encoding.UTF8.GetBytes(jsonData);
                Debug.Log($"ElevenLabs TTS 請求: {text.Substring(0, Mathf.Min(50, text.Length))}...");
            }
            catch (Exception e)
            {
                Debug.LogError($"ElevenLabs TTS JSON 序列化錯誤: {e.Message}");
                if (isLastAttempt)
                {
                    onError?.Invoke($"JSON 序列化錯誤: {e.Message}");
                }
                yield break;
            }
            
            // 構建請求URL
            string url = $"{apiEndpoint}/{voiceId}";
            
            // 創建並配置 Web 請求
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Accept", "audio/mpeg");
                request.timeout = (int)requestTimeout;
                
                // 發送請求
                yield return request.SendWebRequest();
                
                // 檢查請求結果
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"ElevenLabs TTS API 錯誤: {request.error}");
                    string responseText = request.downloadHandler?.text ?? "無回應內容";
                    Debug.LogError($"錯誤回應: {responseText}");
                    
                    if (isLastAttempt)
                    {
                        onError?.Invoke($"ElevenLabs TTS API 錯誤: {request.error}");
                    }
                    yield break;
                }
                
                // 處理MP3音頻數據
                byte[] audioData = request.downloadHandler.data;
                if (audioData == null || audioData.Length == 0)
                {
                    Debug.LogError("ElevenLabs TTS 回應中沒有音頻數據");
                    if (isLastAttempt)
                    {
                        onError?.Invoke("沒有收到音頻數據");
                    }
                    yield break;
                }
                
                Debug.Log($"ElevenLabs TTS 收到音頻數據: {audioData.Length} bytes");
                
                // 將MP3數據保存為臨時檔案並轉換為AudioClip
                yield return StartCoroutine(ConvertMp3ToAudioClip(audioData, text, onAudioClipReady, onError, isLastAttempt));
            }
        }
        
        /// <summary>
        /// 將MP3數據轉換為Unity AudioClip
        /// </summary>
        private IEnumerator ConvertMp3ToAudioClip(byte[] mp3Data, string originalText, Action<AudioClip> onAudioClipReady, Action<string> onError, bool isLastAttempt)
        {
            string tempFileName = $"elevenlabs_tts_{DateTime.Now.Ticks}.mp3";
            string tempFilePath = Path.Combine(_tempDirectory, tempFileName);
            
            // 第一步：寫入MP3檔案
            bool fileWriteSuccess = false;
            try
            {
                File.WriteAllBytes(tempFilePath, mp3Data);
                Debug.Log($"MP3檔案已保存到: {tempFilePath}");
                fileWriteSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"MP3檔案寫入錯誤: {e.Message}");
                if (isLastAttempt)
                {
                    onError?.Invoke($"檔案寫入錯誤: {e.Message}");
                }
                yield break;
            }
            
            if (!fileWriteSuccess)
            {
                yield break;
            }
            
            // 第二步：載入音頻檔案（協程部分，不能在try-catch中）
            string fileUrl = "file://" + tempFilePath;
            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();
                
                // 第三步：處理載入結果
                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"音頻載入錯誤: {audioRequest.error}");
                    if (isLastAttempt)
                    {
                        onError?.Invoke($"音頻載入錯誤: {audioRequest.error}");
                    }
                }
                else
                {
                    try
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                        if (clip != null)
                        {
                            clip.name = $"ElevenLabs_TTS_{DateTime.Now.Ticks}";
                            
                            // 加入快取
                            if (enableCache)
                            {
                                ManageCache(originalText, clip);
                            }
                            
                            Debug.Log("ElevenLabs TTS 音頻剪輯準備就緒");
                            _lastRequestSuccessful = true;
                            onAudioClipReady?.Invoke(clip);
                        }
                        else
                        {
                            Debug.LogError("無法從MP3檔案創建AudioClip");
                            if (isLastAttempt)
                            {
                                onError?.Invoke("無法創建AudioClip");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"AudioClip處理錯誤: {e.Message}");
                        if (isLastAttempt)
                        {
                            onError?.Invoke($"音頻處理錯誤: {e.Message}");
                        }
                    }
                }
            }
            
            // 第四步：清理臨時檔案
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"無法刪除臨時檔案 {tempFilePath}: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 管理音頻快取
        /// </summary>
        private void ManageCache(string text, AudioClip clip)
        {
            // 如果快取已滿，移除最舊的項目
            if (_audioCache.Count >= maxCacheSize)
            {
                var oldestKey = "";
                foreach (var key in _audioCache.Keys)
                {
                    oldestKey = key;
                    break;
                }
                
                if (!string.IsNullOrEmpty(oldestKey))
                {
                    if (_audioCache[oldestKey] != null)
                    {
                        DestroyImmediate(_audioCache[oldestKey]);
                    }
                    _audioCache.Remove(oldestKey);
                }
            }
            
            _audioCache[text] = clip;
        }
        
        /// <summary>
        /// 清理資源
        /// </summary>
        private void OnDestroy()
        {
            // 清理快取中的音頻剪輯
            foreach (var clip in _audioCache.Values)
            {
                if (clip != null)
                {
                    DestroyImmediate(clip);
                }
            }
            _audioCache.Clear();
            
            // 清理臨時目錄
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"無法清理臨時目錄: {e.Message}");
                }
            }
        }
        
        #region ElevenLabs API 資料結構
        
        [Serializable]
        private class ElevenLabsRequest
        {
            public string text;
            public string model_id;
            public VoiceSettings voice_settings;
        }
        
        [Serializable]
        private class VoiceSettings
        {
            public float stability;
            public float similarity_boost;
            public float style;
            public bool use_speaker_boost;
        }
        
        #endregion
    }
} 