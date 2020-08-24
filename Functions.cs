using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Xml.Serialization;

namespace RecordWin
{
    public static class Functions
    {
        /// <summary>
        /// 调用cmd命令
        /// </summary>
        public static void CMD(string args)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = string.Format("cmd.exe");
            cmd.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            cmd.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            cmd.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            cmd.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            cmd.StartInfo.CreateNoWindow = true;//不显示程序窗口
            cmd.StartInfo.Verb = "runas";//设置启动动作,以管理员身份运行
            cmd.Start();//启动程序
            cmd.StandardInput.WriteLine(args + "&exit");
            cmd.StandardInput.AutoFlush = true;
            cmd.StandardInput.Close();
            cmd.BeginOutputReadLine();
            cmd.BeginErrorReadLine();
            cmd.WaitForExit();
            cmd.Close();
        }
        /// <summary>
        /// 统一消息提醒(方便后期调整消息框样式)
        /// </summary>
        public static void Message(string msg)
        {
            MessageBox.Show(msg);
        }

        #region 下载依赖包
        private static FileStream writeStream = null;
        private static Stream readStream = null;
        /// <summary>
        /// 下载录屏依赖文件
        /// </summary>
        public static bool DownRecordLid()
        {
            bool flag;
            string recordZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecordLid.7z");
            string recordLibURL = @"https://upgrade.1shitou.cn/pdt/app/pc/setup/Plus/RecordLid.zip";
            try
            {
                flag = DownloadUrl(recordLibURL, recordZipPath) && UnZipFile(recordZipPath);//成功下载并解压返回true
            }
            catch (Exception)
            {
                flag = false;
            }
            if (File.Exists(recordZipPath))
                File.Delete(recordZipPath);
            return flag && CheckRecordLid();
        }
        /// <summary>
        /// 检测必要依赖文件是否已经存在
        /// </summary>
        public static bool CheckRecordLid()
        {
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avcodec-53.dll"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avdevice-53.dll"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avfilter-2.dll"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avformat-53.dll"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avutil-51.dll"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"))
                && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "swscale-2.dll"));//检查必要的文件是否已经存在了，如果都存在了则认为成功
        }
        /// <summary>
        /// 下载文件
        /// </summary>
        private static bool DownloadUrl(string url, string localfile)
        {
            // 判断要下载的文件夹是否存在
            if (File.Exists(localfile)) return true;
            bool flag = false;

            if (!Directory.Exists(localfile)) Directory.CreateDirectory(Path.GetDirectoryName(localfile));

            try
            {
                for (int tryTime = 0; tryTime < 3; tryTime++)
                {
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);// 打开网络连接
                    myRequest.Timeout = 10000;
                    var rsp = (HttpWebResponse)myRequest.GetResponse();
                    if (rsp.StatusCode == HttpStatusCode.OK && rsp.ContentLength > 0)
                    {
                        if (rsp.ContentLength == 745) return false;
                        writeStream = new FileStream(localfile, FileMode.Create);// 文件不保存创建一个文件

                        readStream = rsp.GetResponseStream();// 向服务器请求,获得服务器的回应数据流
                        byte[] btArray = new byte[10240];// 定义一个字节数据,用来向readStream读取内容和向writeStream写入内容
                        int contentSize = readStream.Read(btArray, 0, btArray.Length);// 向远程文件读第一次
                        long currPostion = 0;//long startPosition = 0; // 上次下载的文件起始位置
                        while (contentSize > 0)// 如果读取长度大于零则继续读
                        {
                            currPostion += contentSize;
                            int percent = (int)(currPostion * 100 / rsp.ContentLength) - 1;
                            writeStream.Write(btArray, 0, contentSize);// 写入本地文件
                            contentSize = readStream.Read(btArray, 0, btArray.Length);// 继续向远程文件读取
                        }
                        //关闭流
                        writeStream.Close();
                        readStream.Close();

                        flag = true;        //返回true下载成功
                        break;
                    }
                    rsp.Close();
                }
            }
            catch (Exception)
            {
                writeStream?.Close();
                if (File.Exists(localfile))
                    File.Delete(localfile);
                flag = false;//返回false下载失败
            }
            return flag;
        }
        /// <summary>
        /// 解压文件
        /// </summary>
        public static bool UnZipFile(string filePath)
        {
            var flag = true;
            try
            {
                if (File.Exists(filePath))
                {
                    var SevenZip = new SevenZipNET.SevenZipExtractor(filePath);
                    SevenZip.ExtractAll(AppContext.BaseDirectory);
                    File.Delete(filePath);//解压完成移除压缩包
                }
            }
            catch (Exception)
            {
                return false;
            }
            return flag;
        }
        #endregion

        #region 读写类中指定属性的值——泛型T
        /// <summary>
        /// 取出指定中指定属性的值
        /// </summary>
        /// <typeparam name="T">待取出的属性的类型</typeparam>
        /// <param name="propertyName">属性名称</param>
        /// <param name="obj">待取出值的类</param>
        /// <returns>取出的值</returns>
        public static T GetKeyPropertyValue<T>(string propertyName, object obj)
        {
            foreach (PropertyInfo p in SettingHelp.Settings.GetType().GetProperties())//找到热键类属性，查找是否有冲突的热键设置
            {
                if (p.PropertyType.Equals(typeof(T)) && p.Name == propertyName)//先判断是指定类型
                {
                    return (T)p.GetValue(obj);
                }
            }
            return default;
        }
        /// <summary>
        /// 向目标类的指定属性赋值
        /// </summary>
        /// <typeparam name="T">属性的类型</typeparam>
        /// <param name="propertyName">属性名称</param>
        /// <param name="obj">待设置值的类</param>
        /// <param name="value">属性值</param>
        public static void SetKeyPropertyValue<T>(string propertyName, object obj, T value)
        {
            foreach (PropertyInfo p in obj.GetType().GetProperties())
            {
                if (p.PropertyType.Equals(typeof(T)) && p.Name == propertyName)//先判断是指定类型
                {
                    p.SetValue(obj, value);
                    break;
                }
            }
        }
        #endregion

        #region 文件保存/加载
        #region 二进制bat
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="oData">待保存数据</param>
        /// <param name="path">保存位置</param>
        public static bool SaveData<T>(T oData, string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (oData == null)
                return false;
            try
            {
                using FileStream stream = new FileStream(path, FileMode.Create)
                {
                    Position = 0
                };
                new BinaryFormatter().Serialize(stream, oData);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 以指定类型加载出指定路径的配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <returns>无法反序列化成指定类型或文件不存在等返回类型的默认值</returns>
        public static T GetData<T>(string path)
        {
            T t = default;
            if (File.Exists(path))
            {
                try
                {
                    using FileStream stream = new FileStream(path, FileMode.Open)
                    {
                        Position = 0
                    };
                    t = (T)new BinaryFormatter().Deserialize(stream);
                }
                catch//异常说明之前保存的数据结构和现在的有了差异，删除旧文件
                {
                    File.Delete(path);
                }
            }
            return t;
        }
        #endregion

        #region XML
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="oData">待保存数据</param>
        /// <param name="path">保存位置</param>
        public static bool SaveDataXML<T>(T oData, string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (oData == null)
                return false;
            try
            {
                using FileStream stream = new FileStream(path, FileMode.Create);
                XmlSerializer xmlserilize = new XmlSerializer(typeof(T));
                xmlserilize.Serialize(stream, oData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 以指定类型加载出指定路径的配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <returns>无法反序列化成指定类型或文件不存在等返回类型的默认值</returns>
        public static T GetDataXML<T>(string path)
        {
            T t = default;
            if (File.Exists(path))
            {
                try
                {
                    using FileStream stream = new FileStream(path, FileMode.Open);
                    XmlSerializer xmlserilize = new XmlSerializer(typeof(T));
                    t = (T)xmlserilize.Deserialize(stream);
                }
                catch (Exception)//异常说明之前保存的数据结构和现在的有了差异，删除旧文件
                {
                    File.Delete(path);
                }
            }
            return t;
        }
        #endregion

        #region Json -- 需要安装Json包
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="oData">待保存数据</param>
        /// <param name="path">保存位置</param>
        public static bool SaveDataJson<T>(T oData, string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (oData == null)
                return false;
            try
            {
                //string json = JsonConvert.SerializeObject(oData);
                using StreamWriter streamWriter = new StreamWriter(path, false, System.Text.Encoding.UTF8);
                //streamWriter.Write(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 以指定类型加载出指定路径的配置
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <returns>无法反序列化成指定类型或文件不存在等返回类型的默认值</returns>
        public static T GetDataJson<T>(string path)
        {
            T t = default;
            if (File.Exists(path))
            {
                try
                {
                    using StreamReader streamReader = new StreamReader(path, System.Text.Encoding.UTF8);
                    string json = streamReader.ReadToEnd();
                    //t = JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception)//异常说明之前保存的数据结构和现在的有了差异，删除旧文件
                {
                    File.Delete(path);
                }
            }
            return t;
        }
        #endregion
        #endregion
    }
}
