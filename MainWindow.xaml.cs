using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace fbtool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ChromeDriver chromeDriver;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            chromeDriver.Quit();
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            KillChrome();
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=C:\\Users\\hungc\\AppData\\Local\\Google\\Chrome\\User Data");
            options.AddArgument("profile-directory=Profile 1");
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://www.facebook.com/";
            chromeDriver.Navigate();
        }

        private void KillChrome()
        {
            Process[] processNames = Process.GetProcessesByName("chrome");
            foreach (Process item in processNames)
            {
                item.Kill();
            }
        }
    }
}
