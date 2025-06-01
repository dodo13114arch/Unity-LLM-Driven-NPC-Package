using System;
using UnityEngine;

namespace AIServices.Interfaces
{
    /// <summary>
    /// 文字轉語音服務介面
    /// </summary>
    public interface ITtsService
    {
        /// <summary>
        /// 將文字轉換為語音
        /// </summary>
        /// <param name="text">要轉換的文字</param>
        /// <param name="onAudioClipReady">音頻準備好時的回調</param>
        /// <param name="onError">錯誤回調</param>
        void Speak(string text, Action<AudioClip> onAudioClipReady, Action<string> onError);
    }
}
