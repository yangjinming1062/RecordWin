using System;
using System.Windows;
using System.Windows.Controls;
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
            cbPlayHidden.IsChecked = SettingHelp.Settings.播放隐藏;
            cbBF.Text = SettingHelp.Settings.播放暂停.Item1.ToString();
            cbTZ.Text = SettingHelp.Settings.停止关闭.Item1.ToString();
            cbBF.SelectionChanged += cbBF_SelectionChanged;//必须放在Text赋值后再加载事件
            cbTZ.SelectionChanged += cbTZ_SelectionChanged;
            txtBF.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2);
            txtTZ.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2);
            slZHiLiang.Value = SettingHelp.Settings.视频质量;
            slZHiLiang.ValueChanged += SlZL_ValueChanged;
            slZhenLv.Value = SettingHelp.Settings.视频帧率;
            slZhenLv.ValueChanged += SlZhenLv_ValueChanged;
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

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        private void cbPlayHidden_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.播放隐藏 = cbPlayHidden.IsChecked.Value;

        private void cbBF_SelectionChanged(object sender, SelectionChangedEventArgs e) => SettingHelp.Settings.播放暂停 = GetKeysFormString(cbBF.Text, txtBF.Text);

        private void cbTZ_SelectionChanged(object sender, SelectionChangedEventArgs e) => SettingHelp.Settings.停止关闭 = GetKeysFormString(cbTZ.Text, txtTZ.Text);

        private void SlZL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => SettingHelp.Settings.视频质量 = (int)slZHiLiang.Value;

        private void SlZhenLv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => SettingHelp.Settings.视频帧率 = (int)slZhenLv.Value;

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
