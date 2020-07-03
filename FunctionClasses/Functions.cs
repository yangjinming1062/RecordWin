using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;

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
        public static void Message(string msg) => MessageBox.Show(msg);

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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            foreach (PropertyInfo p in SettingHelp.Settings.GetType().GetProperties())//找到热键类属性，查找是否有冲突的热键设置
            {
                if (p.PropertyType.Equals(typeof(T)) && p.Name == propertyName)//先判断是指定类型
                {
                    p.SetValue(obj, value);
                }
            }
        } 
        #endregion
    }
}
