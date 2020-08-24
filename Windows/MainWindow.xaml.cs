using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using NAudio.Wave;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
        #region 变量
        /// <summary>
        /// 当前窗体指针
        /// </summary>
        public IntPtr winHandle;
        /// <summary>
        /// 动画执行时间
        /// </summary>
        private static readonly Duration Duration1 = (Duration)Application.Current.Resources["Duration1"];
        /// <summary>
        /// 当前窗体所在屏幕
        /// </summary>
        private System.Windows.Forms.Screen CurrentScreen;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            if (new FilterInfoCollection(FilterCategory.VideoInputDevice).Count < 1)//未检测到摄像头则不允许录制摄像头
            {
                cbSXT.IsEnabled = false;//不隐藏是为了表示有该项功能
                SettingHelp.Settings.摄像头 = false;
            }
            if (WaveInEvent.DeviceCount < 1)//没有麦克风不允许录音
            {
                cbSY.IsEnabled = false;//不隐藏是为了表示有该项功能
                SettingHelp.Settings.声音 = false;
            }
            HiddenTools(SettingHelp.Settings.自动隐藏);
            cbZM.IsChecked = SettingHelp.Settings.桌面;
            cbSXT.IsChecked = SettingHelp.Settings.摄像头;
            cbSY.IsChecked = SettingHelp.Settings.声音;
            new Mutex(true, "RecordWin", out bool canRun);//已有则不允许新开
            if (!canRun)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        #region 私有方法
        /// <summary>
        /// 定位到当前屏幕的顶部中间位置
        /// </summary>
        private void GoToScreenTopMiddle()
        {
            CurrentScreen = System.Windows.Forms.Screen.FromHandle(winHandle);//根据窗口指针获取所属屏幕
            Left = CurrentScreen.Bounds.Left + (CurrentScreen.Bounds.Width - TitlePanel.ActualWidth) / 2;
            Top = CurrentScreen.Bounds.Top - 1;//使感应更好，不然放到边框的1像素上没反应很尴尬啊
        }
        /// <summary>
        /// 根据“命名规则”生成保存文件名称，文件位于“保存路径”文件夹中
        /// </summary>
        /// <param name="Type">文件后缀,如.mp4</param>
        /// <param name="Begin">文件名称前缀</param>
        private string MakeFilePath(string Type, string Begin = "")
        {
            if (!Type.StartsWith("."))//文件类型需要以.开头
            {
                Type = "." + Type;
            }
            string name = string.IsNullOrEmpty(SettingHelp.Settings.命名规则) ? DateTime.Now.ToString("yyMMdd_HHmmss") : SettingHelp.Settings.命名规则;
            string path = Path.Combine(SettingHelp.Settings.保存路径, $"{Begin}{name}{Type}");
            if (!Directory.Exists(path))//如果指定文件夹不存在则新建文件夹
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            return path;
        }
        /// <summary>
        /// 工具栏显隐状态
        /// </summary>
        /// <param name="Hidden">工具栏显隐，默认null表示只调用不修改现有值</param>
        private void HiddenTools(bool? Hidden = null)
        {
            if (Hidden.HasValue) btDing.IsChecked = !Hidden.Value;//参数赋值了则设置
            DingRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, 
                new DoubleAnimation((bool)btDing.IsChecked ? 0 : 45, Duration1));//钉住按钮执行动画
            TitlePanel.Height = !(bool)btDing.IsChecked && !SettingPop.IsOpen ? 3 : 40;//通过修改高度使动画效果出现与否来实现显隐
        }
        /// <summary>
        /// 工具栏是否可拖动
        /// </summary>
        private void TitleDragMove(bool canMove)
        {
            if (canMove && !SettingPop.IsOpen && !(bool)btPen.IsChecked)
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

        #region UI事件
        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (IsRecording) //如果正在录制中关闭则不保存文件退出
                StopRecord(SaveFile: false);
            base.OnClosing(e);
        }
        /// <summary>
        /// 窗体加载完成事件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            winHandle = new WindowInteropHelper(this).Handle;
            hWndSource = HwndSource.FromHwnd(winHandle);
            GoToScreenTopMiddle();
            SetHotKey(true);
        }
        /// <summary>
        /// 防止UC跑丢，我就死活一个状态（针对Win10拖到屏幕边缘可被最大化问题）
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            WindowState = WindowState.Normal;
        }
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// 拖动移动
        /// </summary>
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                if (btDing.IsChecked == false)
                    GoToScreenTopMiddle();//未开自动隐藏则拖到哪算哪
            }
        }
        /// <summary>
        /// 固定命令栏按钮点击事件
        /// </summary>
        private void BtDing_Click(object sender, RoutedEventArgs e)
        {
            SettingHelp.Settings.自动隐藏 = !(bool)btDing.IsChecked;//钉住不隐藏，所以取反
            if (SettingHelp.Settings.自动隐藏) GoToScreenTopMiddle();
            HiddenTools();
        }
        #endregion

        #region 录制
        #region 变量
        /// <summary>
        /// 是否正在录制
        /// </summary>
        private bool IsRecording;
        /// <summary>
        /// 是否暂停中
        /// </summary>
        private bool IsParsing;
        /// <summary>
        /// 视频写入
        /// </summary>
        private readonly VideoFileWriter VideoWriter = new VideoFileWriter();
        /// <summary>
        /// 视频捕获
        /// </summary>
        private ScreenCaptureStream VideoStreamer;
        /// <summary>
        /// 音频捕获
        /// </summary>
        private WaveInEvent AudioStreamer;
        /// <summary>
        /// 音频写入
        /// </summary>
        private WaveFileWriter AudioWriter;
        /// <summary>
        /// 当前录制的视频路径
        /// </summary>
        private string CurrentVideoPath;
        /// <summary>
        /// 当前录制的音频路径
        /// </summary>
        private string CurrentAudioPath;
        /// <summary>
        /// 已经录制帧数：统计帧数计算时长，每凑够一个帧率的数时长加1s并重置回0
        /// </summary>
        private int FrameCount;
        /// <summary>
        /// 录制开始时间点
        /// </summary>
        private DateTime beginTime;
        /// <summary>
        /// 单次暂停时长
        /// </summary>
        private TimeSpan parseSpan;
        /// <summary>
        /// 暂停时间点：计算parseSpan用
        /// </summary>
        private DateTime parseTime;
        /// <summary>
        /// 摄像头窗口
        /// </summary>
        private CameraShowWindow CarmeraShowWin = null;
        #endregion

        #region 鼠标捕获
        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }
        #endregion

        #region 音/视频输出处理
        /// <summary>
        /// 桌面输出回调处理
        /// </summary>
        private void VideoNewFrameHandle(object sender, NewFrameEventArgs e)
        {
            if (IsRecording && !IsParsing)
            {
                TimeSpan time = DateTime.Now - beginTime - parseSpan;
                if (SettingHelp.Settings.捕获鼠标)
                {
                    CURSORINFO pci;
                    pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                    GetCursorInfo(out pci);
                    try
                    {
                        System.Windows.Forms.Cursor cur = new System.Windows.Forms.Cursor(pci.hCursor);//在每一帧上手动绘制鼠标
                        cur.Draw(Graphics.FromImage(e.Frame), new Rectangle(System.Windows.Forms.Cursor.Position.X - 10 - CurrentScreen.Bounds.X,
                            System.Windows.Forms.Cursor.Position.Y - 10 - CurrentScreen.Bounds.Y, cur.Size.Width, cur.Size.Height));
                    }
                    catch { }//打开任务管理器时会导致异常
                }
                VideoWriter.WriteVideoFrame(e.Frame, time);//处理视频时长和实际物理时长不符，用帧间隔时长的办法指定每一帧的间隔
                FrameCount += 1;
                if (FrameCount == SettingHelp.Settings.视频帧率)
                {
                    FrameCount = 0;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        lbTime.Content = time.ToString("hh\\:mm\\:ss");
                    }));
                }
            }
        }
        /// <summary>
        /// 音频回调处理
        /// </summary>
        private void AudioDataAvailableHandle(object sender, WaveInEventArgs e)
        {
            if (IsRecording && !IsParsing)
            {
                AudioWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }
        #endregion

        /// <summary>
        /// 开始录制
        /// </summary>
        private void BeginRecord()
        {
            //重编码体积更小，但清晰度受影响，不录制声音时直接输出MP4不再ffmpeg处理
            CurrentVideoPath = MakeFilePath(".mp4", SettingHelp.Settings.声音 ? "Raw" : "");
            CurrentAudioPath = CurrentVideoPath.Replace(".mp4", ".wav");//使音频文件和视频文件同名
            var curScreen = System.Windows.Forms.Screen.FromHandle(winHandle);
            parseSpan = new TimeSpan();
            lbTime.Content = "00:00:00";
            FrameCount = 0;
            int RecordWidth = 0, RecordHeight = 0, RecordTop = 0, RecordLeft = 0;
            if (SettingHelp.Settings.跨屏录制)
            {
                foreach (var s in System.Windows.Forms.Screen.AllScreens)
                {
                    RecordWidth += Math.Abs(s.Bounds.Width);
                    if (Math.Abs(s.Bounds.Height) > RecordHeight)
                        RecordHeight = Math.Abs(s.Bounds.Height);
                    RecordLeft = Math.Min(s.Bounds.X, RecordLeft);
                    RecordTop = Math.Min(s.Bounds.Y, RecordTop);
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
                    VideoWriter.Open(CurrentVideoPath, RecordWidth, RecordHeight, SettingHelp.Settings.视频帧率, VideoCodec.MPEG4,
                        curScreen.Bounds.Width * curScreen.Bounds.Height * SettingHelp.Settings.视频质量);
                }
                Rectangle rec = SettingHelp.Settings.跨屏录制 ? new Rectangle(RecordLeft, RecordTop, RecordWidth, RecordHeight) :
                    new Rectangle(curScreen.Bounds.X, curScreen.Bounds.Y, RecordWidth, RecordHeight);
                VideoStreamer = new ScreenCaptureStream(rec, 1000 / SettingHelp.Settings.视频帧率);//帧间隔需要和帧率关联，不然录的10秒视频文件不是10s
                VideoStreamer.NewFrame += VideoNewFrameHandle;
                beginTime = DateTime.Now;
                VideoStreamer.Start();
            }
            if (SettingHelp.Settings.摄像头)
            {
                CarmeraShowWin = new CameraShowWindow(CurrentVideoPath, CurrentScreen);//如果录制桌面又开启摄像头则摄像头只在右下角显示用，不单独保存文件
                Visibility = SettingHelp.Settings.桌面 ? Visibility.Visible : Visibility.Collapsed;//当只录摄像头时隐藏主命令栏
                CarmeraShowWin.Owner = this;
            }
            if (SettingHelp.Settings.声音)
            {
                AudioStreamer = new WaveInEvent();
                AudioStreamer.DataAvailable += AudioDataAvailableHandle;
                AudioWriter = new WaveFileWriter(CurrentAudioPath, AudioStreamer.WaveFormat);
                AudioStreamer.StartRecording();
            }

            IsRecording = true;
            IsParsing = false;
            if (SettingHelp.Settings.录制隐藏) HiddenTools(true);
            btSet.Visibility = Visibility.Collapsed;
            lbTime.Visibility = Visibility.Visible;
            GoToScreenTopMiddle();
            //TitleDragMove(false);
        }
        /// <summary>
        /// 停止录制
        /// </summary>
        /// <param name="ShowErr">当发生异常时是否显示错误提示</param>
        /// <param name="CloseCamera">是否关闭摄像头窗口</param>
        /// <param name="SaveFile">是否保留输出文件：可以放弃录制</param>
        internal void StopRecord(bool ShowErr = true, bool CloseCamera = true, bool SaveFile = true)
        {
            try
            {
                IsRecording = false;
                if (CloseCamera && SettingHelp.Settings.摄像头)//摄像头关闭时调用该方法不需要再去关闭摄像头
                {
                    CarmeraShowWin?.Close();//如果摄像头窗口不为空则调用关闭方法关闭窗口
                }
                if (SettingHelp.Settings.桌面)
                {
                    try
                    {
                        VideoStreamer.Stop();//.Net Core时该方法异常，统一都加一个异常捕获
                    }
                    catch { }
                    VideoWriter.Close();
                }
                if (SettingHelp.Settings.声音)
                {
                    AudioStreamer.StopRecording();
                    AudioStreamer.Dispose();
                    AudioWriter.Close();
                }
                HiddenTools(SettingHelp.Settings.自动隐藏);
                btParse.Visibility = Visibility.Collapsed;//暂停按钮隐藏
                btStop.Visibility = Visibility.Collapsed;//停止按钮隐藏
                btSet.Visibility = Visibility.Visible;//恢复设置按钮显示
                lbTime.Visibility = Visibility.Collapsed;//视频时长隐藏
                btClose.Visibility = Visibility.Collapsed;//关闭按钮隐藏
                btBegin.Visibility = Visibility.Visible;//录制按钮显示
                if (SaveFile)//保留输出文件
                {
                    waitBar.Value = 0;
                    barGrid.Visibility = Visibility.Visible;
                    //有视频有声音的时候再进行ffmpeg合成
                    if ((SettingHelp.Settings.桌面 || SettingHelp.Settings.摄像头) && SettingHelp.Settings.声音)
                    {
                        System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                try
                                {
                                    Dispatcher.Invoke(() => { waitBar.Value = i; });
                                }
                                catch
                                {
                                    break;
                                }
                                Thread.Sleep(1000);
                            }
                        });//起一个线程让进度条动起来
                        System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            //CurrentVideoPath为全局的，防止在转码的过程中又开始了新的录制使CurrentVideoPath导致转码完删错文件
                            string tempVideo = CurrentVideoPath;
                            string tempAudio = SettingHelp.Settings.声音 ? $"-i \"{CurrentAudioPath}\"" : "";
                            string outfile = MakeFilePath(SettingHelp.Settings.编码类型);
                            Functions.CMD($"ffmpeg -i {tempVideo} {tempAudio} -acodec copy {outfile} -crf 12");
                            DeleteFile(tempVideo, tempAudio, !SettingHelp.Settings.保留视频, !SettingHelp.Settings.保留音频);
                            Dispatcher.Invoke(() =>
                            {
                                btClose.Visibility = Visibility.Visible;//转码完恢复关闭按钮显示
                                barGrid.Visibility = Visibility.Collapsed;//隐藏转码进度条
                            });
                        });
                    }
                    else
                    {
                        btClose.Visibility = Visibility.Visible;//转码完恢复关闭按钮显示
                        barGrid.Visibility = Visibility.Collapsed;//隐藏转码进度条
                    }
                }
                else//不保留输出则简单的将录制的原始音视频文件删除即算完成
                {
                    DeleteFile(CurrentVideoPath, CurrentAudioPath, !SettingHelp.Settings.保留视频, !SettingHelp.Settings.保留音频);
                }
            }
            catch (Exception ex)
            {
                if (ShowErr) Functions.Message(ex.Message);
            }
        }
        /// <summary>
        /// 600秒内每1秒钟尝试一次删除指定原始音视频文件，直至全部删除
        /// （防止因占用等原因导致没有一次删除成功）
        /// </summary>
        private void DeleteFile(string tempVideo, string tempAudio, bool DelVideo = true, bool DelAudio = true)
        {
            for (int i = 0; i < 600; i++)
            {
                try
                {
                    if (File.Exists(tempVideo) && DelVideo) File.Delete(tempVideo);
                    if (File.Exists(tempAudio) && DelAudio) File.Delete(tempAudio);
                    break;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }

        #region UI事件
        /// <summary>
        /// 开始录制点击事件
        /// </summary>
        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelp.Settings.桌面 && !SettingHelp.Settings.摄像头 && !SettingHelp.Settings.声音)
            {
                Functions.Message("未选择任何录制源，请先选择录制内容");
                return;
            }
            try
            {
                if (IsRecording)//如果已经是录制中则说明是暂停后恢复录制
                {
                    VideoStreamer.Start();
                    parseSpan = parseSpan.Add(DateTime.Now - parseTime);
                    IsParsing = false;
                }
                else//否则为首次开启录制，调用开始录制方法
                    BeginRecord();
                btBegin.Visibility = Visibility.Collapsed;
                btParse.Visibility = Visibility.Visible;
                btClose.Visibility = Visibility.Collapsed;
                btStop.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StopRecord(ShowErr: false);
                Functions.Message(ex.Message);
            }
        }
        /// <summary>
        /// 暂停按钮点击事件
        /// </summary>
        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            IsParsing = true;
            parseTime = DateTime.Now;//记录下暂停时间点
            VideoStreamer.SignalToStop();//停止捕获桌面
        }
        /// <summary>
        /// 停止录制按钮点击事件：保存输出文件
        /// </summary>
        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            StopRecord();
        }
        #endregion

        #endregion

        #region 设置
        /// <summary>
        /// 设置按钮点击事件：显隐设置Popup
        /// </summary>
        private void btSet_Click(object sender, RoutedEventArgs e)
        {
            TitlePanel.Height = SettingPop.IsOpen && SettingHelp.Settings.自动隐藏 ? 3 : 40;
            SettingPop.IsOpen = (bool)btSet.IsChecked;
            btBegin.Visibility = SettingPop.IsOpen ? Visibility.Hidden : Visibility.Visible;//设置时不允许录制，防止录制启动参数和设置的有出入
            TitleDragMove(!SettingPop.IsOpen);//根据设置Popup决定是否可以拖住，当正在设置时不允许拖拽（会和设置小窗窗窗分离）
        }
        /// <summary>
        /// 是否录制声音CheckBox
        /// </summary>
        private void cbSY_Click(object sender, RoutedEventArgs e)
        {
            SettingHelp.Settings.声音 = cbSY.IsChecked.Value;
        }

        /// <summary>
        /// 是否录制摄像头CheckBox
        /// </summary>
        private void cbSXT_Click(object sender, RoutedEventArgs e)
        {
            SettingHelp.Settings.摄像头 = cbSXT.IsChecked.Value;
        }

        /// <summary>
        /// 是否录制桌面CheckBox
        /// </summary>
        private void cbZM_Click(object sender, RoutedEventArgs e)
        {
            SettingHelp.Settings.桌面 = cbZM.IsChecked.Value;
        }

        /// <summary>
        /// 更多设置点击事件
        /// </summary>
        private void btMoreSet_Click(object sender, RoutedEventArgs e)
        {
            SetHotKey(false);//开始设置前先把当前热键卸载
            new SettingWindow().ShowDialog();//模态显示设置窗口
            SetHotKey(true);//重新加载（可能）新的热键设置
        }
        #endregion

        #region 画笔
        private DrawerWindow DrawerWin;
        private void btPen_Click(object sender, RoutedEventArgs e)
        {
            OpenDraweWin();
        }

        private void OpenDraweWin()
        {
            if ((bool)btPen.IsChecked && DrawerWin == null)
            {
                DrawerWin = new DrawerWindow
                {
                    Owner = this
                };
                DrawerWin.Show();
                btPen.IsChecked = true;
            }
            else
            {
                DrawerWin?.Close();
                DrawerWin = null;
                Dispatcher.Invoke(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });
                btPen.IsChecked = false;
            }
        }
        #endregion

        #region 热键
        private HwndSource hWndSource;
        /// <summary>
        /// 播放快捷键
        /// </summary>
        private int HotKeyBF;
        /// <summary>
        /// 停止快捷键
        /// </summary>
        private int HotKeyTZ;
        /// <summary>
        /// 屏幕画笔快捷键
        /// </summary>
        private int HotKeyHB;
        private void SetHotKey(bool Add)
        {
            if (Add)
            {
                hWndSource.AddHook(MainWindowProc);
                HotKeyBF = HotKey.GlobalAddAtom($"{SettingHelp.Settings.播放暂停.Item1}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2)}");
                HotKeyTZ = HotKey.GlobalAddAtom($"{SettingHelp.Settings.停止关闭.Item1}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2)}");
                HotKeyHB = HotKey.GlobalAddAtom($"{SettingHelp.Settings.开关画笔.Item1}-{Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.开关画笔.Item2)}");

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
                                        BtClose_Click(null, null);
                                }
                            }
                            if (sid == HotKeyHB)
                            {
                                btPen.IsChecked = !btPen.IsChecked;
                                OpenDraweWin();
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
