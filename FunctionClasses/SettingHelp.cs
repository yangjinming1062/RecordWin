﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RecordWin
{
    internal static class SettingHelp
    {
        /// <summary>
        /// 配置文件存储路径（根目录下）
        /// </summary>
        private static readonly string filePath = "Setting.dat";
        /// <summary>
        /// 构造函数
        /// </summary>
        static SettingHelp() => GetSetting();
        /// <summary>
        /// 参数配置
        /// </summary>
        internal static Setting Settings { get; private set; } = new Setting();
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
                new BinaryFormatter().Serialize(stream, Settings);
            }
        }
        /// <summary>
        /// 获取配置，当前配置路径如果不存在则创建默认配置文件，存在则加载已有配置
        /// </summary>
        private static void GetSetting()
        {
            if (File.Exists(filePath))//如果存在配置则加载配置文件
            {
                if (Path.GetDirectoryName(filePath).Length > 0)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    try
                    {
                        Settings = (Setting)new BinaryFormatter().Deserialize(stream);
                    }
                    catch//因为配置类变化等关系导致原有配置文件无法正常序列化则新生成配置文件
                    {
                        Settings = new Setting();
                        SaveSetting();
                    }
                }
            }
            else
                SaveSetting();
        }

        [Serializable]
        public class Setting
        {
            #region 录制源
            public bool 桌面 { get; set; } = true;
            public bool 摄像头 { get; set; } = true;
            public bool 声音 { get; set; } = true;
            #endregion

            #region 功能
            public bool 自动隐藏 { get; set; } = false;
            public bool 录制隐藏 { get; set; } = true;
            public string 保存路径 { get; set; } = "Temp";
            public string 编码类型 { get; set; } = "mp4";
            public string 命名规则 { get; set; }
            public bool 跨屏录制 { get; set; } = false;
            public bool 捕获鼠标 { get; set; } = false;
            public bool 保留视频 { get; set; } = false;
            public bool 保留音频 { get; set; } = false;
            #endregion

            #region 录制
            public int 视频帧率 { get; set; } = 10;
            public int 视频质量 { get; set; } = 5;
            public string 摄像头Key { get; set; }
            public int 摄像头参数 { get; set; } = 0;
            #endregion

            #region 热键
            public Tuple<HotKey.KeyModifiers, int> 播放暂停 { get; set; } = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Space);
            public Tuple<HotKey.KeyModifiers, int> 停止关闭 { get; set; } = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.Shift, (int)System.Windows.Forms.Keys.Escape);
            public Tuple<HotKey.KeyModifiers, int> 开关画笔 { get; set; } = new Tuple<HotKey.KeyModifiers, int>(HotKey.KeyModifiers.None, (int)System.Windows.Forms.Keys.F1);
            #endregion
        }
    }
}
