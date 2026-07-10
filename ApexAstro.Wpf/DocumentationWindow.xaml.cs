using ApexAstro.Wpf.Helpers;
using System.Windows;

namespace ApexAstro.Wpf
{
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow(int initialTab = 0)
        {
            InitializeComponent();
            LoadAll();
            HelpTabControl.SelectedIndex = initialTab;
        }

        private void LoadAll()
        {
            var gs = MarkdownRenderer.LoadEmbeddedMarkdown("ApexAstro.Wpf.Documentation.GettingStarted.md");
            if (!string.IsNullOrEmpty(gs))
                MarkdownRenderer.Render(gs, GettingStartedContent);
            else
                MarkdownRenderer.RenderFallback(GettingStartedContent, "Getting Started");

            var doc = MarkdownRenderer.LoadEmbeddedMarkdown("ApexAstro.Wpf.Documentation.Help.md");
            if (!string.IsNullOrEmpty(doc))
                MarkdownRenderer.Render(doc, DocumentContent);
            else
                MarkdownRenderer.RenderFallback(DocumentContent, "Documentation");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
