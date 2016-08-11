using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace okiagarikobosi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int count;
        private DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            count = 3;
        }

        private void Timer_Tick(object sender, object e)
        {
            count--;
            textBlock.Text = count.ToString();
            if (count <= 0)
            {
                throw new Exception("自爆しました。");
            }
        }

        private void jibaku_button_Click(object sender, RoutedEventArgs e)
        {
            jibaku_button.Visibility = Visibility.Collapsed;
            textBlock.Text = count.ToString();
            textBlock.Visibility = Visibility.Visible;
            timer.Start();
        }
    }
}
