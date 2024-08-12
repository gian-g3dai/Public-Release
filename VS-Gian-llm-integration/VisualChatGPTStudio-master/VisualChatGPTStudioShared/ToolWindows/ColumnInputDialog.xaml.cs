using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Unakin
{
    public partial class ColumnInputDialog : Window
    {
        public List<string> ColumnHeaders { get; private set; }
        public int Rows { get; private set; } // Add this line to store the number of rows

        public ColumnInputDialog(string title)
        {
            InitializeComponent();
            Title = title;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ColumnHeaders = new List<string>(txtColumnHeaders.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            // Try to parse the number of rows. If parsing fails, default to 0 or handle the error accordingly
            if (!int.TryParse(txtRows.Text, out int rows) || rows <= 0)
            {
                MessageBox.Show("Please enter a valid number of rows.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Rows = rows;

            DialogResult = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // Remove the watermark when the TextBox gets focus
            if (textBox != null && textBox.Background is VisualBrush)
            {
                textBox.Background = new SolidColorBrush(Colors.White); // Or another appropriate color
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // Only add the watermark back if the TextBox is empty
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "txtColumnHeaders")
                {
                    // Use the appropriate VisualBrush resource for the watermark
                    textBox.Background = (VisualBrush)this.Resources["columnHeadersWatermark"];
                }
                else if (textBox.Name == "txtRows")
                {
                    textBox.Background = (VisualBrush)this.Resources["rowsWatermark"];
                }
            }
        }

    }
}
