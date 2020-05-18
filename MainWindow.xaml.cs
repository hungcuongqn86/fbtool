using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
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
using fbtool.DialogBox;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using OtpNet;

namespace fbtool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        FirebaseClient firebase;
        ChromeDriver chromeDriver;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            _returnedProfiles.Clear();
            var child = firebase.Child("profile/" + serverName);
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
                        var found = _returnedProfiles.FirstOrDefault(i => i.Fid == f.Object.Fid);
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
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            _returnedLinks.Clear();
            var child = firebase.Child("link/" + serverName);
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
                                f.Object.Key = f.Key;
                                _returnedLinks.Add(f.Object);
                            }
                        }
                        else
                        {
                            int tempIndex = _returnedLinks.IndexOf(found);
                            // event was updated 
                            if (f.Object.Status == 0)
                            {
                                f.Object.Key = f.Key;
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
            // Instantiate the dialog box
            var dlg = new AddProfile
            {
                Owner = this
            };

            // Open the dialog box modally 
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
                // Update
                await firebase
                  .Child("profile/" + serverName + "/" + dlg.Profilebd.Fid)
                  .PutAsync(new Profile(dlg.Profilebd.Fid, dlg.Profilebd.Password, dlg.Profilebd.SecretKey));
            }
        }

        private void MnuImportProfile_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            var dlg = new ImportProfile
            {
                Owner = this
            };

            // Open the dialog box modally 
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {

            }
        }

        private async void mnuNewLink_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            var dlg = new AddLink
            {
                Owner = this
            };

            // Open the dialog box modally 
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
                await firebase
                    .Child("link/" + serverName)
                    .PostAsync(new Link(dlg.Linkbd.Url, 0, "", ""));
            }
        }

        private void openProfile(object sender, RoutedEventArgs e)
        {
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Fid);
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://business.facebook.com/select/";
            chromeDriver.Navigate();
        }

        private void waitLoading()
        {
            // wait loading
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(30));
            Func<IWebDriver, bool> waitLoading = new Func<IWebDriver, bool>((IWebDriver Web) =>
            {
                try
                {
                    IWebElement alertE = Web.FindElement(By.Id("abccuongnh"));
                    return false;
                }
                catch
                {
                    return true;
                }
            });

            try
            {
                wait.Until(waitLoading);
            }
            catch { }
        }

        private void RemoveDeadAccount(object sender, RoutedEventArgs e)
        {
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Fid);
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://business.facebook.com/select/";
            chromeDriver.Navigate();
            waitLoading();

            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(15));
            ReadOnlyCollection<IWebElement> table = chromeDriver.FindElements(By.XPath("//div[@class='_3-8-']/table[contains(@class, 'uiGrid')]"));
            if (table.Count > 0)
            {
                ReadOnlyCollection<IWebElement> anchList = table.ElementAt(0).FindElements(By.TagName("a"));
                String selectLinkOpeninNewTab = Keys.Control + Keys.Return;
                foreach (IWebElement item in anchList)
                {
                    item.SendKeys(selectLinkOpeninNewTab);
                }
            }

            ReadOnlyCollection<string> tabs = chromeDriver.WindowHandles;
            foreach (string tab in tabs)
            {
                chromeDriver.SwitchTo().Window(tab);
                waitLoading();
                // Click setting
                ReadOnlyCollection<IWebElement> errAlert = chromeDriver.FindElements(By.XPath("//div[@class='_29dy']"));
                if (errAlert.Count > 0)
                {
                    string colorchk = errAlert.ElementAt(0).GetCssValue("background-color");
                    if ((colorchk.Equals("rgb(206, 0, 47)")) || colorchk.Equals("rgba(206, 0, 47, 1)"))
                    {

                        // Get id
                        string businessId = "";
                        string curUrl = chromeDriver.Url;
                        var queryString = curUrl.Split('?').Last();
                        var JIDArrVal = queryString.Split('&');
                        foreach (string item in JIDArrVal)
                        {
                            var itemSplit = item.Split('=');
                            if (itemSplit.Length > 1)
                            {
                                if (itemSplit[0] == "business_id")
                                {
                                    businessId = itemSplit[1];
                                }
                            }
                        }
                        chromeDriver.Url = "https://business.facebook.com/settings/info?business_id=" + businessId;
                        chromeDriver.Navigate();
                    }
                    else
                    {
                        if (chromeDriver.FindElements(By.XPath("//div[@class='_3-8-']/table[contains(@class, 'uiGrid')]")).Count == 0)
                        {
                            chromeDriver.Close();
                        }
                    }
                }
                else
                {
                    if (chromeDriver.FindElements(By.XPath("//div[@class='_3-8-']/table[contains(@class, 'uiGrid')]")).Count == 0)
                    {
                        chromeDriver.Close();
                    }
                }
            }

            // Leaving
            ReadOnlyCollection<string> tabsFull = chromeDriver.WindowHandles;
            foreach (string tab in tabsFull)
            {
                chromeDriver.SwitchTo().Window(tab);
                waitLoading();

                ReadOnlyCollection<IWebElement> bmnameElementf = chromeDriver.FindElements(By.XPath("//div[@class='_skv']"));
                if (bmnameElementf.Count == 0)
                {
                    bmnameElementf = chromeDriver.FindElements(By.XPath("//div[@class='_3-90']/div[@class='ellipsis']"));
                }
                if (bmnameElementf.Count > 0)
                {
                    string bmname = bmnameElementf.ElementAt(0).Text;
                    string xpath = "//button[contains(@class, '_271k')]/div[@class='_43rl']/div[contains(., '" + bmname + "') and @class='_43rm']";
                    ReadOnlyCollection<IWebElement> LeavingBtnText = chromeDriver.FindElements(By.XPath(xpath));
                    if (LeavingBtnText.Count > 0)
                    {
                        IWebElement divParentLeavingBtnText = LeavingBtnText.ElementAt(0).FindElement(By.XPath(".."));
                        IWebElement buttonLeavingBtn = divParentLeavingBtnText.FindElement(By.XPath(".."));
                        buttonLeavingBtn.Click();

                        // Confirm
                        System.Threading.Thread.Sleep(2000);
                        ReadOnlyCollection<IWebElement> btnConfirm = chromeDriver.FindElements(By.XPath(xpath));
                        if (btnConfirm.Count > 0)
                        {
                            foreach (IWebElement item in btnConfirm)
                            {
                                divParentLeavingBtnText = item.FindElement(By.XPath(".."));
                                buttonLeavingBtn = divParentLeavingBtnText.FindElement(By.XPath(".."));
                                string colorchk = buttonLeavingBtn.GetCssValue("background-color");
                                if ((colorchk.Equals("rgb(24, 119, 242)")) || colorchk.Equals("rgba(24, 119, 242, 1)"))
                                {
                                    buttonLeavingBtn.Click();
                                }
                            }
                        }
                    }
                }
            }

            // end proccess
            chromeDriver.Quit();
            MessageBox.Show("Done!");
        }

        private async void accessLinkAsync(object sender, RoutedEventArgs e)
        {
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Fid);
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://business.facebook.com/select/";
            chromeDriver.Navigate();
            waitLoading();
            // If login
            string url = chromeDriver.Url;
            if (!url.Contains("https://business.facebook.com/login.php"))
            {
                // Open link
                Link curentLink = _returnedLinks.First();
                chromeDriver.Navigate().GoToUrl(curentLink.Url);
                waitLoading();
                WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
                ReadOnlyCollection<IWebElement> fullNameE = chromeDriver.FindElements(By.Name("fullName"));
                if (fullNameE.Count > 0)
                {
                    // Input full name
                    String fullName = "A " + GetRandomAlphaNumeric();
                    fullNameE.ElementAt(0).SendKeys(fullName);
                }

                // wait button enable
                Func<IWebDriver, bool> waitForContBtnEnable = new Func<IWebDriver, bool>((IWebDriver Web) =>
                {
                    Console.WriteLine("Waiting for enable cont button");
                    IWebElement parent = Web.FindElement(By.TagName("button"));
                    IWebElement element = parent.FindElement(By.XPath("div[@class='_43rl']/div[@class='_43rm']"));
                    if (parent.GetAttribute("aria-disabled").Equals("false"))
                    {
                        return true;
                    }
                    return false;
                });
                wait.Until(waitForContBtnEnable);

                // Click button
                chromeDriver.FindElement(By.TagName("button")).Click();

                // wait password
                Func<IWebDriver, bool> waitForPassword = new Func<IWebDriver, bool>((IWebDriver Web) =>
                {
                    Console.WriteLine("Waiting for password");
                    IWebElement element = Web.FindElement(By.TagName("input"));
                    if (element.GetAttribute("type").Equals("password"))
                    {
                        return true;
                    }
                    return false;
                });
                wait.Until(waitForPassword);
                // MessageBox.Show("password!");

                // Input pass
                chromeDriver.FindElement(By.XPath("//input[@type='password']")).SendKeys(profile.Password);

                // wait button enable
                wait.Until(waitForContBtnEnable);
                // MessageBox.Show("button enable!");
                chromeDriver.FindElement(By.TagName("button")).Click();

                // Update link status
                string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
                curentLink.Status = 1;
                await firebase
                  .Child("link/" + serverName)
                  .Child(curentLink.Key)
                  .PutAsync(curentLink);
            }
            chromeDriver.Quit();
        }

        private void genPass2(object sender, RoutedEventArgs e)
        {
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            var sKey = Base32Encoding.ToBytes(profile.SecretKey);
            var totp = new Totp(sKey);
            MessageBox.Show(totp.ComputeTotp());
        }

        private string GetRandomAlphaNumeric()
        {
            return System.IO.Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
        }

        private void KillChrome()
        {
            Process[] processNames = Process.GetProcessesByName("chrome");
            foreach (Process item in processNames)
            {
                item.Kill();
            }
        }

        private void serverNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            var dlg = new ServerNameSetting
            {
                Owner = this
            };

            // Open the dialog box modally 
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                // Update
                MessageBox.Show("Tool sẽ khởi động lại!");
                KillChrome();
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void exitLink_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
