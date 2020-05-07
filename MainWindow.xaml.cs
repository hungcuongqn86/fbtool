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
using Firebase.Database;
using Firebase.Database.Query;
using fbtool.Model;

namespace fbtool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FirebaseClient firebase;
        ChromeDriver chromeDriver;
        public MainWindow()
        {
            InitializeComponent();
            firebase = new FirebaseClient("https://fbtool-e0efc.firebaseio.com/", new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult("lVOdDXogb8edtJdoVW1moAw6MjjUo7obcEQrIJSW")
            });
            LoadProfile().ContinueWith(task => { /* do some other stuff */ },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task LoadProfile()
        {
            var profileData = await firebase.Child("profile/server1").OrderByKey().OnceAsync<Profile>();
            List<Profile> profiles = new List<Profile>();
            foreach (var profile in profileData)
            {
                profiles.Add(profile.Object);
            }
            dgProfile.ItemsSource = profiles;
        }

        private async Task addProfileAsync()
        {
            await firebase
              .Child("profile/server1")
              .PostAsync(new Profile("Profile 1", "Hường Dương"));
        }

        private void openProfile(object sender, RoutedEventArgs e)
        {
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            // KillChrome();
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=C:\\Users\\hungc\\AppData\\Local\\Google\\Chrome\\User Data");
            options.AddArgument("profile-directory=" + profile.Path);
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
