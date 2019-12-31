using RecordWin.Properties;
using System.Windows;

namespace RecordWin
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        int RecordType;
        public SettingWindow()
        {
            InitializeComponent();
            RecordType = Settings.Default.录制类型;
            switch (RecordType)
            {
                case 0: rbZM.IsChecked = true; break;
                case 1: rbSXT.IsChecked = true; break;
                case 2: rbSY.IsChecked = true; break;
            }
            cbSK.IsChecked = Settings.Default.声卡;
            cbMK.IsChecked = Settings.Default.麦克风;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (RecordType == 2 && cbSK.IsChecked.Value == false && cbMK.IsChecked.Value == false)
            {
                MessageBox.Show("一定要选择一个声音的采集源！");
                return;
            }
            Settings.Default.录制类型 = RecordType;
            Settings.Default.声卡 = cbSK.IsChecked.Value;
            Settings.Default.麦克风 = cbMK.IsChecked.Value;
            Close();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (rbZM.IsChecked.Value)
                RecordType = 0;
            else if (rbSXT.IsChecked.Value)
                RecordType = 1;
            else if (rbSY.IsChecked.Value)
                RecordType = 2;
        }
    }
}
