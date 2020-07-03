using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace RecordWin.UserControls
{
    /// <summary>
    /// HotKeyUC.xaml 的交互逻辑
    /// </summary>
    public partial class HotKeyUC : UserControl, INotifyPropertyChanged
    {
        #region 数据绑定
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string hotkeyname = "";
        public string HotKeyName
        {
            get => hotkeyname;
            set
            {
                hotkeyname = value;
                NotifyPropertyChanged("HotKeyName");
            }
        }
        #endregion

        #region 变量
        /// <summary>
        /// 全部热键属性的名称列表
        /// </summary>
        private List<string> HotKeyList = new List<string>() { "播放暂停", "停止关闭", "开关画笔", };
        /// <summary>
        /// 当前对应的热键属性名称
        /// </summary>
        private string Key = string.Empty; 
        #endregion

        public HotKeyUC()
        {
            DataContext = this;
            InitializeComponent();
        }
        /// <summary>
        /// 为当前热键配置控件设置必须的属性
        /// </summary>
        /// <param name="HotKey">热键名称</param>
        /// <param name="PropertyName">对应的属性的名称</param>
        public void SetValue(string HotKey, string PropertyName)
        {
            HotKeyName = HotKey;
            Key = PropertyName;
            Tuple<HotKey.KeyModifiers, int> tuple = Functions.GetKeyPropertyValue<Tuple<HotKey.KeyModifiers, int>>(PropertyName, SettingHelp.Settings);
            comboBox.Text = tuple.Item1.ToString();
            txt.Text = Enum.GetName(typeof(System.Windows.Forms.Keys), tuple.Item2);
        }

        #region 私有方法
        /// <summary>
        /// 通过字符串生成指定的热键设置
        /// </summary>
        private void SetHotKey()
        {
            Enum.TryParse(comboBox.Text, out HotKey.KeyModifiers A);
            int B;
            try
            {
                B = (int)Enum.Parse(typeof(System.Windows.Forms.Keys), txt.Text);
            }
            catch
            {
                return;
            }
            var result = new Tuple<HotKey.KeyModifiers, int>(A, B);
            foreach (PropertyInfo p in SettingHelp.Settings.GetType().GetProperties())//找到热键类属性，查找是否有冲突的热键设置
            {
                if (p.PropertyType.Equals(typeof(Tuple<HotKey.KeyModifiers, int>)))//先判断是热键类设置
                {
                    //先取出设置的值比较，并且不是当前设置，防止没有变化的修改提示冲突
                    if (p.Name != Key && Equals(result, (Tuple<HotKey.KeyModifiers, int>)p.GetValue(SettingHelp.Settings)))
                    {
                        Functions.Message("当前热键设置冲突，可能导致部分热键失效，请重新设置");
                        return;
                    }
                }
            }
            Functions.SetKeyPropertyValue(Key, SettingHelp.Settings, result);
        }
        /// <summary>
        /// 设置快捷键的文本显示
        /// </summary>
        private bool SetTextFormEvent(object sender, KeyEventArgs e)
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
        #endregion

        #region UI事件
        private void Combox_DropDownClosed(object sender, EventArgs e) => SetHotKey();

        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (SetTextFormEvent(sender, e))
                SetHotKey();
        } 
        #endregion
    }
}
