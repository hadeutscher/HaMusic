using HaMusicLib;
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

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for AboutForm.xaml
    /// </summary>
    public partial class AboutForm : Window
    {
        public static DependencyProperty StartAnimation = DependencyProperty.Register("StartAnimation", typeof(bool), typeof(AboutForm), new PropertyMetadata(false));
        public static DependencyProperty Version = DependencyProperty.Register("Version", typeof(string), typeof(AboutForm), new PropertyMetadata(""));
        public AboutForm()
        {
            InitializeComponent();
            DataContext = this;
            SetValue(Version, "Version " + ServerDataSource.LocalVersion);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetValue(StartAnimation, true);
        }
    }
}
