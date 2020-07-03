using AForge.Video.DirectShow;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RecordWin
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
            cbPlayHidden.IsChecked = SettingHelp.Settings.录制隐藏;
            cbMouse.IsChecked = SettingHelp.Settings.捕获鼠标;
            cbRawVideo.IsChecked = SettingHelp.Settings.保留视频;
            cbRawAudio.IsChecked = SettingHelp.Settings.保留音频;
            var devs = new FilterInfoCollection(FilterCategory.VideoInputDevice);//获取摄像头列表 
            if (devs.Count != 0)
            {
                cbSXT.ItemsSource = devs;
                cbSXT.SelectedIndex = 0;
                cbSXT_DropDownClosed(null, null);
            }
            if (System.Windows.Forms.Screen.AllScreens.Length < 2)//没有多个屏幕则不显示
            {
                cbMultiScreen.IsEnabled = false;
                SettingHelp.Settings.跨屏录制 = false;
            }
            cbMultiScreen.IsChecked = SettingHelp.Settings.跨屏录制;
            hotkeyBF.SetValue("播放/暂停：", "播放暂停");
            hotkeyTZ.SetValue("停止/关闭：", "停止关闭");
            hotkeyHB.SetValue("开/关画笔：", "开关画笔");
            cbVideoCode.Text = SettingHelp.Settings.编码类型;
            slZHiLiang.Value = SettingHelp.Settings.视频质量;
            slZHiLiang.ValueChanged += SlZL_ValueChanged;//必须放在Text赋值后再加载事件
            txtSavePath.Text = SettingHelp.Settings.保存路径;
            txtSavePath.TextChanged += txtSavePath_TextChanged;
            txtNameRule.Text = SettingHelp.Settings.命名规则;
            txtNameRule.TextChanged += txtNameRule_TextChanged;
            switch (SettingHelp.Settings.视频帧率)
            {
                case 10: btZLL.IsChecked = true; break;
                case 20: btZLM.IsChecked = true; break;
                case 30: btZLH.IsChecked = true; break;
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SettingHelp.SaveSetting();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        #region 录制设置
        private void cbPlayHidden_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.录制隐藏 = cbPlayHidden.IsChecked.Value;

        private void SlZL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            slZHiLiang.Value = (int)slZHiLiang.Value;//将小数值变成整数
            SettingHelp.Settings.视频质量 = (int)slZHiLiang.Value;
        }

        private void btZL_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as ToggleButton).Name)
            {
                case "btZLL": SettingHelp.Settings.视频帧率 = 10; break;
                case "btZLM": SettingHelp.Settings.视频帧率 = 20; break;
                case "btZLH": SettingHelp.Settings.视频帧率 = 30; break;
            }
        }

        private void SavePath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK) txtSavePath.Text = folderBrowser.SelectedPath;
        }

        private void txtSavePath_TextChanged(object sender, TextChangedEventArgs e) => SettingHelp.Settings.保存路径 = txtSavePath.Text;

        private void cbMultiScreen_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.跨屏录制 = cbMultiScreen.IsChecked.Value;

        private void cbMouse_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.捕获鼠标 = cbMouse.IsChecked.Value;

        private void cbSXT_DropDownClosed(object sender, EventArgs e)
        {
            if (cbSXT.SelectedItem is FilterInfo info)
            {
                var Camera = new VideoCaptureDevice(info.MonikerString);//实例化设备控制类(我选了第1个)
                SettingHelp.Settings.摄像头Key = info.MonikerString;
                cbSXTcs.Items.Clear();
                foreach (var cap in Camera.VideoCapabilities)
                {
                    cbSXTcs.Items.Add(new TextBlock
                    {
                        Text = $"{cap.FrameSize.Width}X{cap.FrameSize.Height}"
                    });
                }
                cbSXTcs.SelectedIndex = 0;
                SettingHelp.Settings.摄像头参数 = 0;
            }
        }

        private void cbSXTcs_DropDownClosed(object sender, EventArgs e)
        {
            if (cbSXTcs.SelectedItem != null) SettingHelp.Settings.摄像头参数 = cbSXTcs.SelectedIndex;
        }
        #endregion

        #region 高级设置
        private void txtNameRule_TextChanged(object sender, TextChangedEventArgs e) => SettingHelp.Settings.命名规则 = txtNameRule.Text;

        private void cbVideoCode_DropDownClosed(object sender, EventArgs e) => SettingHelp.Settings.编码类型 = cbVideoCode.Text;

        private void cbRawVideo_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.保留视频 = cbRawVideo.IsChecked.Value;

        private void cbRawAudio_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.保留音频 = cbRawAudio.IsChecked.Value;
        #endregion
    }
}
