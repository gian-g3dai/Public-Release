using System.ComponentModel;
using System.Windows;

namespace Unakin.ToolWindows
{
    public partial class StepsDialog : Window
    {
        // Define a custom state property
        public string DialogState { get; private set; } = "None";

        public StepsDialog()
        {
            InitializeComponent();
        }

        public void SetStepsText(string text)
        {
            txtSteps.Text = text;
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogState = "Proceed";
            this.DialogResult = true;
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogState = "Regenerate";
            this.DialogResult = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogState = "Quit";
            this.Close(); // Close the dialog
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // If the DialogState hasn't been set, it means the window is being closed without any button press
            if (DialogState == "None")
            {
                DialogState = "Quit";
            }
        }
    }
}
