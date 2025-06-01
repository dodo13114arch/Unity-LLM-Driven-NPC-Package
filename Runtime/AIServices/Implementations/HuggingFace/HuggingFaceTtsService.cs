using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIServices.Interfaces;

namespace AIServices.Implementations.HuggingFace
{
    /// <summary>
    /// Hugging Face 文字轉語音服務實現
    /// </summary>
    public class HuggingFaceTtsService : MonoBehaviour, ITtsService
    {
        [Header("Hugging Face 設定")]
        [SerializeField] private string apiKey;
        [SerializeField] private string modelEndpoint = "https://api-inference.huggingface.co/models/espnet/kan-bayashi_ljspeech_vits";
        
        [Header("語音設定")]
        [SerializeField] private float speakerID = 0;
        [SerializeField] private float speed = 1.0f;
        
        /// <summary>
        /// 將文字轉換為語音
        /// </summary>
        public void Speak(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            StartCoroutine(RequestSpeechFromHuggingFace(text, onAudioClipReady, onError));
        }
        
        /// <summary>
        /// 向 Hugging Face 請求語音合成
        /// </summary>
        private IEnumerator RequestSpeechFromHuggingFace(string text, Action<AudioClip> onAudioClipReady, Action<string> onError)
        {
            // 構建請求
            string jsonPayload = $"{{\"inputs\": \"{text}\"}}";
            
            using (UnityWebRequest www = new UnityWebRequest(modelEndpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                Debug.Log($"發送 TTS 請求到 Hugging Face: {text}");
                
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Hugging Face TTS 錯誤: {www.error}");
                    onError?.Invoke($"Hugging Face TTS 錯誤: {www.error}");
                    yield break;
                }
                
                try
                {
                    // 將回應轉換為 AudioClip
                    byte[] audioData = www.downloadHandler.data;
                    
                    // 注意：這裡假設 Hugging Face 返回的是 WAV 格式的音頻
                    // 實際上可能需要根據具體模型的輸出格式進行調整
                    AudioClip clip = WavUtility.ToAudioClip(audioData);
                    
                    if (clip != null)
                    {
                        Debug.Log("Hugging Face TTS 音頻準備就緒");
                        onAudioClipReady?.Invoke(clip);
                    }
                    else
                    {
                        Debug.LogError("無法將 Hugging Face 回應轉換為 AudioClip");
                        onError?.Invoke("無法將 Hugging Face 回應轉換為 AudioClip");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"處理 Hugging Face TTS 回應時出錯: {e.Message}");
                    onError?.Invoke($"處理 Hugging Face TTS 回應時出錯: {e.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// WAV 格式音頻處理工具
    /// </summary>
    public static class WavUtility
    {
        /// <summary>
        /// 將 WAV 格式的字節數組轉換為 AudioClip
        /// </summary>
        public static AudioClip ToAudioClip(byte[] wavData)
        {
            // 注意：這是一個簡化的實現，實際上需要根據 WAV 文件格式進行更詳細的解析
            // 這裡僅作為示例，實際項目中可能需要更完整的 WAV 解析邏輯
            
            try
            {
                // 檢查 WAV 文件頭
                if (wavData.Length < 44) return null; // WAV 頭至少 44 字節
                
                // 解析 WAV 頭
                int channels = wavData[22]; // 通道數
                int sampleRate = BitConverter.ToInt32(wavData, 24); // 採樣率
                int bitsPerSample = wavData[34]; // 位深度
                
                // 計算音頻數據起始位置和長度
                int headerSize = 44; // 標準 WAV 頭大小
                int dataSize = wavData.Length - headerSize;
                
                // 創建 AudioClip
                AudioClip audioClip = AudioClip.Create("HuggingFaceTTS", dataSize / (bitsPerSample / 8) / channels, channels, sampleRate, false);
                
                // 填充音頻數據
                float[] audioData = new float[audioClip.samples * channels];
                for (int i = 0; i < audioData.Length; i++)
                {
                    int byteIndex = headerSize + i * (bitsPerSample / 8);
                    if (byteIndex + 1 < wavData.Length)
                    {
                        if (bitsPerSample == 16)
                        {
                            short sample = BitConverter.ToInt16(wavData, byteIndex);
                            audioData[i] = sample / 32768f; // 轉換為 -1.0 到 1.0 範圍
                        }
                        else if (bitsPerSample == 8)
                        {
                            audioData[i] = (wavData[byteIndex] - 128) / 128f;
                        }
                    }
                }
                
                audioClip.SetData(audioData, 0);
                return audioClip;
            }
            catch (Exception e)
            {
                Debug.LogError($"WAV 轉換錯誤: {e.Message}");
                return null;
            }
        }
    }
}
