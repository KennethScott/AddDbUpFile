using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Controls;

namespace KennethScott.AddDbUpFile
{
    public partial class FileNameDialog : Window
    {
        private const string DEFAULT_TEXT = "Enter a file name";
        private static readonly List<string> _tips = new List<string> {
            "Tip: 'folder/file' also creates a new folder for the file",
            "Tip: Create folder by ending the name with a forward slash",
            "Tip: Separate names with commas to add multiple files and folders"
        };

        public FileNameDialog(string folder)
        {
            InitializeComponent();

            lblFolder.Content = string.Format("{0}/", folder);

            Loaded += (s, e) =>
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/AddDbUpFile;component/Resources/icon.png", UriKind.RelativeOrAbsolute));
                Title = Vsix.Name;
                SetRandomTip();

                txtName.Focus();
                txtName.CaretIndex = 0;
                txtName.Text = DEFAULT_TEXT;
                txtName.Select(0, txtName.Text.Length);

                txtName.PreviewKeyDown += (a, b) =>
                {
                    if (b.Key == Key.Escape)
                    {
                        if (string.IsNullOrWhiteSpace(txtName.Text) || txtName.Text == DEFAULT_TEXT)
                        {
                            Close();
                        }
                        else
                        {
                            txtName.Text = string.Empty;
                        }
                    }
                    else if (txtName.Text == DEFAULT_TEXT)
                    {
                        txtName.Text = string.Empty;
                    }
                };

                txtName.PreviewMouseDown += (a, b) =>
                {
                    if (txtName.Text == DEFAULT_TEXT)
                        txtName.Text = string.Empty;
                };
            };
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            // Ignore invalid characters \/:*?"<>| and . 
            if (e.Key == Key.Oem5                                                               // \    
                //|| e.Key == Key.Oem2                                                            // /    Allow slash for creating folders
                || e.Key == Key.OemPeriod                                                       // .
                || (e.Key == Key.Oem2 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // ? 
                || (e.Key == Key.Oem5 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // |
                || (e.Key == Key.OemComma && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)  // <
                || (e.Key == Key.OemPeriod && e.KeyboardDevice.Modifiers == ModifierKeys.Shift) // >
                || (e.Key == Key.Oem1 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // :
                || (e.Key == Key.D8 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)        // *
                || (e.Key == Key.Oem7 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift))     // "
                e.Handled = true;
        }

        public string Input
        {
            get
            {
                string input = txtName.Text.Trim();
                if (!input.EndsWith("/"))
                    input += ((ComboBoxItem)cmbExtension.SelectedItem).Content.ToString();
                return input;
            }
        }

        public bool IsEmbeddedResource
        {
            get { return ckEmbeddedResource.IsChecked ?? false; }
        }

        private void SetRandomTip()
        {
            Random rnd = new Random(DateTime.Now.GetHashCode());
            int index = rnd.Next(_tips.Count);
            lblTips.Content = _tips[index];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void txtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Remove invalid filename characters and periods - but leave slash for folder creation
            txtName.Text = String.Join(String.Empty, txtName.Text.Split(new char[] { '\\', ':', '*', '?', '"', '<', '>', '|', '.' } ));

            btnCreate.IsEnabled = (txtName.Text != DEFAULT_TEXT && txtName.Text.Length > 0);
        }

        private void cmbExtension_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ckEmbeddedResource.IsChecked = (((ComboBoxItem)cmbExtension.SelectedItem).Content?.ToString() == ".sql");
        }
    }
}
