using Community.VisualStudio.Toolkit;
using Unakin.Options;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Media;
using UnakinShared.Utils;
using OpenAI_API.Chat;
using System.Collections.ObjectModel;
using UnakinShared.DTO;
using System.IO;
using Microsoft.Win32;
using UnakinShared.Models;

namespace Unakin.ToolWindows
{
    public partial class AddAgentDialog : Window
    {
        public AgentDTO NewAgent { get; private set; }

        public AddAgentDialog()
        {
            InitializeComponent();
            SetInitialWatermarkVisibility();
        }

        private void SetInitialWatermarkVisibility()
        {
            watermarkImage.Visibility = string.IsNullOrEmpty(txtImage.Text) ? Visibility.Visible : Visibility.Collapsed;
            watermarkName.Visibility = string.IsNullOrEmpty(txtName.Text) ? Visibility.Visible : Visibility.Collapsed;
            watermarkFunctionality.Visibility = string.IsNullOrEmpty(txtFunctionality.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Validate the image path
            if (string.IsNullOrWhiteSpace(txtImage.Text) || !File.Exists(txtImage.Text))
            {
                MessageBox.Show("Please select a valid image.", "Invalid Image", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            NewAgent = new AgentDTO
            {
                Image = txtImage.Text,
                Name = txtName.Text,
                Functionality = txtFunctionality.Text
            };
            this.DialogResult = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                TextBlock watermark = FindWatermark(textBox);
                if (watermark != null)
                {
                    watermark.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrEmpty(textBox.Text))
            {
                TextBlock watermark = FindWatermark(textBox);
                if (watermark != null)
                {
                    watermark.Visibility = Visibility.Visible;
                }
            }
        }

        private TextBlock FindWatermark(TextBox textBox)
        {
            if (textBox == txtImage) return watermarkImage;
            if (textBox == txtName) return watermarkName;
            if (textBox == txtFunctionality) return watermarkFunctionality;
            return null;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                txtImage.Text = openFileDialog.FileName;
                watermarkImage.Visibility = Visibility.Collapsed; // Explicitly hide the watermark
            }
        }

    }
}
