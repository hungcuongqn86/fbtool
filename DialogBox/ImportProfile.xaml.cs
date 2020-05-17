using fbtool.Model;
using OpenQA.Selenium.Chrome;
using OtpNet;
using System;
using System.Collections.Generic;
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
        ChromeDriver chromeDriver;
        public ImportProfile()
        {
            InitializeComponent();
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
            }

            // Dialog box accepted
            DialogResult = true;
        }

        private void addVia(string line)
        {
            string[] viadetail = line.Split('|');
            if (viadetail.Length == 3)
            {
                /*var secretKey = Base32Encoding.ToBytes("5GJK63K2GE7XTFFWXQXUIMUPIJUTZJ32");
                var totp = new Totp(secretKey);
                var otp = totp.ComputeTotp();
                Console.WriteLine(otp);*/

                string profilePath = ConfigurationManager.AppSettings["ProfilePath"].ToString();

                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--user-data-dir=" + profilePath);
                options.AddArgument("profile-directory=" + viadetail[0]);
                options.AddArgument("disable-infobars");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--start-maximized");
                chromeDriver = new ChromeDriver(options);
                chromeDriver.Url = "https://www.facebook.com/";
                chromeDriver.Navigate();
            }
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
