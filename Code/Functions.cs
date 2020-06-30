using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
