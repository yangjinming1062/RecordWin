using ESBasic;
using Oraycn.MCapture;
using Oraycn.MFile;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace RecordWin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            HiddenTools(SettingHelp.Settings.自动隐藏);
            switch (SettingHelp.Settings.录制类型)
            {
                case 0: rbZM.IsChecked = true; break;
                case 1: rbSXT.IsChecked = true; break;
                case 2: rbSY.IsChecked = true; break;
            }
            cbSK.IsChecked = SettingHelp.Settings.声卡;
            cbMK.IsChecked = SettingHelp.Settings.麦克风;

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
            this.timer = new Timer();
            this.timer.Interval = 1000;
            this.timer.Tick += timer_Tick;
        }

        #region 私有方法
        /// <summary>
        /// 根据所处屏幕将窗口尺寸改为当前屏幕大小，并将工具栏位置置顶居中
        /// </summary>
        private void ChangePlace()
        {
            var handle = new WindowInteropHelper(this).Handle;
            var S = Screen.FromHandle(handle);
            Left = S.Bounds.Left + (S.Bounds.Width - TitleGrid.ActualWidth) / 2;
            Top = S.Bounds.Top;
            Topmost = true;
        }
        /// <summary>
        /// 根据时间生成保存文件名称，文件位于tmp文件夹中
        /// </summary>
        /// <param name="Type">文件后缀.mp3或者.mp4，需要带点</param>
        private string MakeFilePath(string Type)
        {
            string path = $"{DateTime.Now.ToString("yyMMdd_HHmmss")}{Type}";
            path = Path.Combine("tmp", path);
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }
        /// <summary>
        /// 统一消息提醒
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
            if (Hidden.HasValue)
                cbHidden.IsChecked = Hidden;
            TitleGrid.Height = cbHidden.IsChecked.Value && !SettingPop.IsOpen ? 3 : 40;//通过修改高度使动画效果出现与否来实现
        }

        public void TitleDragMove(bool move)
        {
            if (move && !SettingPop.IsOpen && !btPen.IsActived)
            {
                TitleGrid.MouseDown += Title_MouseDown;
                TitleGrid.Cursor = System.Windows.Input.Cursors.SizeAll;
            }
            else
            {
                TitleGrid.MouseDown -= Title_MouseDown;
                TitleGrid.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }
        #endregion

        #region 事件
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (this.isRecording)
                StopRecord();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) => ChangePlace();
        /// <summary>
        /// 关闭程序
        /// </summary>
        private void btClose_Click(object sender, RoutedEventArgs e) => Close();
        /// <summary>
        /// 拖动到其他屏幕
        /// </summary>
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                ChangePlace();
            }
        }
        /// <summary>
        /// 是否隐藏
        /// </summary>
        private void cbHidden_Click(object sender, RoutedEventArgs e)
        {
            SettingHelp.Settings.自动隐藏 = cbHidden.IsChecked.Value;
            HiddenTools();
        }
        /// <summary>
        /// 防止UC跑丢，我就死活一个状态
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e) => WindowState = WindowState.Normal;
        #endregion

        #region 录制
        private ISoundcardCapturer soundcardCapturer;
        private IMicrophoneCapturer microphoneCapturer;
        private IDesktopCapturer desktopCapturer;
        private ICameraCapturer cameraCapturer;
        private IAudioMixter audioMixter;
        private VideoFileMaker videoFileMaker;
        private SilenceVideoFileMaker silenceVideoFileMaker;
        private AudioFileMaker audioFileMaker;
        private int frameRate = 10; // 采集视频的帧频
        private bool sizeRevised = false;// 是否需要将图像帧的长宽裁剪为4的整数倍
        private volatile bool isRecording = false;
        private volatile bool isParsing = false;
        private bool justRecordVideo = false;

        private Timer timer;
        private int seconds = 0;
        private void ImageCaptured(Bitmap bm)
        {
            if (this.isRecording && !this.isParsing)
            {
                //这里可能要裁剪
                Bitmap imgRecorded = bm;
                if (this.sizeRevised) // 对图像进行裁剪，  MFile要求录制的视频帧的长和宽必须是4的整数倍。
                {
                    imgRecorded = ESBasic.Helpers.ImageHelper.RoundSizeByNumber(bm, 4);
                    bm.Dispose();
                }

                if (!this.justRecordVideo)
                {
                    this.videoFileMaker.AddVideoFrame(imgRecorded);
                }
                else
                {
                    this.silenceVideoFileMaker.AddVideoFrame(imgRecorded);
                }
            }
        }

        private void capturer_CaptureError(Exception ex)
        {

        }

        private void audioMixter_AudioMixed(byte[] audioData)
        {
            if (this.isRecording && !this.isParsing)
            {
                if (SettingHelp.Settings.录制类型 == 2)
                {
                    this.audioFileMaker.AddAudioFrame(audioData);
                }
                else
                {
                    if (!this.justRecordVideo)
                    {
                        this.videoFileMaker.AddAudioFrame(audioData);
                    }
                }
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (this.isRecording && !this.isParsing)
            {
                var ts = new TimeSpan(0, 0, ++seconds);
                this.lbTime.Content = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");
            }
        }

        private void BeginRecord()
        {
            int audioSampleRate = 16000;
            int channelCount = 1;

            IntPtr handle = new WindowInteropHelper(this).Handle;
            Screen S = Screen.FromHandle(handle);//程序当前所处屏幕
            System.Drawing.Size videoSize = S.Bounds.Size;

            #region 设置采集器
            if (SettingHelp.Settings.录制类型 == 0)//桌面采集器
            {
                //如果需要录制鼠标的操作，第二个参数请设置为true
                this.desktopCapturer = CapturerFactory.CreateDesktopCapturer(frameRate, true, S.Bounds);
                this.desktopCapturer.ImageCaptured += ImageCaptured;
                //videoSize = this.desktopCapturer.VideoSize;
            }
            else if (SettingHelp.Settings.录制类型 == 1)//摄像头采集器
            {
                //videoSize = new System.Drawing.Size(videoSize.Width, videoSize.Height);默认给摄像头使用桌面的分辨率，虽然没什么关系，但是我就这样了
                this.cameraCapturer = CapturerFactory.CreateCameraCapturer(0, videoSize, frameRate);
                this.cameraCapturer.ImageCaptured += new CbGeneric<Bitmap>(ImageCaptured);
            }

            if (SettingHelp.Settings.麦克风)//麦克风采集器
            {
                this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(0);
                this.microphoneCapturer.CaptureError += capturer_CaptureError;
            }

            if (SettingHelp.Settings.声卡)//声卡采集器 【目前声卡采集仅支持vista以及以上系统】
            {
                this.soundcardCapturer = CapturerFactory.CreateSoundcardCapturer();
                this.soundcardCapturer.CaptureError += capturer_CaptureError;
                audioSampleRate = this.soundcardCapturer.SampleRate;
                channelCount = this.soundcardCapturer.ChannelCount;
            }

            if (SettingHelp.Settings.麦克风 && SettingHelp.Settings.声卡)//混音器
            {
                this.audioMixter = CapturerFactory.CreateAudioMixter(this.microphoneCapturer, this.soundcardCapturer, SoundcardMode4Mix.DoubleChannel, true);
                this.audioMixter.AudioMixed += audioMixter_AudioMixed; //如果是混音,则不再需要预订microphoneCapturer和soundcardCapturer的AudioCaptured事件
                audioSampleRate = this.audioMixter.SampleRate;
                channelCount = this.audioMixter.ChannelCount;
            }
            else if (SettingHelp.Settings.麦克风)
            {
                this.microphoneCapturer.AudioCaptured += audioMixter_AudioMixed;
            }
            else if (SettingHelp.Settings.声卡)
            {
                this.soundcardCapturer.AudioCaptured += audioMixter_AudioMixed;
            }
            #endregion

            #region 开始采集
            if (SettingHelp.Settings.麦克风)
            {
                try
                {
                    this.microphoneCapturer.Start();
                }
                catch { Message("开启麦克风失败，请确认有麦克风并且驱动正常"); return; }
            }
            if (SettingHelp.Settings.声卡)
            {
                this.soundcardCapturer.Start();
            }

            if (SettingHelp.Settings.录制类型 == 1)
            {
                try
                {
                    this.cameraCapturer.Start();
                }
                catch { Message("开启摄像头失败，请确认有摄像头并且驱动正常"); return; }
            }
            else if (SettingHelp.Settings.录制类型 == 0)
            {
                this.desktopCapturer.Start();
            }
            #endregion

            #region 录制组件
            if (SettingHelp.Settings.录制类型 == 2) //仅仅录制声音
            {
                this.audioFileMaker = new AudioFileMaker();
                this.audioFileMaker.Initialize(MakeFilePath("mp3"), audioSampleRate, channelCount);
            }
            else
            {
                //宽和高修正为4的倍数
                this.sizeRevised = (videoSize.Width % 4 != 0) || (videoSize.Height % 4 != 0);
                if (this.sizeRevised)
                {
                    videoSize = new System.Drawing.Size(videoSize.Width / 4 * 4, videoSize.Height / 4 * 4);
                }

                if (SettingHelp.Settings.麦克风 == false && SettingHelp.Settings.声卡 == false) //仅仅录制图像
                {
                    this.justRecordVideo = true;
                    this.silenceVideoFileMaker = new SilenceVideoFileMaker();
                    this.silenceVideoFileMaker.Initialize(MakeFilePath(".mp4"), VideoCodecType.H264, videoSize.Width, videoSize.Height, frameRate, VideoQuality.Middle);
                }
                else //录制声音+图像
                {
                    this.justRecordVideo = false;
                    this.videoFileMaker = new VideoFileMaker();
                    this.videoFileMaker.Initialize(MakeFilePath(".mp4"), VideoCodecType.H264, videoSize.Width, videoSize.Height, frameRate, VideoQuality.High, AudioCodecType.AAC, audioSampleRate, channelCount, true);
                }
            }
            #endregion

            this.isRecording = true;
            this.isParsing = false;
            HiddenTools(true);
            btSet.Visibility = Visibility.Hidden;
            lbTime.Visibility = Visibility.Visible;
            TitleDragMove(false);
        }

        private void StopRecord()
        {
            try
            {
                if (SettingHelp.Settings.麦克风) // 麦克风
                    this.microphoneCapturer.Stop();
                if (SettingHelp.Settings.声卡) //声卡
                    this.soundcardCapturer.Stop();
                if (SettingHelp.Settings.录制类型 == 1)
                    this.cameraCapturer.Stop();
                if (SettingHelp.Settings.录制类型 == 0)
                    this.desktopCapturer.Stop();
                if (SettingHelp.Settings.录制类型 == 2)
                    this.audioFileMaker.Close(true);
                else
                {
                    if (!this.justRecordVideo)
                        this.videoFileMaker.Close(true);
                    else
                        this.silenceVideoFileMaker.Close(true);
                }
                this.isRecording = false;
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
                Message(ex.Message);
            }
        }

        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.isRecording)
                    this.isParsing = false;
                else
                    BeginRecord();
                this.timer.Start();
                btBegin.Visibility = Visibility.Collapsed;
                btParse.Visibility = Visibility.Visible;
                btClose.Visibility = Visibility.Collapsed;
                btStop.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StopRecord();
                Message(ex.Message);
            }
        }

        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            this.isParsing = true;
            this.timer.Stop();
        }

        private void btStop_Click(object sender, RoutedEventArgs e) => StopRecord();
        #endregion

        #region 设置
        /// <summary>
        /// 打开设置窗口
        /// </summary>
        private void btSet_Click(object sender, RoutedEventArgs e)
        {
            TitleGrid.Height = SettingPop.IsOpen && SettingHelp.Settings.自动隐藏 ? 3 : 40;
            SettingPop.IsOpen = !SettingPop.IsOpen;
            btSet.IsActived = SettingPop.IsOpen;
            btBegin.Visibility = SettingPop.IsOpen ? Visibility.Hidden : Visibility.Visible;
            TitleDragMove(!SettingPop.IsOpen);
        }

        private void rbSet_Click(object sender, RoutedEventArgs e)
        {
            if (rbSY.IsChecked.Value && !cbSK.IsChecked.Value && !cbMK.IsChecked.Value)
            {
                cbMK.IsChecked = true;
                SettingHelp.Settings.麦克风 = cbMK.IsChecked.Value;
            }

            if (rbZM.IsChecked.Value)
                SettingHelp.Settings.录制类型 = 0;
            else if (rbSXT.IsChecked.Value)
                SettingHelp.Settings.录制类型 = 1;
            else if (rbSY.IsChecked.Value)
                SettingHelp.Settings.录制类型 = 2;
        }

        private void cbSK_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.声卡 = cbSK.IsChecked.Value;

        private void cbMK_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.麦克风 = cbMK.IsChecked.Value;
        #endregion

        #region 画笔
        private void btPen_Click(object sender, RoutedEventArgs e)
        {
            if (!btPen.IsActived)
            {
                DrawerWindow win = new DrawerWindow();
                btPen.IsActived = true;
                win.Owner = this;
                win.Show();
                TitleDragMove(false);
            }
        }
        #endregion
    }
}
