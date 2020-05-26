using fbtool.Model;
using Firebase.Database;
using Firebase.Database.Query;
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
    /// Interaction logic for MainBm.xaml
    /// </summary>
    public partial class MainBm : Window, INotifyPropertyChanged
    {
        FirebaseClient firebase;
        public MainBm()
        {
            InitializeComponent();
            string firebaseUrl = ConfigurationManager.AppSettings["FirebaseUrl"].ToString();
            string firebaseSecretKey = ConfigurationManager.AppSettings["FirebaseSecretKey"].ToString();
            firebase = new FirebaseClient(firebaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(firebaseSecretKey)
            });
            loadData();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DataImport Data;

        private async void loadData()
        {
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            var mainBm = await firebase.Child("mainbm/" + serverName).OnceSingleAsync<DataImport>();
            if (mainBm != null)
            {
                Data = new DataImport(mainBm.Data, 0);
            }
            else
            {
                Data = new DataImport();
            }
            
            this.DataContext = Data;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Dialog box canceled
            DialogResult = false;
        }

        private async void okButton_Click(object sender, RoutedEventArgs e)
        {
            // Don't accept the dialog box if there is invalid data
            if (!IsValid(this)) return;

            // Add ID
            string serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
            // Update
            await firebase
                .Child("mainbm/" + serverName)
                .PutAsync(Data);

            // Dialog box accepted
            DialogResult = true;
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
