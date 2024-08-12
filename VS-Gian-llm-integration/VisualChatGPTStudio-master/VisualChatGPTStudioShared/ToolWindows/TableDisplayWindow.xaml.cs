using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Unakin
{
    public partial class TableDisplayWindow : Window
    {
        public TableDisplayWindow()
        {
            InitializeComponent();
        }

        // Call this method from your GetDataGenerationResponseAsync to display the data
        public void DisplayData(string data)
        {
            DataTextBox.Text = data;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = "GeneratedTable",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, DataTextBox.Text);
                    MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save the file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
