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
            get { return _桌面; }
            set { _桌面 = value; SettingHelp.SaveSetting(); }
        }

        bool _摄像头 = true;
        public bool 摄像头
        {
            get { return _摄像头; }
            set { _摄像头 = value; SettingHelp.SaveSetting(); }
        }

        bool _声音 = true;
        public bool 声音
        {
            get { return _声音; }
            set { _声音 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 功能
        bool _自动隐藏 = false;
        public bool 自动隐藏
        {
            get { return _自动隐藏; }
            set { _自动隐藏 = value; SettingHelp.SaveSetting(); }
        }

        bool _录制隐藏 = true;
        public bool 录制隐藏
        {
            get { return _录制隐藏; }
            set { _录制隐藏 = value; SettingHelp.SaveSetting(); }
        }

        string _保存路径 = "Temp";
        public string 保存路径
        {
            get { return _保存路径; }
            set { _保存路径 = value; SettingHelp.SaveSetting(); }
        }

        string _编码类型 = "MSMPEG4v3";
        public string 编码类型
        {
            get { return _编码类型; }
            set { _编码类型 = value; SettingHelp.SaveSetting(); }
        }

        string _命名规则 = "yyMMdd_HHmmss";
        public string 命名规则
        {
            get { return _命名规则; }
            set { _命名规则 = value; SettingHelp.SaveSetting(); }
        }

        bool _跨屏录制 = false;
        public bool 跨屏录制
        {
            get { return _跨屏录制; }
            set { _跨屏录制 = value; SettingHelp.SaveSetting(); }
        }

        bool _捕获鼠标 = false;
        public bool 捕获鼠标
        {
            get { return _捕获鼠标; }
            set { _捕获鼠标 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 录制
        int _视频帧率 = 10;
        public int 视频帧率
        {
            get { return _视频帧率; }
            set { _视频帧率 = value; SettingHelp.SaveSetting(); }
        }

        int _视频质量 = 5;
        public int 视频质量
        {
            get { return _视频质量; }
            set { _视频质量 = value; SettingHelp.SaveSetting(); }
        }
        #endregion

        #region 热键
        Tuple<HotKey.KeyModifiers, int> _播放暂停 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Space);
        public Tuple<HotKey.KeyModifiers, int> 播放暂停
        {
            get { return _播放暂停; }
            set { _播放暂停 = value; SettingHelp.SaveSetting(); }
        }

        Tuple<HotKey.KeyModifiers, int> _停止关闭 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Escape);
        public Tuple<HotKey.KeyModifiers, int> 停止关闭
        {
            get { return _停止关闭; }
            set { _停止关闭 = value; SettingHelp.SaveSetting(); }
        }

        Tuple<HotKey.KeyModifiers, int> _开关画笔 = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.None, (int)System.Windows.Forms.Keys.F1);
        public Tuple<HotKey.KeyModifiers, int> 开关画笔
        {
            get { return _开关画笔; }
            set { _开关画笔 = value; SettingHelp.SaveSetting(); }
        }
        #endregion
    }
}
