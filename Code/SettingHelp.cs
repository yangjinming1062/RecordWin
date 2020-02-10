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
        /// <summary>
        /// 根据当前filePath获取配置
        /// </summary>
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
        #region 录制源
        bool _桌面 = true;
        public bool 桌面
        {
            get => _桌面;
            set { _桌面 = value; SettingHelp.SaveSetting(); }
        }

        bool _摄像头 = true;
        public bool 摄像头
        {
            get => _摄像头;
            set { _摄像头 = value; SettingHelp.SaveSetting(); }
        }

        bool _声音 = true;
        public bool 声音
        {
            get => _声音;
            set { _声音 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 功能
        bool _自动隐藏 = false;
        public bool 自动隐藏
        {
            get => _自动隐藏;
            set { _自动隐藏 = value; SettingHelp.SaveSetting(); }
        }

        bool _录制隐藏 = true;
        public bool 录制隐藏
        {
            get => _录制隐藏;
            set { _录制隐藏 = value; SettingHelp.SaveSetting(); }
        }

        string _保存路径 = "Temp";
        public string 保存路径
        {
            get => _保存路径;
            set { _保存路径 = value; SettingHelp.SaveSetting(); }
        }

        string _编码类型 = "mp4";
        public string 编码类型
        {
            get => _编码类型;
            set { _编码类型 = value; SettingHelp.SaveSetting(); }
        }

        string _命名规则 = "yyMMdd_HHmmss";
        public string 命名规则
        {
            get => _命名规则;
            set { _命名规则 = value; SettingHelp.SaveSetting(); }
        }

        bool _跨屏录制 = false;
        public bool 跨屏录制
        {
            get => _跨屏录制;
            set { _跨屏录制 = value; SettingHelp.SaveSetting(); }
        }

        bool _捕获鼠标 = false;
        public bool 捕获鼠标
        {
            get => _捕获鼠标;
            set { _捕获鼠标 = value; SettingHelp.SaveSetting(); }
        }

        bool _保留视频 = false;
        public bool 保留视频
        {
            get => _保留视频;
            set { _保留视频 = value; SettingHelp.SaveSetting(); }
        }

        bool _保留音频 = false;
        public bool 保留音频
        {
            get => _保留音频;
            set { _保留音频 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 录制
        int _视频帧率 = 5;
        public int 视频帧率
        {
            get => _视频帧率;
            set { _视频帧率 = value; SettingHelp.SaveSetting(); }
        }

        int _视频质量 = 5;
        public int 视频质量
        {
            get => _视频质量;
            set { _视频质量 = value; SettingHelp.SaveSetting(); }
        }

        string _摄像头Key;
        public string 摄像头Key
        {
            get => _摄像头Key;
            set { _摄像头Key = value; SettingHelp.SaveSetting(); }
        }

        int _摄像头参数 = 0;
        public int 摄像头参数
        {
            get => _摄像头参数;
            set { _摄像头参数 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 热键
        Tuple<HotKey.KeyModifiers, int> _播放暂停 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Space);
        public Tuple<HotKey.KeyModifiers, int> 播放暂停
        {
            get => _播放暂停;
            set { _播放暂停 = value; SettingHelp.SaveSetting(); }
        }

        Tuple<HotKey.KeyModifiers, int> _停止关闭 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Escape);
        public Tuple<HotKey.KeyModifiers, int> 停止关闭
        {
            get => _停止关闭;
            set { _停止关闭 = value; SettingHelp.SaveSetting(); }
        }

        Tuple<HotKey.KeyModifiers, int> _开关画笔 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.None, (int)System.Windows.Forms.Keys.F1);
        public Tuple<HotKey.KeyModifiers, int> 开关画笔
        {
            get => _开关画笔;
            set { _开关画笔 = value; SettingHelp.SaveSetting(); }
        }
        #endregion
    }
}
