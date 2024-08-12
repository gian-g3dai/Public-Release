using ICSharpCode.AvalonEdit; // Ensure you have this using directive
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows; // This namespace contains the Window class
using Unakin.Utils;



namespace Unakin.ToolWindows
{
    public partial class CodeDisplayWindow : Window
    {
        public CodeDisplayWindow()
        {
            InitializeComponent();
            this.Loaded += CodeDisplayWindow_Loaded;
        }

        private void CodeDisplayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Assuming "CustomLanguage" is the name under which you've registered your custom syntax highlighting
            CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CustomLanguage");
        }


        public void SetCodeText(string code)
        {
            // Assuming your TextEditor's x:Name is CodeEditor
            CodeEditor.Text = code;
        }
    }
}