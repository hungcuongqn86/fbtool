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
using System.Reactive.Linq;

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
            LoadProfile();
            LoadLink();
        }

        private void LoadProfile()
        {
            var profileData = firebase.Child("profile/server1").AsObservable<Profile>();
            dgProfile.ItemsSource = profileData.ObserveOnDispatcher().AsObservableCollection();
        }

        private void LoadLink()
        {
            var child = firebase.Child("link");
            var observable = child.AsObservable<Link>();

            var subscription = observable
                .Where(f => !string.IsNullOrEmpty(f.Key))
                .Where(f => f.Object?.Status != 1)
                .Subscribe(f => {
                    Console.WriteLine($"{f.Object.Url}: {f.Object.Status} : {f.EventType}");
                    /*List<Link> links = new List<Link>();
                    foreach (var profile in profileData)
                    {
                        profiles.Add(profile.Object);
                    }*/
                });
            // subscription.Dispose();

        }

        private async Task LoadLinkData()
        {
            var linkData = await firebase.Child("link").OrderByKey().OnceAsync<Link>();
            List<Link> links = new List<Link>();
            foreach (var link in linkData)
            {
                links.Add(link.Object);
            }
            dgLink.ItemsSource = links;
        }

        private async void mnuNewProfile_Click(object sender, RoutedEventArgs e)
        {
            await addProfileAsync();
        }

        private async Task addProfileAsync()
        {
            await firebase
              .Child("profile/server1")
              .PostAsync(new Profile("Profile 1", "Hường Dương"));
        }

        private async void mnuNewLink_Click(object sender, RoutedEventArgs e)
        {
            await addLinkAsync();
        }

        private async Task addLinkAsync()
        {
            await firebase
              .Child("link")
              .PostAsync(new Link("https://fb.me/1L99yBeX1EvzH7L", 0, ""));
        }

        private void openProfile(object sender, RoutedEventArgs e)
        {
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            KillChrome();

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
