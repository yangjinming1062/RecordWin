using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RecordWin
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        private int frameRate = 21; // 采集视频的帧频
        public SettingWindow()
        {
            InitializeComponent();
            txtZL.Text = SettingHelp.Settings.视频帧率.ToString();
            txtZL.TextChanged += txtZL_TextChanged;
            cbPlayHidden.IsChecked = SettingHelp.Settings.播放隐藏;
            cbBF.Text = SettingHelp.Settings.播放暂停.Item1.ToString();
            cbTZ.Text = SettingHelp.Settings.停止关闭.Item1.ToString();
            cbBF.SelectionChanged += cbBF_SelectionChanged;//必须放在Text赋值后再加载事件
            cbTZ.SelectionChanged += cbTZ_SelectionChanged;
            txtBF.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2);
            txtTZ.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2);
        }

        /// <summary>
        /// 统一消息提醒
        /// </summary>
        private void Message(string msg)
        {
            System.Windows.MessageBox.Show(msg);
        }

        private Tuple<HotKey.KeyModifiers, int> GetKeysFormString(string a, string b)
        {
            HotKey.KeyModifiers A = HotKey.KeyModifiers.None;
            int B = 0;
            try
            {
                A = (HotKey.KeyModifiers)Enum.Parse(typeof(HotKey.KeyModifiers), a);
                B = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), b);
            }
            catch {  }
            return new Tuple<HotKey.KeyModifiers, int>(A, B);
        }

        private void txtZL_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtZL.Text, out frameRate))
            {
                if (frameRate > 30 || frameRate < 0)
                    Message("请输入(0，30]之间正整数！");
                else
                    SettingHelp.Settings.视频帧率 = frameRate;
            }
            else
            {
                Message("请输入(0，30]之间正整数！");
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        private void cbPlayHidden_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.播放隐藏 = cbPlayHidden.IsChecked.Value;

        private void cbBF_SelectionChanged(object sender, SelectionChangedEventArgs e) => SettingHelp.Settings.播放暂停 = GetKeysFormString(cbBF.Text, txtBF.Text);

        private void cbTZ_SelectionChanged(object sender, SelectionChangedEventArgs e) => SettingHelp.Settings.停止关闭 = GetKeysFormString(cbTZ.Text, txtTZ.Text);

        private void txtBF_KeyDown(object sender, KeyEventArgs e)
        {
            int v = (int)System.Windows.Forms.Keys.None;
            try
            {
                v = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), e.Key.ToString());
            }
            catch { }
            txtBF.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), v);
            SettingHelp.Settings.播放暂停 = GetKeysFormString(cbBF.Text, txtBF.Text);
            e.Handled = true;//到此为止，不加这个Text处会显示重复的字母等混乱情况
        }

        private void txtTZ_KeyDown(object sender, KeyEventArgs e)
        {
            int v = (int)System.Windows.Forms.Keys.None;
            try
            {
                v = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), e.Key.ToString());
            }
            catch { }
            txtTZ.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), v);
            SettingHelp.Settings.停止关闭 = GetKeysFormString(cbTZ.Text, txtTZ.Text);
            e.Handled = true;//到此为止，不加这个Text处会显示重复的字母等混乱情况
        }
    }
}
