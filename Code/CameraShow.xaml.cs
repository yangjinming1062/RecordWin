using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RecordWin
{
    /// <summary>
    /// CameraShow.xaml 的交互逻辑
    /// </summary>
    public partial class CameraShow : Window
    {
        private VideoCaptureDevice Camera;//用来操作摄像头
        private VideoFileWriter VideoOutPut;//用来把每一帧图像编码到视频文件
        private string FileName;
        private bool isParsing;
        public CameraShow(string fileName)
        {
            InitializeComponent();
            FileName = fileName;
            BeginRecord();
        }

        private void BeginRecord()
        {
            var devs = new FilterInfoCollection(FilterCategory.VideoInputDevice);//获取摄像头列表 
            if (devs.Count != 0)
            {
                Camera = new VideoCaptureDevice(devs[0].MonikerString);//实例化设备控制类(我选了第1个)
                                                                       //配置录像参数(宽,高,帧率,比特率等参数)VideoCapabilities这个属性会返回摄像头支持哪些配置,从这里面选一个赋值接即可
                Camera.VideoResolution = Camera.VideoCapabilities[Camera.VideoCapabilities.Length - 1];
                Camera.NewFrame += Camera_NewFrame;//设置回调,aforge会不断从这个回调推出图像数据
                Camera.Start();//打开摄像头

                if (!string.IsNullOrEmpty(FileName))
                {
                    lock (this) //打开录像文件(如果没有则创建,如果有也会清空),这里还有关于
                    {
                        VideoOutPut = new VideoFileWriter();
                        VideoOutPut.Open(FileName, Camera.VideoResolution.FrameSize.Width, Camera.VideoResolution.FrameSize.Height,
                           Camera.VideoResolution.AverageFrameRate, VideoCodec.MPEG4, Camera.VideoResolution.BitCount);
                    }
                }
                Show();
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// 摄像头输出回调
        /// </summary>
        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!this.isParsing)
            {
                if (!string.IsNullOrEmpty(FileName))
                    VideoOutPut.WriteVideoFrame(eventArgs.Frame);//写到文件
                lock (this)
                {
                    MemoryStream ms = new MemoryStream();
                    eventArgs.Frame.Save(ms, ImageFormat.Bmp);
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(ms.GetBuffer());
                    ms.Close();
                    image.EndInit();
                    imgCamera.Source = image;
                }
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Collapsed;
            btParse.Visibility = Visibility.Visible;
            this.isParsing = false;
        }

        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            this.isParsing = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                if (Camera != null)
                {
                    Camera.Stop();//停摄像头
                    if (!string.IsNullOrEmpty(FileName))
                    {
                        VideoOutPut.Close();//关闭录像文件,如果忘了不关闭,将会得到一个损坏的文件,无法播放
                    }
                    if (this.Owner is MainWindow)
                    {
                        var main = this.Owner as MainWindow;
                        if (main.Visibility != Visibility.Visible)//只录摄像头时，关闭摄像头录制回调主窗体的停录函数，进行音视频合成
                        {
                            main.Visibility = Visibility.Visible;
                            main.StopRecord();
                        }
                    }
                }
            }
            catch { }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                var S = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                Width = S.WorkingArea.Width / 5;
                Height = S.WorkingArea.Height / 5;
                Left = S.WorkingArea.Width - Width - 10;
                Top = S.WorkingArea.Height - Height - 10;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
            BeginRecord();
        }
    }
}
