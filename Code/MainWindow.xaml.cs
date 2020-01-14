using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace RecordWin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Duration Duration1 = (Duration)Application.Current.Resources["Duration1"];
        public MainWindow()
        {
            InitializeComponent();
            lbTime.Visibility = Visibility.Collapsed;
            HiddenTools(SettingHelp.Settings.自动隐藏);
            cbZM.IsChecked = SettingHelp.Settings.桌面;
            cbSXT.IsChecked = SettingHelp.Settings.摄像头;
            cbSY.IsChecked = SettingHelp.Settings.声音;
            if (true)//不允许多开，如果已经有一个录屏进程存在则干死它……
            {
                Process current = Process.GetCurrentProcess();
                //防止程序被改名，按文件的名称去查找
                Process[] pro = Process.GetProcessesByName(current.ProcessName);
                try
                {
                    foreach (Process temp in pro)
                    {
                        if (temp.Id != current.Id)
                            temp.Kill();
                    }
                }
                catch { }
            }
        }

        #region 基础方法
        /// <summary>
        /// 定位到当前屏幕的顶部中间位置
        /// </summary>
        private void ChangePlace()
        {
            var S = System.Windows.Forms.Screen.FromHandle(winHandle);
            Left = S.Bounds.Left + (S.Bounds.Width - TitlePanel.ActualWidth) / 2;
            Top = S.Bounds.Top;
        }
        /// <summary>
        /// 根据时间生成保存文件名称，文件位于tmp文件夹中
        /// </summary>
        /// <param name="Type">文件后缀，需要带点，如.mp4</param>
        private string MakeFilePath(string Type)
        {
            string path = Path.Combine("Temp", $"{DateTime.Now.ToString("yyMMdd_HHmmss")}{Type}");
            if (!Directory.Exists(path)) Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }
        /// <summary>
        /// 统一消息提醒(方便后期调整消息框样式)
        /// </summary>
        private void Message(string msg)
        {
            System.Windows.MessageBox.Show(msg);
        }
        /// <summary>
        /// 工具栏显隐状态
        /// </summary>
        private void HiddenTools(bool? Hidden = null)
        {
            if (Hidden.HasValue) btDing.IsActived = !Hidden.Value;//参数赋值了则设置
            DingRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, new DoubleAnimation(btDing.IsActived ? -45 : 0, Duration1));//钉住按钮执行动画
            TitlePanel.Height = !btDing.IsActived && !SettingPop.IsOpen ? 3 : 40;//通过修改高度使动画效果出现与否来实现显隐
        }
        /// <summary>
        /// 工具栏是否可拖动
        /// </summary>
        public void TitleDragMove(bool move)
        {
            if (move && !SettingPop.IsOpen && !btPen.IsActived)
            {
                TitlePanel.MouseDown += Title_MouseDown;
                TitlePanel.Cursor = Cursors.SizeAll;
            }
            else
            {
                TitlePanel.MouseDown -= Title_MouseDown;
                TitlePanel.Cursor = Cursors.Arrow;
            }
        }
        #endregion

        #region 事件
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.isRecording) StopRecord();
            base.OnClosing(e);
        }
        /// <summary>
        /// 窗体加载后自动定位
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            winHandle = new WindowInteropHelper(this).Handle;
            hWndSource = HwndSource.FromHwnd(winHandle);
            ChangePlace();
            SetHotKey(true);
        }
        /// <summary>
        /// 关闭程序
        /// </summary>
        private void btClose_Click(object sender, RoutedEventArgs e) => Close();
        /// <summary>
        /// 拖动移动
        /// </summary>
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                if (SettingHelp.Settings.自动隐藏) ChangePlace();//未开自动隐藏则拖到哪算哪
            }
        }
        /// <summary>
        /// 是否隐藏
        /// </summary>
        private void btDing_Click(object sender, RoutedEventArgs e)
        {
            btDing.IsActived = !btDing.IsActived;//点击后切换状态
            SettingHelp.Settings.自动隐藏 = !btDing.IsActived;//激活时不隐藏，所以取反
            if (SettingHelp.Settings.自动隐藏) ChangePlace();
            HiddenTools();
        }
        /// <summary>
        /// 防止UC跑丢，我就死活一个状态（针对Win10拖到屏幕边缘可被最大化问题）
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e) => WindowState = WindowState.Normal;
        #endregion

        #region 录制
        private bool isRecording;
        private bool isParsing;
        private Stopwatch stopWatch = new Stopwatch();//用来实时显示当前录制时长
        private VideoFileWriter videoWriter = new VideoFileWriter();
        private ScreenCaptureStream videoStreamer;
        private static RecordSound recordSound = new RecordSound();//录音
        private string curVedioName;
        /// <summary>
        /// 桌面输出回调
        /// </summary>
        private void video_NewFrame(object sender, NewFrameEventArgs e)
        {
            if (this.isRecording && !isParsing)
            {
                this.videoWriter.WriteVideoFrame(e.Frame);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.lbTime.Content = this.stopWatch.Elapsed.ToString("hh\\:mm\\:ss");
                }));
            }
        }

        private void BeginRecord()
        {
            curVedioName = MakeFilePath(".mp4");
            if (SettingHelp.Settings.桌面)
            {
                var curScreen = System.Windows.Forms.Screen.FromHandle(winHandle);
                lock (this)
                {
                    this.videoWriter.Open(curVedioName, curScreen.Bounds.Width, curScreen.Bounds.Height, 
                        SettingHelp.Settings.视频帧率, VideoCodec.MPEG4, 
                        curScreen.Bounds.Width * curScreen.Bounds.Height * SettingHelp.Settings.视频质量);
                }
                this.videoStreamer = new ScreenCaptureStream(curScreen.Bounds, 1000 / SettingHelp.Settings.视频帧率);
                this.videoStreamer.NewFrame += new NewFrameEventHandler(video_NewFrame);
                this.videoStreamer.Start();
            }
            if (SettingHelp.Settings.摄像头)
            {
                var carmeraShow = new CameraShow(SettingHelp.Settings.桌面 ? "" : curVedioName);//如果录制桌面又开启摄像头则摄像头只在右下角显示用，不单独保存文件
                Visibility = SettingHelp.Settings.桌面 ? Visibility.Visible : Visibility.Collapsed;//当只录摄像头时隐藏主命令栏
                carmeraShow.Owner = this;
                carmeraShow.Show();
            }
            if (SettingHelp.Settings.声音)
                recordSound.StartRecordSound();
            this.isRecording = true;
            this.isParsing = false;
            if (SettingHelp.Settings.播放隐藏) HiddenTools(true);
            btSet.Visibility = Visibility.Hidden;
            lbTime.Visibility = Visibility.Visible;
            ChangePlace();
            TitleDragMove(false);
        }

        internal void StopRecord(bool ShowErr = true)
        {
            try
            {
                this.Visibility = Visibility.Visible;//防止调用CameraShow的Close死循环
                if (SettingHelp.Settings.摄像头)
                {
                    foreach (Window shower in Application.Current.Windows)
                    {
                        if (shower is CameraShow)
                        {
                            shower.Close();
                            break;
                        }
                    }
                }
                if (SettingHelp.Settings.桌面)
                {
                    videoStreamer.Stop();
                    videoWriter.Close();
                }
                string audioName = MakeFilePath(".wav");
                if (SettingHelp.Settings.声音)
                {
                    recordSound.EndRecordSound(audioName);
                }

                #region 音视频合成
                if (SettingHelp.Settings.桌面 || SettingHelp.Settings.摄像头)//有视频源
                {
                    if (SettingHelp.Settings.声音)//又有声音,则合成
                    {
                        //todo
                    }
                }
                #endregion

                this.isRecording = false;
                stopWatch.Reset();
                HiddenTools(SettingHelp.Settings.自动隐藏);
                btBegin.Visibility = Visibility.Visible;
                btParse.Visibility = Visibility.Collapsed;
                btClose.Visibility = Visibility.Visible;
                btStop.Visibility = Visibility.Collapsed;
                btSet.Visibility = Visibility.Visible;
                lbTime.Visibility = Visibility.Collapsed;
                TitleDragMove(true);
            }
            catch (Exception ex)
            {
                if (ShowErr)
                    Message(ex.Message);
            }
        }

        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SettingHelp.Settings.桌面 && !SettingHelp.Settings.摄像头 && !SettingHelp.Settings.声音)
                {
                    Message("未选择任何录制源，请先选择录制内容");
                    return;
                }
                if (this.isRecording)
                {
                    videoStreamer.Start();
                    this.isParsing = false;
                }
                else
                    BeginRecord();
                this.stopWatch.Start();
                btBegin.Visibility = Visibility.Collapsed;
                btParse.Visibility = Visibility.Visible;
                btClose.Visibility = Visibility.Collapsed;
                btStop.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StopRecord(false);
                Message(ex.Message);
            }
        }

        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            this.isParsing = true;
            videoStreamer.SignalToStop();
            this.stopWatch.Stop();
        }

        private void btStop_Click(object sender, RoutedEventArgs e) => StopRecord();
        #endregion

        #region 设置
        /// <summary>
        /// 当前窗体指针
        /// </summary>
        private IntPtr winHandle;
        HwndSource hWndSource;
        int HotKeyBF;
        int HotKeyTZ;
        /// <summary>
        /// 打开设置窗口
        /// </summary>
        private void btSet_Click(object sender, RoutedEventArgs e)
        {
            TitlePanel.Height = SettingPop.IsOpen && SettingHelp.Settings.自动隐藏 ? 3 : 40;
            SettingPop.IsOpen = !SettingPop.IsOpen;
            btSet.IsActived = SettingPop.IsOpen;
            btBegin.Visibility = SettingPop.IsOpen ? Visibility.Hidden : Visibility.Visible;
            TitleDragMove(!SettingPop.IsOpen);//根据设置Popup决定是否可以拖住，当正在设置时不允许拖拽（会和设置小窗窗窗分离）
        }

        private void cbSY_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.声音 = cbSY.IsChecked.Value;
        private void cbSXT_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.摄像头 = cbSXT.IsChecked.Value;
        private void cbZM_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.桌面 = cbZM.IsChecked.Value;

        private void btMoreSet_Click(object sender, RoutedEventArgs e)
        {
            SetHotKey(false);//开始设置前先把当前热键卸载
            new SettingWindow().ShowDialog();
            SetHotKey(true);//重新加载（可能）新的热键设置
        }

        private void SetHotKey(bool Add)
        {
            if (Add)
            {
                hWndSource.AddHook(MainWindowProc);
                HotKeyBF = HotKey.GlobalAddAtom($"{SettingHelp.Settings.播放暂停.Item1.ToString()}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2)}");
                HotKeyTZ = HotKey.GlobalAddAtom($"{SettingHelp.Settings.停止关闭.Item1.ToString()}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2)}");
                
                HotKey.RegisterHotKey(winHandle, HotKeyBF, SettingHelp.Settings.播放暂停.Item1, SettingHelp.Settings.播放暂停.Item2);
                HotKey.RegisterHotKey(winHandle, HotKeyTZ, SettingHelp.Settings.停止关闭.Item1, SettingHelp.Settings.停止关闭.Item2);
            }
            else//暂时没起作用，todo
            {
                hWndSource.RemoveHook(MainWindowProc);
                HotKey.GlobalDeleteAtom((short)HotKeyBF);
                HotKey.GlobalDeleteAtom((short)HotKeyTZ);
                HotKey.UnregisterHotKey(winHandle, HotKeyBF);
                HotKey.UnregisterHotKey(winHandle, HotKeyTZ);
            }
        }
        #endregion

        #region 画笔
        private void btPen_Click(object sender, RoutedEventArgs e)
        {
            if (btPen.IsActived)
            {
                foreach (Window drawer in Application.Current.Windows)
                {
                    if (drawer is DrawerWindow)
                    {
                        drawer.Close();
                        btPen.IsActived = false;
                        return;
                    }
                }
            }
            else
            {
                DrawerWindow win = new DrawerWindow();
                btPen.IsActived = true;
                win.Owner = this;
                win.Show();
                TitleDragMove(false);
            }
        }
        #endregion

        #region 键盘操作
        private IntPtr MainWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HotKey.WM_HOTKEY:
                    {
                        int sid = wParam.ToInt32();
                        if (!SettingPop.IsOpen)
                        {
                            if (sid == HotKeyBF)
                            {
                                if (btBegin.Visibility == Visibility.Visible)
                                    btBegin_Click(null, null);
                                else
                                    btParse_Click(null, null);
                            }
                            else if (sid == HotKeyTZ)
                            {
                                if (btStop.Visibility == Visibility.Visible)
                                    btStop_Click(null, null);
                                else
                                    btClose_Click(null, null);
                            }
                        }
                        handled = true;
                        break;
                    }
            }
            return IntPtr.Zero;
        }
        #endregion
    }
}
