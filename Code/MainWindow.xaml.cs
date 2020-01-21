using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using NAudio.Wave;
using NReco.VideoConverter;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        private IntPtr winHandle;// 当前窗体指针
        private static readonly Duration Duration1 = (Duration)Application.Current.Resources["Duration1"];
        public MainWindow()
        {
            InitializeComponent();
            if (new FilterInfoCollection(FilterCategory.VideoInputDevice).Count < 1)//未检测到摄像头则不显示摄像头配置
            {
                cbSXT.Visibility = Visibility.Collapsed;
                SettingHelp.Settings.摄像头 = false;
            }
            if (WaveInEvent.DeviceCount < 1)
            {
                cbSY.Visibility = Visibility.Collapsed;
                SettingHelp.Settings.声音 = false;
            }
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
                        if (temp.Id != current.Id) temp.Kill();
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
            Top = S.Bounds.Top - 1;//使感应更好，不然放到边框的1像素上没反应很尴尬啊
        }
        /// <summary>
        /// 根据时间生成保存文件名称，文件位于Temp文件夹中
        /// </summary>
        /// <param name="Type">文件后缀,如.mp4</param>
        private string MakeFilePath(string Type, string Begin = "")
        {
            if (!Type.StartsWith(".")) Type = "." + Type;
            string path = Path.Combine(SettingHelp.Settings.保存路径, $"{Begin}{DateTime.Now.ToString(SettingHelp.Settings.命名规则)}{Type}");
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

        #region 基础事件
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (isRecording) StopRecord();
            base.OnClosing(e);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            winHandle = new WindowInteropHelper(this).Handle;
            hWndSource = HwndSource.FromHwnd(winHandle);
            ChangePlace();
            SetHotKey(true);
        }
        private void btClose_Click(object sender, RoutedEventArgs e) => Close();
        /// <summary>
        /// 拖动移动
        /// </summary>
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                if (!btDing.IsActived) ChangePlace();//未开自动隐藏则拖到哪算哪
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
        private TimeSpan videoSpan;//用来实时显示当前录制时长
        private VideoFileWriter videoWriter = new VideoFileWriter();//视频写入
        private ScreenCaptureStream videoStreamer;//视频捕获
        private WaveInEvent audioStreamer;//音频捕获
        private WaveFileWriter audioWriter;//音频写入
        private string curVideoName;//当前录制的视频路径
        private string curAudioName;//当前录制的音频路径
        private int FrameCount;//统计帧数计算时长，每凑够一个帧率的数时长加1s并重置回0

        #region 鼠标捕获
        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public Int32 x;
            public Int32 y;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }
        #endregion
        /// <summary>
        /// 桌面输出回调
        /// </summary>
        private void VideoNewFrame(object sender, NewFrameEventArgs e)
        {
            if (isRecording && !isParsing)
            {
                if (SettingHelp.Settings.捕获鼠标)
                {
                    var g = System.Drawing.Graphics.FromImage(e.Frame);
                    CURSORINFO pci;
                    pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                    GetCursorInfo(out pci);
                    try
                    {
                        System.Windows.Forms.Cursor cur = new System.Windows.Forms.Cursor(pci.hCursor);
                        cur.Draw(g, new System.Drawing.Rectangle(System.Windows.Forms.Cursor.Position.X - 10, System.Windows.Forms.Cursor.Position.Y - 10, cur.Size.Width, cur.Size.Height));
                    }
                    catch { }//打开任务管理器时会导致异常
                }
                videoWriter.WriteVideoFrame(e.Frame);
                //计算当前进度这个会拖慢视频录制进程,新开线程来处理进度显示
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    FrameCount += 1;
                    if (FrameCount == SettingHelp.Settings.视频帧率)//凑够一个帧组加1s1,一切努力都是为了使实际视频时长和显示的时长高度匹配
                    {
                        FrameCount = 0;
                        videoSpan = videoSpan.Add(new TimeSpan(0, 0, 0, 1));
                        Dispatcher.Invoke(new Action(() =>
                        {
                            lbTime.Content = videoSpan.ToString("hh\\:mm\\:ss");
                        }));
                    }
                });
            }
        }
        /// <summary>
        /// 音频回调
        /// </summary>
        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (isRecording && !isParsing) audioWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }
        private void BeginRecord()
        {
            curVideoName = MakeFilePath(".avi", "Raw");
            curAudioName = curVideoName.Replace(".avi", ".wav");//使音频文件和视频文件同名
            var curScreen = System.Windows.Forms.Screen.FromHandle(winHandle);
            videoSpan = new TimeSpan();
            lbTime.Content = videoSpan.ToString("hh\\:mm\\:ss");
            FrameCount = 0;
            int RecordWidth = 0, RecordHeight = 0;
            if (SettingHelp.Settings.跨屏录制)
            {
                foreach (var s in System.Windows.Forms.Screen.AllScreens)
                {
                    RecordWidth += s.Bounds.Width;
                    if (s.Bounds.Height > RecordHeight)
                        RecordHeight = s.Bounds.Height;
                }
            }
            else
            {
                RecordWidth = curScreen.Bounds.Width;
                RecordHeight = curScreen.Bounds.Height;
            }
            if (SettingHelp.Settings.桌面)
            {
                lock (this)
                {
                    videoWriter.Open(curVideoName, RecordWidth, RecordHeight, SettingHelp.Settings.视频帧率, VideoCodec.MSMPEG4v3,
                        curScreen.Bounds.Width * curScreen.Bounds.Height * SettingHelp.Settings.视频质量);
                }
                System.Drawing.Rectangle rec = new System.Drawing.Rectangle(SettingHelp.Settings.跨屏录制 ? 0 : curScreen.Bounds.X,
                    SettingHelp.Settings.跨屏录制 ? 0 : curScreen.Bounds.Y, RecordWidth, RecordHeight);
                videoStreamer = new ScreenCaptureStream(rec, 1000 / SettingHelp.Settings.视频帧率);//帧间隔需要和帧率关联，不然录的10秒视频文件不是10s
                videoStreamer.NewFrame += VideoNewFrame;
                videoStreamer.Start();
            }
            if (SettingHelp.Settings.摄像头)
            {
                var carmeraShow = new CameraShow(curVideoName);//如果录制桌面又开启摄像头则摄像头只在右下角显示用，不单独保存文件
                Visibility = SettingHelp.Settings.桌面 ? Visibility.Visible : Visibility.Collapsed;//当只录摄像头时隐藏主命令栏
                carmeraShow.Owner = this;
            }
            if (SettingHelp.Settings.声音)
            {
                audioStreamer = new WaveInEvent();
                audioStreamer.DataAvailable += AudioDataAvailable;
                audioWriter = new WaveFileWriter(curAudioName, audioStreamer.WaveFormat);
                audioStreamer.StartRecording();
            }

            isRecording = true;
            isParsing = false;
            if (SettingHelp.Settings.录制隐藏) HiddenTools(true);
            btSet.Visibility = Visibility.Collapsed;
            //waitBar.Visibility = Visibility.Collapsed;
            lbTime.Visibility = Visibility.Visible;
            ChangePlace();
            //TitleDragMove(false);
        }
        internal void StopRecord(bool ShowErr = true, bool CloseCamera = true)
        {
            try
            {
                if (CloseCamera && SettingHelp.Settings.摄像头)//摄像头关闭时调用该方法不需要再去关闭摄像头
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
                if (SettingHelp.Settings.声音)
                {
                    audioStreamer.StopRecording();
                    audioStreamer.Dispose();
                }
                isRecording = false;
                HiddenTools(SettingHelp.Settings.自动隐藏);
                btParse.Visibility = Visibility.Collapsed;
                btStop.Visibility = Visibility.Collapsed;
                btSet.Visibility = Visibility.Visible;
                lbTime.Visibility = Visibility.Collapsed;
                btClose.Visibility = Visibility.Visible;
                btBegin.Visibility = Visibility.Visible;
                waitBar.Value = 0;
                waitBar.Visibility = Visibility.Visible;
                //Convert后的MP4体积更小但清晰度没什么影响，所以无论有无声音都进行一次转换处理
                if (SettingHelp.Settings.桌面 || SettingHelp.Settings.摄像头)//没有视频则不转换
                {
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        string tempVideo = curVideoName, tempAudio = curAudioName;//运行未完成转换再次开始录制，所以这里需要把当前转换中的文件名记录下来
                        var ffMpeg = new FFMpegConverter();
                        ffMpeg.ConvertProgress += FfMpeg_ConvertProgress;
                        FFMpegInput[] input = SettingHelp.Settings.声音 ? new FFMpegInput[] { new FFMpegInput(tempVideo), new FFMpegInput(tempAudio) } : new FFMpegInput[] { new FFMpegInput(tempVideo) };
                        ffMpeg.ConvertMedia(input, MakeFilePath(SettingHelp.Settings.编码类型), SettingHelp.Settings.编码类型, new OutputSettings());
                        if (File.Exists(tempVideo) && !SettingHelp.Settings.保留视频) File.Delete(tempVideo);
                        if (File.Exists(tempAudio) && !SettingHelp.Settings.保留音频) File.Delete(tempAudio);//合成后移除音频文件
                        Dispatcher.Invoke(() =>
                            {
                                waitBar.Visibility = Visibility.Collapsed;
                            });
                    });
                }
            }
            catch (Exception ex)
            {
                if (ShowErr) Message(ex.Message);
            }
        }
        private void FfMpeg_ConvertProgress(object sender, ConvertProgressEventArgs e) => Dispatcher.Invoke(() => { waitBar.Value = (waitBar.Value + 1) % 10; });
        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelp.Settings.桌面 && !SettingHelp.Settings.摄像头 && !SettingHelp.Settings.声音)
            {
                Message("未选择任何录制源，请先选择录制内容");
                return;
            }
            try
            {
                if (isRecording)
                {
                    videoStreamer.Start();
                    isParsing = false;
                }
                else
                    BeginRecord();
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
            isParsing = true;
            videoStreamer.SignalToStop();
        }
        private void btStop_Click(object sender, RoutedEventArgs e) => StopRecord();
        #endregion

        #region 设置
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
                        Dispatcher.Invoke(() => {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        });
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
            }
        }
        #endregion

        #region 热键
        HwndSource hWndSource;
        int HotKeyBF;
        int HotKeyTZ;
        int HotKeyHB;
        private void SetHotKey(bool Add)
        {
            if (Add)
            {
                hWndSource.AddHook(MainWindowProc);
                HotKeyBF = HotKey.GlobalAddAtom($"{SettingHelp.Settings.播放暂停.Item1.ToString()}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2)}");
                HotKeyTZ = HotKey.GlobalAddAtom($"{SettingHelp.Settings.停止关闭.Item1.ToString()}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2)}");
                HotKeyHB = HotKey.GlobalAddAtom($"{SettingHelp.Settings.开关画笔.Item1.ToString()}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.开关画笔.Item2)}");

                HotKey.RegisterHotKey(winHandle, HotKeyBF, SettingHelp.Settings.播放暂停.Item1, SettingHelp.Settings.播放暂停.Item2);
                HotKey.RegisterHotKey(winHandle, HotKeyTZ, SettingHelp.Settings.停止关闭.Item1, SettingHelp.Settings.停止关闭.Item2);
                HotKey.RegisterHotKey(winHandle, HotKeyHB, SettingHelp.Settings.开关画笔.Item1, SettingHelp.Settings.开关画笔.Item2);
            }
            else//暂时没起作用，todo
            {
                hWndSource.RemoveHook(MainWindowProc);
                HotKey.GlobalDeleteAtom((short)HotKeyBF);
                HotKey.GlobalDeleteAtom((short)HotKeyTZ);
                HotKey.GlobalDeleteAtom((short)HotKeyHB);
                HotKey.UnregisterHotKey(winHandle, HotKeyBF);
                HotKey.UnregisterHotKey(winHandle, HotKeyTZ);
                HotKey.UnregisterHotKey(winHandle, HotKeyHB);
            }
        }
        private IntPtr MainWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HotKey.WM_HOTKEY:
                    {
                        int sid = wParam.ToInt32();
                        if (Visibility == Visibility.Visible)
                        {
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
                            if (sid == HotKeyHB)
                            {
                                btPen_Click(null, null);
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
