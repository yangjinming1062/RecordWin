using System;
using System.IO;
using System.Windows;

namespace RecordWin
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            string zipPath = Path.Combine(AppContext.BaseDirectory, "RecordLid.zip");
            if (File.Exists(zipPath))//依赖文件以压缩包存储，启动时检测压缩包，如果存在则解压
            {
                var SevenZip = new SevenZipNET.SevenZipExtractor(zipPath);
                SevenZip.ExtractAll(AppContext.BaseDirectory);
                File.Delete(zipPath);//解压完成移除压缩包
            }
            MainWindow win = new MainWindow();
            win.Show();
        }
    }
}
