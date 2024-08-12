using System.Windows;

namespace Unakin
{

    public partial class LoginWindow : Window
    {
        public string Username
        {
            get { return UsernameTextBox.Text; }
        }

        public string Password
        {
            get { return PasswordBox.Password; }
        }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle the "Login" button click event here, e.g., validate credentials
            // You can close the window by calling this.Close() after successful login
            DialogResult = true; // This will close the window and return DialogResult as true
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

    }
}
