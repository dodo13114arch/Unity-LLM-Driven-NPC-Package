using System;
using System.Collections;
using System.IO;
using UnityEngine;
using GoogleSpeechToText.Scripts;
using AIServices.Interfaces;

namespace AIServices.Implementations.Google
{
    /// <summary>
    /// Google 語音轉文字服務實現
    /// </summary>
    public class GoogleSttService : MonoBehaviour, ISttService
    {
        [Header("Google Cloud API Key")]
        [SerializeField] private string apiKey;
        
        [Header("麥克風設定")]
        [SerializeField] private string microphoneDevice = null; // 預設為 null 表示使用預設麥克風
        [SerializeField, Range(0.0001f, 0.1f)] private float volumeThreshold = 0.001f;
        
        private AudioClip _clip;
        private bool _isListening = false;
        
        public event Action<string> OnTranscriptionResult;
        public event Action<string> OnError;
        
        public bool IsListening => _isListening;
        
        private void Start()
        {
            // 檢查可用的麥克風設備
            string[] devices = Microphone.devices;
            Debug.Log($"可用的麥克風設備數量: {devices.Length}");
            foreach (var device in devices)
            {
                Debug.Log($"麥克風設備: {device}");
            }
            
            // 如果沒有指定麥克風設備，使用預設設備
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
            if (_isListening) return;
            
            try
            {
                // 檢查麥克風權限
                if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                {
                    Debug.LogError("沒有麥克風權限");
                    OnError?.Invoke("請授予麥克風權限");
                    return;
                }
                
                // 檢查麥克風設備是否存在
                if (!string.IsNullOrEmpty(microphoneDevice) && !Array.Exists(Microphone.devices, d => d == microphoneDevice))
                {
                    Debug.LogError($"指定的麥克風設備不存在: {microphoneDevice}");
                    OnError?.Invoke("麥克風設備不可用");
                    return;
                }
                
                _clip = Microphone.Start(microphoneDevice, false, 30, 44100);
                if (_clip == null)
                {
                    Debug.LogError("無法啟動麥克風錄音");
                    OnError?.Invoke("無法啟動麥克風錄音");
                    return;
                }
                
                _isListening = true;
                Debug.Log($"開始錄音，使用設備: {microphoneDevice ?? "預設設備"}");
            }
            catch (Exception e)
            {
                Debug.LogError($"啟動錄音時發生錯誤: {e.Message}");
                OnError?.Invoke($"啟動錄音時發生錯誤: {e.Message}");
            }
        }
        
        /// <summary>
        /// 停止錄音並發送到 Google 進行辨識
        /// </summary>
        public void StopListening()
        {
            if (!_isListening || _clip == null) return;
            
            _isListening = false; // 立即標記為非錄音狀態
            
            try
            {
                // 獲取錄音位置
                var position = Microphone.GetPosition(null);
                if (position <= 0)
                {
                    Debug.LogWarning("錄音可能為空，未檢測到聲音");
                    OnError?.Invoke("未檢測到有效聲音");
                    return;
                }
                
                // 結束錄音
                Microphone.End(null);
                
                // 添加小延遲以確保音頻數據已完全寫入
                StartCoroutine(ProcessAudioDataWithDelay(_clip, position));
            }
            catch (Exception e)
            {
                Debug.LogError($"處理錄音時發生錯誤: {e.Message}");
                OnError?.Invoke($"錄音處理錯誤: {e.Message}");
                Microphone.End(null); // 確保麥克風被釋放
            }
        }
        
        /// <summary>
        /// 延遲處理音頻數據的協程
        /// </summary>
        private IEnumerator ProcessAudioDataWithDelay(AudioClip clip, int position)
        {
            yield return new WaitForSeconds(0.5f);
            
            try
            {
                int sampleCount = Mathf.Min(position, clip.samples) * clip.channels;
                if (sampleCount <= 0)
                {
                    Debug.LogWarning("無效的樣本數: " + sampleCount);
                    OnError?.Invoke("錄音處理錯誤: 無效的樣本數");
                    yield break;
                }
                
                var samples = new float[sampleCount];
                
                if (!clip.GetData(samples, 0))
                {
                    Debug.LogError("無法獲取音頻數據");
                    OnError?.Invoke("無法獲取音頻數據");
                    yield break;
                }
                
                // 改進的聲音檢測邏輯
                bool hasSomeAudio = false;
                float maxAmplitude = 0f;
                int audioSampleCount = 0;
                
                for (int i = 0; i < samples.Length; i++)
                {
                    float amplitude = Mathf.Abs(samples[i]);
                    maxAmplitude = Mathf.Max(maxAmplitude, amplitude);
                    
                    if (amplitude > volumeThreshold)
                    {
                        audioSampleCount++;
                    }
                }
                
                // 計算有效音頻樣本的比例
                float audioRatio = (float)audioSampleCount / samples.Length;
                Debug.Log($"音頻分析 - 最大音量: {maxAmplitude}, 有效樣本比例: {audioRatio}");
                
                // 如果有效音頻樣本比例超過 1%，則認為有聲音
                if (audioRatio > 0.01f)
                {
                    hasSomeAudio = true;
                }
                
                if (!hasSomeAudio)
                {
                    Debug.LogWarning($"錄音靜音或音量太低 (最大音量: {maxAmplitude}, 有效樣本比例: {audioRatio})");
                    OnError?.Invoke("未檢測到有效聲音，請說話");
                    yield break;
                }
                
                // 編碼為WAV並發送請求
                byte[] bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
                
                GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey,
                    (response) => {
                        Debug.Log("Speech-to-Text Response: " + response);
                        
                        try
                        {
                            if (string.IsNullOrEmpty(response))
                            {
                                OnError?.Invoke("STT 回傳為空");
                                return;
                            }
                            
                            var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);
                            
                            if (speechResponse == null)
                            {
                                OnError?.Invoke("STT 回傳格式異常，無法解析為 SpeechToTextResponse");
                                return;
                            }
                            
                            if (speechResponse.results == null || speechResponse.results.Length == 0)
                            {
                                Debug.Log("STT 未檢測到語音 (空結果)");
                                OnTranscriptionResult?.Invoke("");
                                return;
                            }
                            
                            if (speechResponse.results[0].alternatives == null || speechResponse.results[0].alternatives.Length == 0)
                            {
                                OnError?.Invoke("STT 回傳格式異常，alternatives 為空");
                                return;
                            }
                            
                            var transcript = speechResponse.results[0].alternatives[0].transcript;
                            Debug.Log("Transcript: " + transcript);
                            
                            OnTranscriptionResult?.Invoke(transcript);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"STT 回應處理錯誤: {ex.Message}\n原始回應: {response}");
                            OnError?.Invoke($"STT 回應處理錯誤: {ex.Message}");
                        }
                    },
                    (error) => {
                        Debug.LogError($"STT 錯誤: {error?.error?.message ?? "未知錯誤"}");
                        OnError?.Invoke(error?.error?.message ?? "未知錯誤");
                    });
            }
            catch (Exception e)
            {
                Debug.LogError($"處理音頻數據時發生錯誤: {e.Message}");
                OnError?.Invoke($"音頻處理錯誤: {e.Message}");
            }
        }
        
        /// <summary>
        /// 將音頻樣本編碼為 WAV 格式
        /// </summary>
        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + samples.Length * 2);
                    writer.Write("WAVE".ToCharArray());
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((ushort)1);
                    writer.Write((ushort)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16);
                    writer.Write("data".ToCharArray());
                    writer.Write(samples.Length * 2);

                    foreach (var sample in samples)
                    {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }
                return memoryStream.ToArray();
            }
        }
    }
}
