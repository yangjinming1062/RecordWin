using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            ShowShell();
            BeginRecord();
        }

        private void BeginRecord()
        {
            if (!string.IsNullOrEmpty(SettingHelp.Settings.摄像头Key) && SettingHelp.Settings.摄像头参数 > -1)//实例化设备控制类
            {
                Camera = new VideoCaptureDevice(SettingHelp.Settings.摄像头Key);
                //配置录像参数(宽,高,帧率,比特率等参数)VideoCapabilities这个属性会返回摄像头支持哪些配置
                Camera.VideoResolution = Camera.VideoCapabilities[SettingHelp.Settings.摄像头参数];
                imgCamera.Width = Camera.VideoResolution.FrameSize.Width;
                imgCamera.Height = Camera.VideoResolution.FrameSize.Height;
                Camera.NewFrame += Camera_NewFrame;//设置回调,aforge会不断从这个回调推出图像数据
                Camera.Start();//打开摄像头

                if (!SettingHelp.Settings.桌面)
                {
                    lock (this) //打开录像文件(如果没有则创建,如果有也会清空),这里还有关于
                    {
                        VideoOutPut = new VideoFileWriter();
                        VideoOutPut.Open(FileName, Camera.VideoResolution.FrameSize.Width, Camera.VideoResolution.FrameSize.Height,
                           Camera.VideoResolution.AverageFrameRate, (VideoCodec)Enum.Parse(typeof(VideoCodec), SettingHelp.Settings.编码类型),
                           Camera.VideoResolution.FrameSize.Width * Camera.VideoResolution.FrameSize.Height * SettingHelp.Settings.视频质量);
                    }
                }
                Show();
            }
            else
            {
                Close();
            }
        }

        #region 窗体大小拖拽
        private void ShowShell()
        {
            //foreach (UIElement element in ResizeGrid.Children)
            //{
            //    if (element is Rectangle)
            //    {
            //        var resizeRectangle = element as Rectangle;
            //        if (resizeRectangle != null)
            //        {
            //            resizeRectangle.Visibility = Visibility.Visible;
            //            resizeRectangle.PreviewMouseDown += ResizeRectangle_PreviewMouseDown;
            //            resizeRectangle.MouseEnter += ResizeRectangle_MouseEnter;
            //            resizeRectangle.MouseLeave += ResizeRectangle_MouseLeave;
            //        }
            //    }
            //}
        }

        private void ResizeRectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// 按下拖动区域发生事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rectangle = sender as Rectangle;
            if (rectangle != null)
            {
                switch (rectangle.Name)
                {
                    case "Top":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Top);
                        break;
                    case "Bottom":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Bottom);
                        break;
                    case "Left":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Left);
                        break;
                    case "Right":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Right);
                        break;
                    case "TopLeft":
                        Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.TopLeft);
                        break;
                    case "TopRight":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.TopRight);
                        break;
                    case "BottomLeft":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.BottomLeft);
                        break;
                    case "BottomRight":
                        Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.BottomRight);
                        break;
                    default:
                        break;
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// 重绘Window
        /// </summary>
        /// <param name="direction"></param>
        private void ResizeWindow(ResizeDirection direction)
        {
            SendMessage(((HwndSource)PresentationSource.FromVisual(this)).Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }
        /// <summary>
        /// 拖动区域内移动前事件
        /// </summary>
        private void ResizeRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle rectangle = sender as Rectangle;
            if (rectangle != null)
            {
                switch (rectangle.Name)
                {
                    case "Top":
                        Cursor = Cursors.SizeNS;
                        break;
                    case "Bottom":
                        Cursor = Cursors.SizeNS;
                        break;
                    case "Left":
                        Cursor = Cursors.SizeWE;
                        break;
                    case "Right":
                        Cursor = Cursors.SizeWE;
                        break;
                    case "TopLeft":
                        Cursor = Cursors.SizeNWSE;
                        break;
                    case "TopRight":
                        Cursor = Cursors.SizeNESW;
                        break;
                    case "BottomLeft":
                        Cursor = Cursors.SizeNESW;
                        break;
                    case "BottomRight":
                        Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        break;
                }
            }
        }
        public enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }
        #endregion
        /// <summary>
        /// 摄像头输出回调
        /// </summary>
        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!isParsing)
            {
                //var bmp = eventArgs.Frame.Clone() as System.Drawing.Bitmap;
                Dispatcher.Invoke(new Action(() =>
                {
                    MemoryStream ms = new MemoryStream();
                    eventArgs.Frame.Save(ms, ImageFormat.Bmp);
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(ms.GetBuffer());
                    ms.Close();
                    image.EndInit();
                    imgCamera.Source = image;
                }));
                if (!SettingHelp.Settings.桌面)
                    VideoOutPut.WriteVideoFrame(eventArgs.Frame);
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Collapsed;
            btParse.Visibility = Visibility.Visible;
            isParsing = false;
        }

        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            isParsing = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                if (Camera != null)
                {
                    isParsing = true;
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        Camera.SignalToStop();
                        Camera = null;
                    });
                    //停摄像头
                    if (!SettingHelp.Settings.桌面)
                    {
                        VideoOutPut.Close();//关闭录像文件,如果忘了不关闭,将会得到一个损坏的文件,无法播放
                    }
                    if (Owner is MainWindow)
                    {
                        var main = Owner as MainWindow;
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
            if (SettingHelp.Settings.桌面)//同时录制桌面时摄像头作为一部分显示在桌面上
            {
                var S = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                if (Camera != null)
                {
                    Width = Camera.VideoResolution.FrameSize.Width / 4;
                    Height = Camera.VideoResolution.FrameSize.Height / 4;
                }
                else
                {
                    Width = S.WorkingArea.Width / 5;
                    Height = S.WorkingArea.Height / 5;
                }
                Left = S.WorkingArea.Width - Width - 10;
                Top = S.WorkingArea.Height - Height - 10;
            }
        }
    }
}
