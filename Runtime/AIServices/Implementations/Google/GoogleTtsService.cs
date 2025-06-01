using System;
using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using GoogleTextToSpeech.Scripts.Data;
using AIServices.Interfaces;

namespace AIServices.Implementations.Google
{
    /// <summary>
    /// Google 文字轉語音服務實現 (VoiceScriptableObject 整合版)
    /// </summary>
    public class GoogleTtsService : MonoBehaviour, ITtsService
    {
        [Header("Google TTS 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private VoiceScriptableObject voice;
        [SerializeField] private string apiEndpoint = "https://texttospeech.googleapis.com/v1/text:synthesize";
        
        private const string MP3_FILE_NAME = "tts_audio.mp3";
        private string _tempFilePath;
        
        private void Awake()
        {
            // 設置臨時文件路徑
            _tempFilePath = Path.Combine(Application.temporaryCachePath, MP3_FILE_NAME);
        }
        
        /// <summary>
        /// 將文字轉換為語音
        /// </summary>
        public void Speak(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            // 檢查 voice 是否已設置
            if (voice == null)
            {
                Debug.LogError("未設置 VoiceScriptableObject");
                onError?.Invoke("未設置 VoiceScriptableObject，無法生成語音");
                return;
            }
            
            StartCoroutine(SynthesizeSpeech(text, onAudioClipReady, onError));
        }
        
        /// <summary>
        /// 合成語音的協程
        /// </summary>
        private IEnumerator SynthesizeSpeech(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            // 構建請求數據 (使用 VoiceScriptableObject 中的設置)
            var requestData = new TtsRequest
            {
                input = new TtsInput { text = text },
                voice = new TtsVoice 
                { 
                    languageCode = voice.languageCode,
                    name = voice.name 
                },
                audioConfig = new TtsAudioConfig 
                { 
                    audioEncoding = "MP3",
                    speakingRate = voice.speed,
                    pitch = voice.pitch
                }
            };
            
            string jsonData = "";
            byte[] jsonToSend = null;
            
            try
            {
                // 序列化為 JSON
                jsonData = JsonUtility.ToJson(requestData);
                jsonToSend = Encoding.UTF8.GetBytes(jsonData);
                Debug.Log($"TTS 請求數據: {jsonData}");
            }
            catch (Exception e)
            {
                Debug.LogError($"TTS JSON 序列化錯誤: {e.Message}");
                onError?.Invoke($"TTS JSON 序列化錯誤: {e.Message}");
                yield break;
            }
            
            // 構建 URL 並添加 API 密鑰
            string url = $"{apiEndpoint}?key={apiKey}";
            Debug.Log($"使用 TTS 端點: {apiEndpoint}");
            
            // 創建並配置 Web 請求
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                
                // 發送請求
                yield return request.SendWebRequest();
                
                // 檢查請求結果
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"TTS API 錯誤: {request.error}, 響應內容: {request.downloadHandler.text}");
                    onError?.Invoke($"TTS API 錯誤: {request.error}");
                    yield break;
                }
                
                string response = "";
                TtsResponse ttsResponse = null;
                byte[] audioBytes = null;
                
                try
                {
                    // 解析響應
                    response = request.downloadHandler.text;
                    Debug.Log($"TTS 響應: {response}");
                    ttsResponse = JsonUtility.FromJson<TtsResponse>(response);
                    
                    if (string.IsNullOrEmpty(ttsResponse.audioContent))
                    {
                        Debug.LogError("TTS 響應中沒有音頻內容");
                        onError?.Invoke("TTS 響應中沒有音頻內容");
                        yield break;
                    }
                    
                    // 將 base64 轉換為二進制並保存為臨時 MP3 文件
                    audioBytes = Convert.FromBase64String(ttsResponse.audioContent);
                    File.WriteAllBytes(_tempFilePath, audioBytes);
                    Debug.Log($"已將音頻保存到: {_tempFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"TTS 響應處理錯誤: {e.Message}");
                    onError?.Invoke($"TTS 響應處理錯誤: {e.Message}");
                    yield break;
                }
                
                // 從 MP3 加載音頻剪輯
                using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + _tempFilePath, AudioType.MPEG))
                {
                    yield return audioRequest.SendWebRequest();
                    
                    if (audioRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"音頻加載錯誤: {audioRequest.error}");
                        onError?.Invoke($"音頻加載錯誤: {audioRequest.error}");
                        yield break;
                    }
                    
                    AudioClip clip = null;
                    
                    try
                    {
                        clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                        Debug.Log("TTS 音頻剪輯準備就緒");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"音頻剪輯處理錯誤: {e.Message}");
                        onError?.Invoke($"音頻剪輯處理錯誤: {e.Message}");
                        yield break;
                    }
                    
                    onAudioClipReady?.Invoke(clip);
                }
            }
        }
        
        #region TTS API 數據結構
        
        [Serializable]
        private class TtsRequest
        {
            public TtsInput input;
            public TtsVoice voice;
            public TtsAudioConfig audioConfig;
        }
        
        [Serializable]
        private class TtsInput
        {
            public string text;
        }
        
        [Serializable]
        private class TtsVoice
        {
            public string languageCode;
            public string name;
        }
        
        [Serializable]
        private class TtsAudioConfig
        {
            public string audioEncoding;
            public float speakingRate;
            public float pitch;
        }
        
        [Serializable]
        private class TtsResponse
        {
            public string audioContent;
        }
        
        #endregion
    }
}
