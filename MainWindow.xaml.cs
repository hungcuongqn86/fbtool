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
                  .Child("profile/" + serverName)
                  .PostAsync(new Profile(dlg.Profilebd.Path, dlg.Profilebd.Facebook, dlg.Profilebd.UserName, dlg.Profilebd.Password));
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
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            KillChrome();

            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Path);
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://www.facebook.com/";
            chromeDriver.Navigate();
        }

        private async void removeDeadAccount(object sender, RoutedEventArgs e)
        {
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Path);
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = "https://business.facebook.com/";
            chromeDriver.Navigate();

            // wait login
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(15));
            Func<IWebDriver, bool> waitForLogin = new Func<IWebDriver, bool>((IWebDriver Web) =>
            {
                Console.WriteLine("Waiting for login Facebook");
                IWebElement parent1 = Web.FindElement(By.XPath("//table[contains(@class, 'uiGrid')]"));
                ReadOnlyCollection<IWebElement> anchos = parent1.FindElements(By.TagName("a"));
                if (anchos.Count > 0)
                {
                    return true;
                }
                return false;
            });
            wait.Until(waitForLogin);
            // MessageBox.Show("login!");

            IWebElement table = chromeDriver.FindElement(By.XPath("//table[contains(@class, 'uiGrid')]"));
            ReadOnlyCollection<IWebElement> anchList = table.FindElements(By.TagName("a"));
            String selectLinkOpeninNewTab = Keys.Control + Keys.Return;
            foreach (IWebElement item in anchList)
            {
                item.SendKeys(selectLinkOpeninNewTab);
            }
            ReadOnlyCollection<string> tabs = chromeDriver.WindowHandles;
            foreach (string tab in tabs)
            {
                chromeDriver.SwitchTo().Window(tab);

                Func<IWebDriver, bool> waitShowDiedBm = new Func<IWebDriver, bool>((IWebDriver Web) =>
                {
                    try
                    {
                        IWebElement alertE = Web.FindElement(By.XPath("//div[@class='_29dy']"));
                        string color = alertE.GetCssValue("background-color");
                        if ((color.Equals("rgb(206, 0, 47)")) || color.Equals("rgba(206, 0, 47, 1)"))
                        {
                            return true;
                        }
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                });

                try
                {
                    wait.Until(waitShowDiedBm);
                }
                catch {}

                // Click setting
                ReadOnlyCollection<IWebElement> errAlert = chromeDriver.FindElements(By.XPath("//div[@class='_29dy']"));
                if (errAlert.Count > 0)
                {
                    string colorchk = errAlert.ElementAt(0).GetCssValue("background-color");
                    if ((colorchk.Equals("rgb(206, 0, 47)")) || colorchk.Equals("rgba(206, 0, 47, 1)"))
                    {
                        chromeDriver.FindElement(By.XPath("//a[contains(@href, 'https://business.facebook.com/settings?')]")).Click();
                    }
                }

                if (chromeDriver.FindElements(By.XPath("//div[@class='_3-8-']/table[contains(@class, 'uiGrid')]")).Count == 0)
                {
                    chromeDriver.Close();
                }
            }

            // Leaving
            ReadOnlyCollection<string> tabsFull = chromeDriver.WindowHandles;
            foreach (string tab in tabsFull)
            {
                chromeDriver.SwitchTo().Window(tab);

                Func<IWebDriver, bool> waitShowInfoButton = new Func<IWebDriver, bool>((IWebDriver Web) =>
                {
                    try
                    {
                        ReadOnlyCollection<IWebElement> iconElements = Web.FindElements(By.XPath("//i[contains(@class, 'sx_15dd7f')]"));
                        if (iconElements.Count > 0)
                        {
                            return true;
                        }
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                });

                try
                {
                    wait.Until(waitShowInfoButton);
                }
                catch { }

                // Click infoButton
                ReadOnlyCollection<IWebElement> iconElementss = chromeDriver.FindElements(By.XPath("//i[contains(@class, 'sx_15dd7f')]"));
                if (iconElementss.Count > 0)
                {
                    IWebElement parentr1 = iconElementss.ElementAt(0).FindElement(By.XPath(".."));
                    IWebElement parentr2 = parentr1.FindElement(By.XPath(".."));
                    IWebElement parentr3 = parentr2.FindElement(By.XPath(".."));
                    ReadOnlyCollection<IWebElement> InfoButtons = parentr3.FindElements(By.TagName("button"));

                    if (InfoButtons.Count > 0)
                    {
                        InfoButtons.ElementAt(0).Click();
                        // wait Leaving btn
                        Func<IWebDriver, bool> waitShowLeavingButton = new Func<IWebDriver, bool>((IWebDriver Web) =>
                        {
                            try
                            {
                                IWebElement parent1 = Web.FindElement(By.XPath("//button[contains(@class, '_271k')]"));
                                IWebElement parent2 = parent1.FindElement(By.XPath("div[@class='_43rl']"));
                                IWebElement element = parent2.FindElement(By.XPath("div[@class='_43rm']"));

                                if (element.GetAttribute("data-hover").Equals("tooltip"))
                                {
                                    return true;
                                }
                                return false;
                            }
                            catch
                            {
                                return true;
                            }
                        });

                        try
                        {
                            wait.Until(waitShowLeavingButton);
                        }
                        catch { }

                        // click Leaving btn

                        // Confirm

                    }
                }
            }

            // end proccess
            chromeDriver.SwitchTo().Window(tabs.ElementAt(0));
        }

        private async void accessLinkAsync(object sender, RoutedEventArgs e)
        {
            Profile profile = ((FrameworkElement)sender).DataContext as Profile;
            if (chromeDriver != null)
            {
                chromeDriver.Quit();
            }
            KillChrome();

            ChromeOptions options = new ChromeOptions();
            string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();
            options.AddArgument("--user-data-dir=" + profilePath);
            options.AddArgument("profile-directory=" + profile.Path);
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Url = profile.Facebook;
            chromeDriver.Navigate();

            // wait login
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromMinutes(1));
            Func<IWebDriver, bool> waitForLogin = new Func<IWebDriver, bool>((IWebDriver Web) =>
            {
                Console.WriteLine("Waiting for login Facebook");
                IWebElement parent1 = Web.FindElement(By.Id("u_0_a"));
                IWebElement element = parent1.FindElement(By.XPath("div[contains(@class, '_cy6')]/div[contains(@class, '_4kny')]/div[contains(@class, '_cy7')]/a"));
                if (element.GetAttribute("href").Equals(profile.Facebook))
                {
                    return true;
                }
                return false;
            });
            wait.Until(waitForLogin);
            // MessageBox.Show("login!");

            // Open link
            Link curentLink = _returnedLinks.First();
            chromeDriver.Navigate().GoToUrl(curentLink.Url);

            // wait load input
            // WebDriverWait waitInputFullName = new WebDriverWait(chromeDriver, TimeSpan.FromMinutes(1));
            Func<IWebDriver, bool> waitForInputFullName = new Func<IWebDriver, bool>((IWebDriver Web) =>
            {
                Console.WriteLine("Waiting for load input");
                IWebElement element = Web.FindElement(By.Name("fullName"));
                if (element.GetAttribute("class").Equals("_58al"))
                {
                    return true;
                }
                return false;
            });
            wait.Until(waitForInputFullName);
            // MessageBox.Show("load input!");

            // Input full name
            String fullName = "A " + GetRandomAlphaNumeric();
            chromeDriver.FindElement(By.Name("fullName")).SendKeys(fullName);

            // wait button enable
            // WebDriverWait waitContBtnEnable = new WebDriverWait(chromeDriver, TimeSpan.FromMinutes(1));
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
            // MessageBox.Show("button enable!");

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

            // wait success

            // Update link status
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            curentLink.Status = 1;
            await firebase
              .Child("link/" + serverName)
              .Child(curentLink.Key)
              .PutAsync(curentLink);
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
