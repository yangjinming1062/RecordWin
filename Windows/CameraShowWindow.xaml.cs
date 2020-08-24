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
    public partial class CameraShowWindow : Window
    {
        #region 变量
        private VideoCaptureDevice Camera;//用来操作摄像头
        private VideoFileWriter VideoOutPut;//用来把每一帧图像编码到视频文件
        /// <summary>
        /// 录制摄像头时输出的视频文件路径
        /// </summary>
        private string FileName;
        private bool isParsing;
        private DateTime beginTime;
        private TimeSpan parseSpan;
        private DateTime parseTime; 
        #endregion

        /// <summary>
        /// 构造函数，完成构造后自己调用Show显示
        /// </summary>
        /// <param name="fileName">输出文件路径</param>
        /// <param name="Screen">所属屏幕</param>
        public CameraShowWindow(string fileName, System.Windows.Forms.Screen Screen)
        {
            InitializeComponent();
            FileName = fileName;
            BeginRecord();
            if (SettingHelp.Settings.桌面)//同时录制桌面时摄像头作为一部分显示在桌面上
            {
                double targerWidth = Screen.WorkingArea.Width * 0.2;//目标显示宽度：屏幕宽度的20%
                double targetHeight = Screen.WorkingArea.Height * 0.2;//同上，高度也20%
                if (Math.Abs(targerWidth - Camera.VideoResolution.FrameSize.Width) <= Math.Abs(targetHeight - Camera.VideoResolution.FrameSize.Height))
                {
                    targetHeight = Camera.VideoResolution.FrameSize.Height * targerWidth / Camera.VideoResolution.FrameSize.Width;
                }
                else//找到摄像头配置中最接近目标宽高的是宽度还是高度，等比缩放
                {
                    targerWidth = Camera.VideoResolution.FrameSize.Width * targetHeight / Camera.VideoResolution.FrameSize.Height;
                }
                imgCamera.Width = targerWidth;
                imgCamera.Height = targetHeight;
                Width = targerWidth;
                Height = targetHeight + 30;//因为头部的存在，所以需要多30
                Left = Screen.WorkingArea.Width - Width - 10;
                Top = Screen.WorkingArea.Height - Height - 10;
            }
            else
            {
                imgCamera.Width = Camera.VideoResolution.FrameSize.Width;
                imgCamera.Height = Camera.VideoResolution.FrameSize.Height;
                Width = imgCamera.Width;
                Height = imgCamera.Height + 30;
            }
        }

        #region 私有方法
        /// <summary>
        /// 录制函数：当只录摄像头时会将输出记入文件，由该函数启动窗口的Show方法
        /// </summary>
        private void BeginRecord()
        {
            if (!string.IsNullOrEmpty(SettingHelp.Settings.摄像头Key) && SettingHelp.Settings.摄像头参数 > -1)//实例化设备控制类
            {
                Camera = new VideoCaptureDevice(SettingHelp.Settings.摄像头Key);
                //配置录像参数(宽,高,帧率,比特率等参数)VideoCapabilities这个属性会返回摄像头支持哪些配置
                Camera.VideoResolution = Camera.VideoCapabilities[SettingHelp.Settings.摄像头参数];
                Camera.NewFrame += NewFrameHandle;//设置回调处理函数,aforge会不断从这个回调推出图像数据,SnapshotFrame也是有待比较
                Camera.Start();//打开摄像头
                parseSpan = new TimeSpan();
                if (!SettingHelp.Settings.桌面)//开启摄像头又不录制桌面说明是要录制摄像头的视频
                {
                    lock (this) //打开录像文件(如果没有则创建,如果有也会清空),这里还有关于
                    {
                        VideoOutPut = new VideoFileWriter();
                        beginTime = new DateTime();
                        VideoOutPut.Open(FileName, Camera.VideoResolution.FrameSize.Width, Camera.VideoResolution.FrameSize.Height,
                           Camera.VideoResolution.AverageFrameRate, VideoCodec.MPEG4,
                           Camera.VideoResolution.FrameSize.Width * Camera.VideoResolution.FrameSize.Height * SettingHelp.Settings.视频质量);
                    }
                }
                Show();
            }
            else//未找到摄像头设置则关闭窗口
            {
                Close();
            }
        }
        /// <summary>
        /// 摄像头输出处理函数
        /// </summary>
        private void NewFrameHandle(object sender, NewFrameEventArgs eventArgs)
        {
            if (!isParsing)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    using MemoryStream ms = new MemoryStream();
                    eventArgs.Frame.Save(ms, ImageFormat.Bmp);
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(ms.GetBuffer());
                    image.EndInit();
                    imgCamera.Source = image;
                }));//同步显示
                if (!SettingHelp.Settings.桌面)
                {
                    try
                    {
                        VideoOutPut.WriteVideoFrame(eventArgs.Frame, DateTime.Now - beginTime - parseSpan);
                    }
                    catch { }
                }
            }
        }
        #endregion

        #region UI事件

        #region 窗体大小拖拽
        private void ResizeRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Rectangle rectangle)
            {
                switch (rectangle.Name)
                {
                    case "rTop": Cursor = Cursors.SizeNS; break;
                    case "rBottom": Cursor = Cursors.SizeNS; break;
                    case "rLeft": Cursor = Cursors.SizeWE; break;
                    case "rRight": Cursor = Cursors.SizeWE; break;
                    case "rTopLeft": Cursor = Cursors.SizeNWSE; break;
                    case "rTopRight": Cursor = Cursors.SizeNESW; break;
                    case "rBottomLeft": Cursor = Cursors.SizeNESW; break;
                    case "rBottomRight": Cursor = Cursors.SizeNWSE; break;
                    default: break;
                }
            }
        }
        private void ResizeRectangle_MouseLeave(object sender, MouseEventArgs e) => Cursor = Cursors.Arrow;
        private void ResizeRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rectangle)
            {
                switch (rectangle.Name)
                {
                    case "rTop":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Top);
                        break;
                    case "rBottom":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Bottom);
                        break;
                    case "rLeft":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Left);
                        break;
                    case "rRight":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Right);
                        break;
                    case "rTopLeft":
                        Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.TopLeft);
                        break;
                    case "rTopRight":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.TopRight);
                        break;
                    case "rBottomLeft":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.BottomLeft);
                        break;
                    case "rBottomRight":
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

        private void ResizeWindow(ResizeDirection direction) => SendMessage(((HwndSource)PresentationSource.FromVisual(this)).Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);

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
        /// 关闭按钮点击事件
        /// </summary>
        private void btClose_Click(object sender, RoutedEventArgs e) => Close();
        /// <summary>
        /// 恢复录制点击事件
        /// </summary>
        private void btBegin_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Collapsed;
            btParse.Visibility = Visibility.Visible;
            parseSpan = parseSpan.Add(DateTime.Now - parseTime);
            isParsing = false;
        }
        /// <summary>
        /// 暂停按钮点击事件
        /// </summary>
        private void btParse_Click(object sender, RoutedEventArgs e)
        {
            btBegin.Visibility = Visibility.Visible;
            btParse.Visibility = Visibility.Collapsed;
            parseTime = DateTime.Now;
            isParsing = true;
        }
        /// <summary>
        /// 重载Closing事件
        /// </summary>
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
                        Camera.SignalToStop();//用其他的stop会长时间卡住，必须用这个
                        Camera = null;
                    });
                    //停摄像头
                    if (!SettingHelp.Settings.桌面)//这里只负责关闭文件即可，转码统一由MainWindow的StopRecord处理
                    {
                        VideoOutPut.Close();//关闭录像文件,如果忘了不关闭,将会得到一个损坏的文件,无法播放
                        VideoOutPut.Dispose();
                    }
                    if (Owner is MainWindow main)
                    {
                        if (main.Visibility != Visibility.Visible)//只录摄像头时，关闭摄像头录制回调主窗体的停录函数，进行音视频合成
                        {
                            main.Visibility = Visibility.Visible;
                            main.StopRecord(CloseCamera: false);
                        }
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// 窗口大小变化事件
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            imgCamera.Width = ActualWidth;
            imgCamera.Height = ActualHeight - 30;
        }
        /// <summary>
        /// 拖动移动
        /// </summary>
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount > 1)//双击切换最大化状态
                    WindowState = WindowState == WindowState.Maximized ? WindowState = WindowState.Normal : WindowState = WindowState.Maximized;
                else if (e.LeftButton == MouseButtonState.Pressed && WindowState == WindowState.Normal)//非最大化时拖拽移动
                    DragMove();
            }
        } 
        #endregion
    }
}
