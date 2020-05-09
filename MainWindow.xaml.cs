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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Configuration;
using System.Xml;

namespace fbtool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        FirebaseClient firebase;
        ChromeDriver chromeDriver;

        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<Link> _returnedLinks = new ObservableCollection<Link>();

        ObservableCollection<Profile> _returnedProfiles = new ObservableCollection<Profile>();

        public MainWindow()
        {
            InitializeComponent();
            string firebaseUrl = ConfigurationManager.AppSettings["FirebaseUrl"].ToString();
            string firebaseSecretKey = ConfigurationManager.AppSettings["FirebaseSecretKey"].ToString();
            firebase = new FirebaseClient(firebaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(firebaseSecretKey)
            });
            LoadProfile();
            LoadLink();
            dgLink.ItemsSource = _returnedLinks;
            dgProfile.ItemsSource = _returnedProfiles;
        }

        private void LoadProfile()
        {
            var profileData = firebase.Child("profile/server1").AsObservable<Profile>();
            dgProfile.ItemsSource = profileData.ObserveOnDispatcher().AsObservableCollection();

            _returnedProfiles.Clear();
            var child = firebase.Child("profile/server1");
            var observable = child.AsObservable<Profile>();
            var subscription = observable
                .Where(f => !string.IsNullOrEmpty(f.Key)).ObserveOn(SynchronizationContext.Current)
                .Subscribe(f => {
                    if (f.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                    {
                        // record deleted - remove from ObservableCollection
                        _returnedProfiles.Remove(f.Object);
                    }

                    if (f.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        // see if the inserted/updated object is already in our ObservableCollection
                        var found = _returnedProfiles.FirstOrDefault(i => i.Path == f.Object.Path);
                        if (found == null)
                        {
                            //  is NOT in the observableCollection - add it
                            _returnedProfiles.Add(f.Object);
                        }
                        else
                        {
                            int tempIndex = _returnedProfiles.IndexOf(found);
                            // event was updated 
                            _returnedProfiles[tempIndex] = f.Object;
                        }
                    }
                });
            // subscription.Dispose();
        }

        private void LoadLink()
        {
            _returnedLinks.Clear();
            var child = firebase.Child("link");
            var observable = child.AsObservable<Link>();
            var subscription = observable
                .Where(f => !string.IsNullOrEmpty(f.Key)).ObserveOn(SynchronizationContext.Current)
                .Subscribe(f => {
                    if (f.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                    {
                        // record deleted - remove from ObservableCollection
                        _returnedLinks.Remove(f.Object);
                    }

                    if (f.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        // see if the inserted/updated object is already in our ObservableCollection
                        var found = _returnedLinks.FirstOrDefault(i => i.Url == f.Object.Url);
                        if (found == null)
                        {
                            //  is NOT in the observableCollection - add it
                            if (f.Object.Status == 0)
                            {
                                _returnedLinks.Add(f.Object);
                            }
                        }
                        else
                        {
                            int tempIndex = _returnedLinks.IndexOf(found);
                            // event was updated 
                            if (f.Object.Status == 0)
                            {
                                _returnedLinks[tempIndex] = f.Object;
                            }
                            else
                            {
                                _returnedLinks.Remove(f.Object);
                            }
                        }
                    }
                });
            // subscription.Dispose();
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

        /*private void Button_Click(object sender, RoutedEventArgs e)

        {

            if (!string.IsNullOrEmpty(textBoxCountry.Text))

            {

                UpdateConfigKey("Country", textBoxCountry.Text);

                textBoxCountry.Text = string.Empty;

            }

            else

                MessageBox.Show("Please type some value.");

        }*/

        public void UpdateConfigKey(string strKey, string newValue)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "..\\..\\App.config");

            if (!ConfigKeyExists(strKey))
            {
                throw new ArgumentNullException("Key", "<" + strKey + "> not find in the configuration.");
            }
            XmlNode appSettingsNode = xmlDoc.SelectSingleNode("configuration/appSettings");
            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == strKey)
                    childNode.Attributes["value"].Value = newValue;
            }

            xmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + "..\\..\\App.config");
            xmlDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            MessageBox.Show("Key Upated Successfullly");
        }

        public bool ConfigKeyExists(string strKey)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "..\\..\\App.config");
            XmlNode appSettingsNode = xmlDoc.SelectSingleNode("configuration/appSettings");
            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes["key"].Value == strKey)
                return true;
            }
            return false;
        }
    }
}
