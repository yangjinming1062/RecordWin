using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RecordWin
{
    internal static class SettingHelp
    {
        static string filePath = "Setting.dat";
        public static Setting Settings = new Setting();
        /// <summary>
        /// 配置存取类，使用之前需要先调用SetPath方法修改配置文件路径（不调用使用默认位置）
        /// </summary>
        static SettingHelp()
        {
            CheckSetting();
        }

        public static void SetPath(string settingPath)
        {
            filePath = settingPath;
            CheckSetting();
        }

        #region 私有方法
        /// <summary>
        /// 检查当前配置路径是否存在配置文件，如果不存在则创建默认配置文件，存在则加载已有配置
        /// </summary>
        private static void CheckSetting()
        {
            if (!File.Exists(filePath))
                SaveSetting();
            else
                GetSetting();
        }

        private static void GetSetting()
        {
            if (Path.GetDirectoryName(filePath).Length > 0)
            {
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter bFormat = new BinaryFormatter();
                Settings = (Setting)bFormat.Deserialize(stream);
            }
        }
        /// <summary>
        /// Setting中属性变更时调用
        /// </summary>
        internal static void SaveSetting()
        {
            if (Path.GetDirectoryName(filePath).Length > 0)
            {
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter bFormat = new BinaryFormatter();
                bFormat.Serialize(stream, Settings);
            }
        }
        #endregion
    }
    [Serializable]
    class Setting
    {
        int _录制类型 = 0;
        public int 录制类型
        {
            get { return _录制类型; }
            set { _录制类型 = value;SettingHelp.SaveSetting(); }
        }

        bool _麦克风 = true;
        public bool 麦克风
        {
            get { return _麦克风; }
            set { _麦克风 = value; SettingHelp.SaveSetting(); }
        }

        bool _声卡 = false;
        public bool 声卡
        {
            get { return _声卡; }
            set { _声卡 = value; SettingHelp.SaveSetting(); }
        }

        bool _自动隐藏 = false;
        public bool 自动隐藏
        {
            get { return _自动隐藏; }
            set { _自动隐藏 = value; SettingHelp.SaveSetting(); }
        }
    }
}
