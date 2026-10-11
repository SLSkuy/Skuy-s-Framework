using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 存储管理器
    /// </summary>
    public class StorageManager : SubSystemBase
    {
        private static readonly UTF8Encoding Utf8 = new(false);

        #region 属性
        public override int Priority => (int)SubSystemPriority.StorageManager;
        #endregion

        #region Json存储管理
        
        /// <summary>
        /// 读取 Json 文件中的数据
        /// </summary>
        public bool Load<T>(string fileName, out T data)
        {
            if (!Exists(fileName))
            {
                Debug.LogWarning($"[{GetType().Name}] 不存在 {fileName} 文件");
                data = default;
                return false;
            }
            
            string json = File.ReadAllText(GetPath(fileName), Utf8);
            data = JsonConvert.DeserializeObject<T>(json);
            return true;
        }

        /// <summary>
        /// 保存到 Json 文件中
        /// </summary>
        public void SaveJson<T>(string fileName, T data)
        {
            string path = GetPath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json, Utf8);
        }

        #endregion

        public static void DeleteFile(string fileName)
        {
            File.Delete(GetPath(fileName));
        }

        /// <summary>
        /// 判断本地文件是否已经存在
        /// </summary>
        private static bool Exists(string fileName)
        {
            return File.Exists(GetPath(fileName));
        }

        /// <summary>
        /// 把相对文件名拼到 <see cref="Application.persistentDataPath"/> 下
        /// </summary>
        private static string GetPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}