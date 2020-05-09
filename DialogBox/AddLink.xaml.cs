using fbtool.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for AddLink.xaml
    /// </summary>
    public partial class AddLink : Window, INotifyPropertyChanged
    {
        public AddLink()
        {
            InitializeComponent();
            Linkbd = new Link();
            this.DataContext = Linkbd;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Link Linkbd;

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
