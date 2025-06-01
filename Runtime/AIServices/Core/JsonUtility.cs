using System;
using UnityEngine;

namespace AIServices.Core
{
    /// <summary>
    /// JSON 工具類，提供序列化/反序列化功能
    /// 在沒有 Newtonsoft.Json 時使用 Unity 內建 JsonUtility
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 將物件序列化為 JSON 字串
        /// </summary>
        public static string SerializeObject(object obj)
        {
#if NEWTONSOFT_JSON
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
#else
            return UnityEngine.JsonUtility.ToJson(obj);
#endif
        }
        
        /// <summary>
        /// 將 JSON 字串反序列化為指定類型
        /// </summary>
        public static T DeserializeObject<T>(string json)
        {
#if NEWTONSOFT_JSON
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
#else
            return UnityEngine.JsonUtility.FromJson<T>(json);
#endif
        }
        
        /// <summary>
        /// 將 JSON 字串反序列化為指定類型
        /// </summary>
        public static object DeserializeObject(string json, Type type)
        {
#if NEWTONSOFT_JSON
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
#else
            return UnityEngine.JsonUtility.FromJson(json, type);
#endif
        }
    }
} 