using System;
using System.Reflection;
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
            cbHB.Text = SettingHelp.Settings.开关画笔.Item1.ToString();
            cbVideoCode.Text = SettingHelp.Settings.编码类型;
            txtBF.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.播放暂停.Item2);
            txtTZ.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.停止关闭.Item2);
            txtHB.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), SettingHelp.Settings.开关画笔.Item2);
            slZHiLiang.Value = SettingHelp.Settings.视频质量;
            slZHiLiang.ValueChanged += SlZL_ValueChanged;//必须放在Text赋值后再加载事件
            slZhenLv.Value = SettingHelp.Settings.视频帧率;
            slZhenLv.ValueChanged += SlZhenLv_ValueChanged;
            txtSavePath.Text = SettingHelp.Settings.保存路径;
            txtSavePath.TextChanged += txtSavePath_TextChanged;
            txtNameRule.Text = SettingHelp.Settings.命名规则;
            txtNameRule.TextChanged += txtNameRule_TextChanged;
        }

        /// <summary>
        /// 统一消息提醒(方便后期调整消息框样式)
        /// </summary>
        private void Message(string msg)
        {
            System.Windows.MessageBox.Show(msg);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e) => Close();

        private void cbPlayHidden_Click(object sender, RoutedEventArgs e) => SettingHelp.Settings.播放隐藏 = cbPlayHidden.IsChecked.Value;

        private void SlZL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            slZHiLiang.Value = (int)slZHiLiang.Value;
            SettingHelp.Settings.视频质量 = (int)slZHiLiang.Value;
        }

        private void SlZhenLv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            slZhenLv.Value = ((int)(slZhenLv.Value / 10)) * 10;//只允许整10的
            SettingHelp.Settings.视频帧率 = (int)slZhenLv.Value;
        }

        #region 快捷键
        private Tuple<HotKey.KeyModifiers, int> GetKeysFormString(string a, string b,string curSet)
        {
            HotKey.KeyModifiers A = HotKey.KeyModifiers.None;
            int B = 0;
            try
            {
                A = (HotKey.KeyModifiers)Enum.Parse(typeof(HotKey.KeyModifiers), a);
                B = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), b);
            }
            catch { }
            var result = new Tuple<HotKey.KeyModifiers, int>(A, B);
            PropertyInfo[] propertys = SettingHelp.Settings.GetType().GetProperties();
            foreach (PropertyInfo pi in propertys)//找到热键类属性，查找是否有冲突的热键设置
            {
                if (pi.PropertyType.Equals(typeof(Tuple<HotKey.KeyModifiers, int>)))//先判断是热键类设置
                {
                    var v = (Tuple<HotKey.KeyModifiers, int>)pi.GetValue(SettingHelp.Settings);//取除设置的值
                    if (Equals(result, v) && pi.Name != curSet)//防止没有变化的修改提示冲突
                    {
                        Message("当前热键设置冲突，可能导致部分热键失效，请重新设置");
                        break;
                    }
                }
            }
            return result;
        }

        private bool SetTextFormHandle(object sender, KeyEventArgs e)
        {
            int v;
            try
            {
                v = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), e.Key.ToString());
            }
            catch { return false; }
            (sender as TextBox).Text = Enum.GetName(typeof(System.Windows.Forms.Keys), v);
            e.Handled = true;//到此为止，不加这个Text处会显示重复的字母等混乱情况
            return true;
        }
        private void cbBF_DropDownClosed(object sender, EventArgs e) => SettingHelp.Settings.播放暂停 = GetKeysFormString(cbBF.Text, txtBF.Text, "播放暂停");
        private void cbTZ_DropDownClosed(object sender, EventArgs e) => SettingHelp.Settings.停止关闭 = GetKeysFormString(cbTZ.Text, txtTZ.Text, "停止关闭");
        private void cbHB_DropDownClosed(object sender, EventArgs e) => SettingHelp.Settings.开关画笔 = GetKeysFormString(cbHB.Text, txtHB.Text, "开关画笔");
        private void txtBF_KeyDown(object sender, KeyEventArgs e)
        {
            if (SetTextFormHandle(sender, e)) SettingHelp.Settings.播放暂停 = GetKeysFormString(cbBF.Text, txtBF.Text, "播放暂停");
        }
        private void txtTZ_KeyDown(object sender, KeyEventArgs e)
        {
            if (SetTextFormHandle(sender, e)) SettingHelp.Settings.停止关闭 = GetKeysFormString(cbTZ.Text, txtTZ.Text, "停止关闭");
        }
        private void txtHB_KeyDown(object sender, KeyEventArgs e)
        {
            if (SetTextFormHandle(sender, e)) SettingHelp.Settings.开关画笔 = GetKeysFormString(cbHB.Text, txtHB.Text, "开关画笔");
        }
        #endregion

        private void SavePath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSavePath.Text = folderBrowser.SelectedPath;
            }
        }

        private void txtSavePath_TextChanged(object sender, TextChangedEventArgs e) => SettingHelp.Settings.保存路径 = txtSavePath.Text;

        private void txtNameRule_TextChanged(object sender, TextChangedEventArgs e) => SettingHelp.Settings.命名规则 = string.IsNullOrEmpty(txtNameRule.Text) ? "yyMMdd_HHmmss" : txtNameRule.Text;//为空时使用默认

        private void cbVideoCode_DropDownClosed(object sender, EventArgs e) => SettingHelp.Settings.编码类型 = cbVideoCode.Text;
    }
}
