using System;
using System.Collections.Generic;

namespace AIServices.Interfaces
{
    /// <summary>
    /// 大型語言模型服務介面
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// 發送訊息並取得回應
        /// </summary>
        /// <param name="message">使用者訊息</param>
        /// <param name="onResult">成功回調</param>
        /// <param name="onError">錯誤回調</param>
        void SendMessage(string message, Action<string> onResult, Action<string> onError);
    }

    /// <summary>
    /// 聊天訊息資料結構
    /// </summary>
    public struct ChatMessage
    {
        /// <summary>
        /// 訊息角色 (user, assistant, system)
        /// </summary>
        public string role;
        
        /// <summary>
        /// 訊息內容
        /// </summary>
        public string content;
    }
}
