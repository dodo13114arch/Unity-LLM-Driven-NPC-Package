using System;

namespace AIServices.Interfaces
{
    /// <summary>
    /// 語音轉文字服務介面
    /// </summary>
    public interface ISttService
    {
        /// <summary>
        /// 開始錄音或監聽
        /// </summary>
        void StartListening();
        
        /// <summary>
        /// 停止錄音或監聽
        /// </summary>
        void StopListening();
        
        /// <summary>
        /// 當收到辨識結果時觸發的事件
        /// </summary>
        event Action<string> OnTranscriptionResult;
        
        /// <summary>
        /// 當發生錯誤時觸發的事件
        /// </summary>
        event Action<string> OnError;
        
        /// <summary>
        /// 檢查是否正在監聽
        /// </summary>
        bool IsListening { get; }
    }
}
