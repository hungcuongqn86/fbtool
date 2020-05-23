using fbtool.Model;
using Firebase.Database;
using Firebase.Database.Query;
using OpenQA.Selenium.Chrome;
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
    /// Interaction logic for ImportLink.xaml
    /// </summary>
    public partial class ImportLink : Window, INotifyPropertyChanged
    {
        FirebaseClient firebase;
        public ImportLink()
        {
            InitializeComponent();
            string firebaseUrl = ConfigurationManager.AppSettings["FirebaseUrl"].ToString();
            string firebaseSecretKey = ConfigurationManager.AppSettings["FirebaseSecretKey"].ToString();
            firebase = new FirebaseClient(firebaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(firebaseSecretKey)
            });
            Data = new DataImport();
            this.DataContext = Data;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DataImport Data;

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Dialog box canceled
            DialogResult = false;
        }

        private async void okButton_Click(object sender, RoutedEventArgs e)
        {
            // Don't accept the dialog box if there is invalid data
            if (!IsValid(this)) return;

            // Add profile
            string data = Data.Data;
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Do something with the line
                    await addLink(line);
                }
            }

            // Dialog box accepted
            DialogResult = true;
        }

        private async Task addLink(string line)
        {
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            // Update
            await firebase
                .Child("link/" + serverName)
                .PostAsync(new Link(line, 0, "", ""));
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
