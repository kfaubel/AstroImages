using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Documentation Window - displays help content and README information.
    /// This window can display markdown content converted to WPF's FlowDocument format.
    /// </summary>
    public partial class DocumentationWindow : Window
    {
        /// <summary>
        /// Constructor - sets up the documentation window and loads content.
        /// </summary>
        public DocumentationWindow()
        {
            // Initialize the WPF components from the XAML file
            InitializeComponent();
            
            // Load and display the documentation content
            LoadDocumentation();
        }

        /// <summary>
        /// Loads documentation content from embedded Help.md resource file.
        /// This method loads the bundled help documentation from the embedded resource.
        /// </summary>
        private void LoadDocumentation()
        {
            try
            {
                // Load the embedded Help.md resource
                string? markdownContent = LoadEmbeddedResource("AstroImages.Wpf.Documentation.Help.md");
                
                if (!string.IsNullOrEmpty(markdownContent))
                {
                    // Convert markdown to displayable format
                    DisplayMarkdown(markdownContent);
                }
                else
                {
                    // Resource not found, show fallback content
                    DisplayFallbackContent();
                }
            }
            catch
            {
                // If any error occurs, show fallback content
                // We don't want documentation loading to crash the app
                DisplayFallbackContent();
            }
        }

        /// <summary>
        /// Loads an embedded resource from the current assembly as a string.
        /// </summary>
        /// <param name="resourceName">The full name of the embedded resource</param>
        /// <returns>The resource content as a string, or null if not found</returns>
        private string? LoadEmbeddedResource(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // Debug: List all available resources
                var availableResources = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"Available embedded resources ({availableResources.Length}):");
                foreach (var resource in availableResources)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {resource}");
                }
                
                // Try to load the specific resource
                System.Diagnostics.Debug.WriteLine($"Attempting to load: {resourceName}");
                using var stream = assembly.GetManifestResourceStream(resourceName);
                
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource '{resourceName}' not found.");
                    return null;
                }
                
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                System.Diagnostics.Debug.WriteLine($"Successfully loaded resource. Content length: {content.Length}");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading embedded resource: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts markdown text to WPF FlowDocument format for display.
        /// Enhanced parser that handles headers, lists, paragraphs, bold text, and code blocks.
        /// </summary>
        /// <param name="markdownContent">The markdown text to convert and display</param>
        private void DisplayMarkdown(string markdownContent)
        {
            // Clear any existing content in the document
            DocumentContent.Blocks.Clear();
            
            // Split the markdown into individual lines for processing
            var lines = markdownContent.Split('\n');
            
            // Keep track of the current paragraph and whether we're in a code block
            Paragraph? currentParagraph = null;
            bool inCodeBlock = false;
            Paragraph? codeBlock = null;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Handle code block start/end
                if (trimmedLine.StartsWith("```"))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    
                    if (!inCodeBlock)
                    {
                        // Start code block
                        inCodeBlock = true;
                        codeBlock = new Paragraph();
                        codeBlock.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248));
                        codeBlock.Margin = new Thickness(10, 5, 10, 5);
                        codeBlock.Padding = new Thickness(10);
                        codeBlock.FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace");
                        codeBlock.FontSize = 12;
                    }
                    else
                    {
                        // End code block
                        inCodeBlock = false;
                        if (codeBlock != null)
                        {
                            DocumentContent.Blocks.Add(codeBlock);
                            codeBlock = null;
                        }
                    }
                    continue;
                }
                
                // Handle content inside code blocks
                if (inCodeBlock)
                {
                    if (codeBlock != null)
                    {
                        if (codeBlock.Inlines.Count > 0)
                        {
                            codeBlock.Inlines.Add(new LineBreak());
                        }
                        codeBlock.Inlines.Add(new Run(line)); // Use original line to preserve indentation
                    }
                    continue;
                }
                
                // Handle empty lines outside code blocks
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    continue;
                }
                
                // Handle headers
                if (trimmedLine.StartsWith("# "))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    
                    var header = new Paragraph();
                    header.Inlines.Add(new Run(trimmedLine.Substring(2)) { FontSize = 18, FontWeight = FontWeights.Bold });
                    header.Margin = new Thickness(0, 10, 0, 5);
                    DocumentContent.Blocks.Add(header);
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    
                    var header = new Paragraph();
                    header.Inlines.Add(new Run(trimmedLine.Substring(3)) { FontSize = 16, FontWeight = FontWeights.Bold });
                    header.Margin = new Thickness(0, 8, 0, 4);
                    DocumentContent.Blocks.Add(header);
                }
                else if (trimmedLine.StartsWith("### "))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    
                    var header = new Paragraph();
                    header.Inlines.Add(new Run(trimmedLine.Substring(4)) { FontSize = 14, FontWeight = FontWeights.Bold });
                    header.Margin = new Thickness(0, 6, 0, 3);
                    DocumentContent.Blocks.Add(header);
                }
                // Handle list items
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    if (currentParagraph != null)
                    {
                        DocumentContent.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    
                    var listItem = new Paragraph();
                    var listText = trimmedLine.Substring(2);
                    ProcessInlineFormatting(listItem, "• " + listText);
                    listItem.Margin = new Thickness(20, 2, 0, 2);
                    DocumentContent.Blocks.Add(listItem);
                }
                // Handle regular paragraphs
                else
                {
                    if (currentParagraph == null)
                    {
                        currentParagraph = new Paragraph();
                        currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    }
                    else
                    {
                        currentParagraph.Inlines.Add(new LineBreak());
                    }
                    
                    ProcessInlineFormatting(currentParagraph, trimmedLine);
                }
            }
            
            if (currentParagraph != null)
            {
                DocumentContent.Blocks.Add(currentParagraph);
            }
        }

        /// <summary>
        /// Processes inline formatting like bold (**text**) and inline code (`code`)
        /// </summary>
        /// <param name="paragraph">The paragraph to add formatted text to</param>
        /// <param name="text">The text to process</param>
        private void ProcessInlineFormatting(Paragraph paragraph, string text)
        {
            var parts = System.Text.RegularExpressions.Regex.Split(text, @"(\*\*.*?\*\*|`.*?`)");
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    // Bold text
                    var boldText = part.Substring(2, part.Length - 4);
                    paragraph.Inlines.Add(new Run(boldText) { FontWeight = FontWeights.Bold });
                }
                else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
                {
                    // Inline code
                    var codeText = part.Substring(1, part.Length - 2);
                    var codeRun = new Run(codeText)
                    {
                        FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace"),
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                        FontSize = 11
                    };
                    paragraph.Inlines.Add(codeRun);
                }
                else
                {
                    // Regular text
                    paragraph.Inlines.Add(new Run(part));
                }
            }
        }

        /// <summary>
        /// Displays fallback content when the embedded documentation resource cannot be loaded.
        /// This is a minimal backup in case the embedded Help.md file is missing or corrupted.
        /// </summary>
        private void DisplayFallbackContent()
        {
            DocumentContent.Blocks.Clear();
            
            var title = new Paragraph();
            title.Inlines.Add(new Run("AstroImages Documentation") { FontSize = 18, FontWeight = FontWeights.Bold });
            title.Margin = new Thickness(0, 0, 0, 10);
            DocumentContent.Blocks.Add(title);
            
            var content = new Paragraph();
            content.Inlines.Add(new Run("Documentation could not be loaded from the embedded resource.\n\n"));
            content.Inlines.Add(new Run("AstroImages is a FITS and XISF image viewer and analysis tool for astronomical imaging.\n\n"));
            content.Inlines.Add(new Run("Basic features include image viewing, zooming, panning, and metadata display.\n"));
            content.Inlines.Add(new Run("Please check that the application was installed correctly."));
            DocumentContent.Blocks.Add(content);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}