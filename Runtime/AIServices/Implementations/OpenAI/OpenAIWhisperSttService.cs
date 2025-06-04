using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIServices.Interfaces;
using Newtonsoft.Json;

namespace AIServices.Implementations.OpenAI
{
    /// <summary>
    /// OpenAI Whisper 語音轉文字服務實現
    /// 使用Whisper API進行高精度語音辨識
    /// </summary>
    public class OpenAIWhisperSttService : MonoBehaviour, ISttService
    {
        [Header("OpenAI API 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        
        [Header("Whisper 設定")]
        [SerializeField] private string model = "whisper-1";
        [SerializeField] private string language = "zh"; // 設定語言可提高準確度
        [SerializeField] private string responseFormat = "json"; // json, text, srt, verbose_json, vtt
        [SerializeField, Range(0f, 1f)] private float temperature = 0f; // 控制輸出的隨機性
        
        [Header("錄音設定")]
        [SerializeField] private string microphoneDevice = null;
        [SerializeField] private int maxRecordingSeconds = 30;
        [SerializeField] private int sampleRate = 44100;
        [SerializeField, Range(0.0001f, 0.1f)] private float volumeThreshold = 0.001f;
        
        [Header("進階設定")]
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private int maxRetries = 3;
        
        // 事件
        public event Action<string> OnTranscriptionResult;
        public event Action<string> OnError;
        
        // 錄音狀態
        private AudioClip _recordingClip;
        private bool _isListening = false;
        private string _tempDirectory;
        
        public bool IsListening => _isListening;
        
        private void Awake()
        {
            // 設置臨時目錄
            _tempDirectory = Path.Combine(Application.temporaryCachePath, "OpenAIWhisperTemp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }
        
        private void Start()
        {
            // 檢查可用的麥克風設備
            string[] devices = Microphone.devices;
            Debug.Log($"OpenAI Whisper STT - 可用麥克風設備數量: {devices.Length}");
            
            foreach (var device in devices)
            {
                Debug.Log($"麥克風設備: {device}");
            }
            
            // 使用預設設備
            if (string.IsNullOrEmpty(microphoneDevice) && devices.Length > 0)
            {
                microphoneDevice = devices[0];
                Debug.Log($"使用預設麥克風設備: {microphoneDevice}");
            }
        }
        
        /// <summary>
        /// 開始錄音
        /// </summary>
        public void StartListening()
        {
            if (_isListening)
            {
                Debug.LogWarning("已經在錄音中");
                return;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                OnError?.Invoke("OpenAI API密鑰未設置");
                return;
            }
            
            try
            {
                // 檢查麥克風權限
                if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                {
                    Debug.LogError("沒有麥克風權限");
                    OnError?.Invoke("請授予麥克風權限");
                    return;
                }
                
                // 檢查麥克風設備
                if (!string.IsNullOrEmpty(microphoneDevice) && !Array.Exists(Microphone.devices, d => d == microphoneDevice))
                {
                    Debug.LogError($"指定的麥克風設備不存在: {microphoneDevice}");
                    OnError?.Invoke("麥克風設備不可用");
                    return;
                }
                
                // 開始錄音
                _recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
                if (_recordingClip == null)
                {
                    Debug.LogError("無法啟動麥克風錄音");
                    OnError?.Invoke("無法啟動麥克風錄音");
                    return;
                }
                
                _isListening = true;
                Debug.Log($"OpenAI Whisper STT - 開始錄音，使用設備: {microphoneDevice ?? "預設設備"}");
            }
            catch (Exception e)
            {
                Debug.LogError($"OpenAI Whisper STT 啟動錄音錯誤: {e.Message}");
                OnError?.Invoke($"啟動錄音錯誤: {e.Message}");
            }
        }
        
        /// <summary>
        /// 停止錄音並發送到OpenAI Whisper進行轉錄
        /// </summary>
        public void StopListening()
        {
            if (!_isListening || _recordingClip == null)
            {
                return;
            }
            
            _isListening = false;
            
            try
            {
                // 獲取錄音位置
                int position = Microphone.GetPosition(microphoneDevice);
                Microphone.End(microphoneDevice);
                
                if (position <= 0)
                {
                    Debug.LogWarning("錄音可能為空，未檢測到聲音");
                    OnError?.Invoke("未檢測到有效聲音");
                    return;
                }
                
                Debug.Log($"錄音完成，位置: {position}, 樣本數: {_recordingClip.samples}");
                
                // 延遲處理音頻數據
                StartCoroutine(ProcessRecordingWithDelay(_recordingClip, position));
            }
            catch (Exception e)
            {
                Debug.LogError($"OpenAI Whisper STT 處理錄音錯誤: {e.Message}");
                OnError?.Invoke($"錄音處理錯誤: {e.Message}");
                
                // 確保麥克風被釋放
                if (Microphone.IsRecording(microphoneDevice))
                {
                    Microphone.End(microphoneDevice);
                }
            }
        }
        
        /// <summary>
        /// 延遲處理錄音數據
        /// </summary>
        private IEnumerator ProcessRecordingWithDelay(AudioClip clip, int recordedSamples)
        {
            yield return new WaitForSeconds(0.2f);
            
            // 第一步：獲取音頻數據
            float[] samples = null;
            bool audioProcessSuccess = false;
            
            try
            {
                // 獲取音頻數據
                int actualSamples = Mathf.Min(recordedSamples, clip.samples);
                samples = new float[actualSamples * clip.channels];
                
                if (!clip.GetData(samples, 0))
                {
                    OnError?.Invoke("無法獲取音頻數據");
                    yield break;
                }
                
                audioProcessSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取音頻數據錯誤: {e.Message}");
                OnError?.Invoke($"獲取音頻數據錯誤: {e.Message}");
                yield break;
            }
            
            if (!audioProcessSuccess || samples == null)
            {
                yield break;
            }
            
            // 第二步：驗證音頻數據
            if (!ValidateAudioData(samples))
            {
                OnError?.Invoke("未檢測到有效聲音，請重新錄音");
                yield break;
            }
            
            // 第三步：編碼WAV數據
            byte[] wavData = null;
            try
            {
                wavData = EncodeAsWAV(samples, clip.frequency, clip.channels);
            }
            catch (Exception e)
            {
                Debug.LogError($"WAV編碼錯誤: {e.Message}");
                OnError?.Invoke($"音頻編碼錯誤: {e.Message}");
                yield break;
            }
            
            if (wavData == null)
            {
                OnError?.Invoke("音頻編碼失敗");
                yield break;
            }
            
            // 第四步：發送到Whisper API（協程調用，不能在try-catch中）
            yield return StartCoroutine(SendToWhisperAPI(wavData));
        }
        
        /// <summary>
        /// 驗證音頻數據是否有效
        /// </summary>
        private bool ValidateAudioData(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return false;
            
            float maxAmplitude = 0f;
            int validSamples = 0;
            
            for (int i = 0; i < samples.Length; i++)
            {
                float amplitude = Mathf.Abs(samples[i]);
                maxAmplitude = Mathf.Max(maxAmplitude, amplitude);
                
                if (amplitude > volumeThreshold)
                {
                    validSamples++;
                }
            }
            
            float validRatio = (float)validSamples / samples.Length;
            Debug.Log($"音頻驗證 - 最大音量: {maxAmplitude:F4}, 有效樣本比例: {validRatio:F4}");
            
            return validRatio > 0.01f; // 至少1%的樣本超過閾值
        }
        
        /// <summary>
        /// 發送音頻到OpenAI Whisper API
        /// </summary>
        private IEnumerator SendToWhisperAPI(byte[] audioData)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                yield return StartCoroutine(AttemptWhisperRequest(audioData, attempt == maxRetries - 1));
                
                if (_lastRequestSuccessful)
                {
                    yield break;
                }
                
                if (attempt < maxRetries - 1)
                {
                    Debug.LogWarning($"Whisper API 重試 {attempt + 1}/{maxRetries}");
                    yield return new WaitForSeconds(Mathf.Pow(2, attempt)); // 指數退避
                }
            }
        }
        
        private bool _lastRequestSuccessful = false;
        
        /// <summary>
        /// 嘗試發送Whisper請求
        /// </summary>
        private IEnumerator AttemptWhisperRequest(byte[] audioData, bool isLastAttempt)
        {
            _lastRequestSuccessful = false;
            
            // 創建multipart/form-data請求
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", audioData, "recording.wav", "audio/wav");
            form.AddField("model", model);
            
            if (!string.IsNullOrEmpty(language))
                form.AddField("language", language);
            
            form.AddField("response_format", responseFormat);
            form.AddField("temperature", temperature.ToString("F2"));
            
            using (UnityWebRequest request = UnityWebRequest.Post(apiEndpoint, form))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                request.timeout = (int)requestTimeout;
                
                Debug.Log($"發送音頻到OpenAI Whisper API ({audioData.Length} bytes)");
                
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorMsg = $"Whisper API 錯誤: {request.error}";
                    string responseText = request.downloadHandler?.text ?? "";
                    
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        errorMsg += $" - 回應: {responseText}";
                    }
                    
                    Debug.LogError(errorMsg);
                    
                    if (isLastAttempt)
                    {
                        OnError?.Invoke($"語音辨識失敗: {request.error}");
                    }
                }
                else
                {
                    string response = request.downloadHandler.text;
                    Debug.Log($"Whisper API 回應: {response}");
                    
                    try
                    {
                        ParseWhisperResponse(response);
                        _lastRequestSuccessful = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"解析Whisper回應錯誤: {e.Message}");
                        if (isLastAttempt)
                        {
                            OnError?.Invoke($"解析語音辨識結果錯誤: {e.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 解析Whisper API回應
        /// </summary>
        private void ParseWhisperResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                OnError?.Invoke("Whisper回應為空");
                return;
            }
            
            if (responseFormat == "text")
            {
                // 純文字格式
                string transcription = response.Trim();
                if (!string.IsNullOrEmpty(transcription))
                {
                    Debug.Log($"辨識結果: {transcription}");
                    OnTranscriptionResult?.Invoke(transcription);
                }
                else
                {
                    OnError?.Invoke("未能辨識出文字內容");
                }
            }
            else
            {
                // JSON格式
                try
                {
                    var whisperResponse = JsonConvert.DeserializeObject<WhisperResponse>(response);
                    
                    if (whisperResponse != null && !string.IsNullOrEmpty(whisperResponse.text))
                    {
                        string transcription = whisperResponse.text.Trim();
                        Debug.Log($"辨識結果: {transcription}");
                        OnTranscriptionResult?.Invoke(transcription);
                    }
                    else
                    {
                        OnError?.Invoke("未能辨識出文字內容");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON解析錯誤: {e.Message}");
                    OnError?.Invoke("解析辨識結果失敗");
                }
            }
        }
        
        /// <summary>
        /// 將音頻樣本編碼為WAV格式
        /// </summary>
        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                int sampleCount = samples.Length;
                int byteCount = sampleCount * 2; // 16-bit samples
                
                // WAV header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + byteCount);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // fmt chunk size
                writer.Write((short)1); // PCM format
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2); // byte rate
                writer.Write((short)(channels * 2)); // block align
                writer.Write((short)16); // bits per sample
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(byteCount);
                
                // Audio data
                foreach (float sample in samples)
                {
                    short intSample = (short)(sample * 32767f);
                    writer.Write(intSample);
                }
                
                return memoryStream.ToArray();
            }
        }
        
        /// <summary>
        /// 清理資源
        /// </summary>
        private void OnDestroy()
        {
            // 停止錄音
            if (_isListening && Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
            }
            
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
        
        #region Whisper API 資料結構
        
        [Serializable]
        private class WhisperResponse
        {
            public string text;
        }
        
        #endregion
    }
} 