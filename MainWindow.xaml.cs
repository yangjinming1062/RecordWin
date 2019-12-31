using ESBasic;
using Oraycn.MCapture;
using Oraycn.MFile;
using RecordWin.Properties;
using System;
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
        #region 变量
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
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            cbHidden.IsChecked = Settings.Default.自动隐藏;
            IsHiddenTitle();
        }

        #region 私有方法
        /// <summary>
        /// 根据所处屏幕将窗口尺寸改为当前屏幕大小，并将工具栏位置置顶居中
        /// </summary>
        private void ChangePlace()
        {
            var handle = new WindowInteropHelper(this).Handle;
            var S = Screen.FromHandle(handle);
            Width = S.Bounds.Width;
            Height = S.Bounds.Height;
            Left = S.Bounds.Left;
            Top = S.Bounds.Top;
            Topmost = true;
            Canvas.SetLeft(Title, (ActualWidth - Title.Width) / 2);
        }

        private string MakeFilePath(string Type)
        {
            string path = $"{DateTime.Now.ToString("yyMMdd_HHmmss")}{Type}";
            path = System.IO.Path.Combine("tmp", path);
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            return path;
        }

        private void Message(string msg)
        {
            System.Windows.MessageBox.Show(msg);
        }

        private void IsHiddenTitle() => Title.Height = cbHidden.IsChecked.Value ? 3 : 40;//通过修改高度使动画效果出现与否来实现

        void ImageCaptured(Bitmap bm)
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

        void capturer_CaptureError(Exception ex)
        {

        }

        void audioMixter_AudioMixed(byte[] audioData)
        {
            if (this.isRecording && !this.isParsing)
            {
                if (Settings.Default.录制类型 == 2)
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

        private void BeginRecord()
        {
            int audioSampleRate = 16000;
            int channelCount = 1;

            System.Drawing.Size videoSize = Screen.PrimaryScreen.Bounds.Size;

            #region 设置采集器
            if (Settings.Default.录制类型 == 0)//桌面采集器
            {
                //如果需要录制鼠标的操作，第二个参数请设置为true
                this.desktopCapturer = CapturerFactory.CreateDesktopCapturer(frameRate, true);
                this.desktopCapturer.ImageCaptured += ImageCaptured;
                videoSize = this.desktopCapturer.VideoSize;
            }
            else if (Settings.Default.录制类型 == 1)//摄像头采集器
            {
                //videoSize = new System.Drawing.Size(videoSize.Width, videoSize.Height);默认给摄像头使用桌面的分辨率，虽然没什么关系，但是我就这样了
                this.cameraCapturer = CapturerFactory.CreateCameraCapturer(0, videoSize, frameRate);
                this.cameraCapturer.ImageCaptured += new CbGeneric<Bitmap>(ImageCaptured);
            }

            if (Settings.Default.麦克风)//麦克风采集器
            {
                this.microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(0);
                this.microphoneCapturer.CaptureError += capturer_CaptureError;
            }

            if (Settings.Default.声卡)//声卡采集器 【目前声卡采集仅支持vista以及以上系统】
            {
                this.soundcardCapturer = CapturerFactory.CreateSoundcardCapturer();
                this.soundcardCapturer.CaptureError += capturer_CaptureError;
                audioSampleRate = this.soundcardCapturer.SampleRate;
                channelCount = this.soundcardCapturer.ChannelCount;
            }

            if (Settings.Default.麦克风 && Settings.Default.声卡)//混音器
            {
                this.audioMixter = CapturerFactory.CreateAudioMixter(this.microphoneCapturer, this.soundcardCapturer, SoundcardMode4Mix.DoubleChannel, true);
                this.audioMixter.AudioMixed += audioMixter_AudioMixed; //如果是混音,则不再需要预订microphoneCapturer和soundcardCapturer的AudioCaptured事件
                audioSampleRate = this.audioMixter.SampleRate;
                channelCount = this.audioMixter.ChannelCount;
            }
            else if (Settings.Default.麦克风)
            {
                this.microphoneCapturer.AudioCaptured += audioMixter_AudioMixed;
            }
            else if (Settings.Default.声卡)
            {
                this.soundcardCapturer.AudioCaptured += audioMixter_AudioMixed;
            }
            #endregion

            #region 开始采集
            if (Settings.Default.麦克风)
            {
                try
                {
                    this.microphoneCapturer.Start();
                }
                catch { Message("开启麦克风失败，请确认有麦克风并且驱动正常"); return; }
            }
            if (Settings.Default.声卡)
            {
                this.soundcardCapturer.Start();
            }

            if (Settings.Default.录制类型 == 1)
            {
                try
                {
                    this.cameraCapturer.Start();
                }
                catch { Message("开启摄像头失败，请确认有摄像头并且驱动正常"); return; }
            }
            else if (Settings.Default.录制类型 == 0)
            {
                this.desktopCapturer.Start();
            }
            #endregion

            #region 录制组件
            if (Settings.Default.录制类型 == 2) //仅仅录制声音
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

                if (Settings.Default.麦克风 == false && Settings.Default.声卡 == false) //仅仅录制图像
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
            cbHidden.IsChecked = true;
            IsHiddenTitle();
            btBegin.Content = "停止";
            btSet.IsEnabled = false;
        }

        private void StopRecord()
        {
            if (Settings.Default.麦克风) // 麦克风
            {
                this.microphoneCapturer.Stop();
            }
            if (Settings.Default.声卡) //声卡
            {
                this.soundcardCapturer.Stop();
            }
            if (Settings.Default.录制类型 == 1)
            {
                this.cameraCapturer.Stop();
            }
            if (Settings.Default.录制类型 == 0)
            {
                this.desktopCapturer.Stop();
            }
            if (Settings.Default.录制类型 == 2)
            {
                this.audioFileMaker.Close(true);
            }
            else
            {
                if (!this.justRecordVideo)
                {
                    this.videoFileMaker.Close(true);
                }
                else
                {
                    this.silenceVideoFileMaker.Close(true);
                }
            }
            this.isRecording = false;
            cbHidden.IsChecked = Settings.Default.自动隐藏;
            IsHiddenTitle();
            btBegin.Content = "开始";
            btSet.IsEnabled = true;
        }
        #endregion

        #region 事件
        private void Window_Loaded(object sender, RoutedEventArgs e) => ChangePlace();

        private void btPen_Click(object sender, RoutedEventArgs e)
        {
            Title.Height = 40;//为了不让动画将工具栏收回这里手动修改高度
            ColorPop.IsOpen = true;
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ColorPop.IsOpen = false;
            Title.Height = 3;//选择颜色后再将工具栏高度改回去
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            if (btBegin.Content.ToString() == "停止")
                btBegin_Click(null, null);
            Close();
        }
        /// <summary>
        /// 拖动到其他屏幕，录制其他屏幕
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
        /// 录制与停止录制
        /// </summary>
        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btBegin.Content.ToString() == "开始")
                {
                    BeginRecord();
                }
                else
                {
                    StopRecord();
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }
        /// <summary>
        /// 打开设置窗口
        /// </summary>
        private void btSet_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow setting = new SettingWindow();
            setting.Owner = this;
            setting.ShowDialog();
        }
        private void cbHidden_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.自动隐藏 = cbHidden.IsChecked.Value;
            IsHiddenTitle();
        }
        #endregion
    }
}
