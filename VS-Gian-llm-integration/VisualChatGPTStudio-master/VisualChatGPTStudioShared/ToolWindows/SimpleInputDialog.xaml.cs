using System.Windows;

namespace Unakin
{
    public partial class SimpleInputDialog : Window
    {
        public int Rows { get; private set; }

        public SimpleInputDialog()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtRows.Text, out int rows))
            {
                Rows = rows;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Invalid number. Please enter a valid number of rows.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
