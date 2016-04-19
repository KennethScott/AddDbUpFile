using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;

namespace KennethScott.AddDbUpFile
{
    public partial class FileNameDialog : Window
    {
        private const string DEFAULT_TEXT = "Enter a file name";
        private static List<string> _tips = new List<string> {
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
                            Close();
                        else
                            txtName.Text = string.Empty;
                    }
                    else if (txtName.Text == DEFAULT_TEXT)
                    {
                        txtName.Text = string.Empty;
                        btnCreate.IsEnabled = true;
                    }
                };
            };
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            // Ignore invalid characters \/:*?"<>|
            if (e.Key == Key.Oem5                                                               // \    
                || e.Key == Key.Oem2                                                            // /   
                || (e.Key == Key.Oem2 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // ? 
                || (e.Key == Key.Oem5 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // |
                || (e.Key == Key.OemComma && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)  // <
                || (e.Key == Key.OemPeriod && e.KeyboardDevice.Modifiers == ModifierKeys.Shift) // >
                || (e.Key == Key.Oem1 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)      // :
                || (e.Key == Key.D8 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)        // *
                || (e.Key == Key.Oem7 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift))     // "
                e.Handled = true;

            // Only allow one period
            if (e.Key == Key.OemPeriod && txtName.Text.Contains("."))
                e.Handled = true;
        }

        public string Input
        {
            get { return txtName.Text.Trim(); }
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
            if (txtName.Text.Count(x => x == '.') <= 1)
            {
                DialogResult = true;
                Close();
            }
        }

        private void txtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Remove invalid characters 
            txtName.Text = String.Join(String.Empty, txtName.Text.Split(Path.GetInvalidFileNameChars()));

            // Only allow first period
            if (txtName.Text.Count(x => x == '.') > 1)
            {
                string reversed = Reverse(txtName.Text);

                string[] x = reversed.Split(new char[] { '.' }, 2);
                if (x.Length > 1)
                {
                    x[1] = x[1].Replace(".", "");
                }

                reversed = String.Join(".", x);

                txtName.Text = Reverse(reversed);
            }

            if (txtName.Text.EndsWith(".sql"))
                ckEmbeddedResource.IsChecked = true;
            else if (txtName.Text.EndsWith(".cs"))
                ckEmbeddedResource.IsChecked = false;

            txtName.CaretIndex = txtName.Text.Length;
        }

        private string Reverse(string s)
        {
            char[] temp = s.ToCharArray();
            Array.Reverse(temp);
            return new string(temp);
        }
    }
}
