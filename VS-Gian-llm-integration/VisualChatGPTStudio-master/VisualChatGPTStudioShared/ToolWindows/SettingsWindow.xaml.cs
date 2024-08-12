using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Unakin.ToolWindows
{
    public partial class SettingsWindow : Window
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public SettingsWindow(double initialLeft, double top, double height)
        {
            InitializeComponent();
            this.Left = initialLeft; // Set the initial left position to the right edge of the ChatControl window
            this.Top = top; // Align the top of the SettingsWindow with the ChatControl window
            this.Height = height; // Optional: Match the height of the ChatControl window
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Store the username and password
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;

            // Here you could pass the Username and Password to OptionPageGridGeneral
            // This is a placeholder for that functionality

            this.DialogResult = true; // Close the window
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var targetLeft = this.Left - this.Width; // Calculate the target position to slide towards the left

            var animation = new DoubleAnimation
            {
                From = this.Left,
                To = targetLeft,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            this.BeginAnimation(Window.LeftProperty, animation);
        }

    }


}
