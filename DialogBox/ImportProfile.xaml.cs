using fbtool.Model;
using Firebase.Database;
using Firebase.Database.Query;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
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

namespace fbtool.DialogBox
{
    /// <summary>
    /// Interaction logic for ImportProfile.xaml
    /// </summary>
    public partial class ImportProfile : Window, INotifyPropertyChanged
    {
        FirebaseClient firebase;
        ChromeDriver chromeDriver;
        public ImportProfile()
        {
            InitializeComponent();
            string firebaseUrl = ConfigurationManager.AppSettings["FirebaseUrl"].ToString();
            string firebaseSecretKey = ConfigurationManager.AppSettings["FirebaseSecretKey"].ToString();
            firebase = new FirebaseClient(firebaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(firebaseSecretKey)
            });
            ViaData = new Via();
            this.DataContext = ViaData;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Via ViaData;

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Dialog box canceled
            DialogResult = false;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // Don't accept the dialog box if there is invalid data
            if (!IsValid(this)) return;

            // Add profile
            string data = ViaData.Data;
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Do something with the line
                    addVia(line);
                }
                if (chromeDriver != null)
                {
                    chromeDriver.Close();
                    System.Threading.Thread.Sleep(2000);
                    chromeDriver.Quit();
                    MessageBox.Show("Done");
                }
            }

            // Dialog box accepted
            DialogResult = true;
        }

        private async void addVia(string line)
        {
            string[] viadetail = line.Split('|');
            short status = 1;
            if (viadetail.Length == 3)
            {
                if (chromeDriver != null)
                {
                    chromeDriver.Close();
                    System.Threading.Thread.Sleep(2000);
                    chromeDriver.Quit();
                }
                string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();

                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--user-data-dir=" + profilePath + "/" + viadetail[0]);
                options.AddArgument("profile-directory=" + viadetail[0]);
                options.AddArgument("disable-infobars");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--start-maximized");
                chromeDriver = new ChromeDriver(options);
                chromeDriver.Url = "https://business.facebook.com/select/";
                chromeDriver.Navigate();
                waitLoading();

                // If login
                string url = chromeDriver.Url;
                if (url.Contains("https://business.facebook.com/login.php"))
                {
                    chromeDriver.FindElement(By.Name("email")).SendKeys(viadetail[0]);
                    chromeDriver.FindElement(By.Name("pass")).SendKeys(viadetail[1]);
                    chromeDriver.FindElement(By.Id("loginbutton")).Click();
                    waitLoading();
                    pass2Submit(viadetail[2]);
                    waitLoading();
                    saveBrowser();
                    waitLoading();
                }
                // Check success
                string curUrl = chromeDriver.Url;
                if (curUrl.Contains("business.facebook.com/checkpoint"))
                {
                    status = 0;
                }
                await saveToDbAsync(viadetail[0], viadetail[1], viadetail[2], status);
            }
        }

        private string genOtp(string secretKey)
        {
            var sKey = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(sKey);
            return totp.ComputeTotp();
        }

        private async Task saveToDbAsync(string id, string pass, string secretKey, short status)
        {
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            // Update
            await firebase
              .Child("profile/" + serverName + "/" + id)
              .PutAsync(new Profile(id, pass, secretKey, status));
        }

        private void pass2Submit(string secretKey)
        {
            ReadOnlyCollection<IWebElement> pass2 = chromeDriver.FindElements(By.Name("approvals_code"));
            if (pass2.Count > 0)
            {
                pass2.ElementAt(0).SendKeys(genOtp(secretKey));
                chromeDriver.FindElement(By.Id("checkpointSubmitButton")).Click();
            }
        }

        private void saveBrowser()
        {
            ReadOnlyCollection<IWebElement> elements = chromeDriver.FindElements(By.Name("name_action_selected"));
            if (elements.Count > 0)
            {
                elements.ElementAt(0).Click();
                chromeDriver.FindElement(By.Id("checkpointSubmitButton")).Click();
            }
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

        // Validate all dependency objects in a window
        private bool IsValid(DependencyObject node)
        {
            // Check if dependency object was passed
            if (node != null)
            {
                // Check if dependency object is valid.
                // NOTE: Validation.GetHasError works for controls that have validation rules attached 
                var isValid = !Validation.GetHasError(node);
                if (!isValid)
                {
                    // If the dependency object is invalid, and it can receive the focus,
                    // set the focus
                    if (node is IInputElement) Keyboard.Focus((IInputElement)node);
                    return false;
                }
            }

            // If this dependency object is valid, check all child dependency objects
            return LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>().All(IsValid);

            // All dependency objects are valid
        }
    }
}
