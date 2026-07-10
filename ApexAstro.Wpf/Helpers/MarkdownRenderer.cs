using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using WpfBrush = System.Windows.Media.Brush;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfApplication = System.Windows.Application;

namespace ApexAstro.Wpf.Helpers
{
    /// <summary>
    /// Shared static helper that converts a subset of Markdown into a WPF FlowDocument.
    /// Handles: headings (# ## ###), bullet lists (- *), paragraphs, bold (**), inline code (`), fenced code blocks (```).
    /// </summary>
    public static class MarkdownRenderer
    {
        public static string? LoadEmbeddedMarkdown(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch { return null; }
        }

        public static void Render(string markdownContent, FlowDocument doc)
        {
            doc.Blocks.Clear();
            var lines = markdownContent.Split('\n');
            Paragraph? currentParagraph = null;
            bool inCodeBlock = false;
            Paragraph? codeBlock = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("```"))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlock = new Paragraph();
                        if (WpfApplication.Current.TryFindResource("ThemeCodeBlockBackground") is WpfBrush bg)
                            codeBlock.Background = bg;
                        if (WpfApplication.Current.TryFindResource("ThemeCodeBlockForeground") is WpfBrush fg)
                            codeBlock.Foreground = fg;
                        codeBlock.Margin = new Thickness(10, 5, 10, 5);
                        codeBlock.Padding = new Thickness(10);
                        codeBlock.FontFamily = new WpfFontFamily("Consolas, Courier New, monospace");
                        codeBlock.FontSize = 13;
                    }
                    else
                    {
                        inCodeBlock = false;
                        if (codeBlock != null) { doc.Blocks.Add(codeBlock); codeBlock = null; }
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    if (codeBlock != null)
                    {
                        if (codeBlock.Inlines.Count > 0) codeBlock.Inlines.Add(new LineBreak());
                        codeBlock.Inlines.Add(new Run(line));
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    continue;
                }

                if (trimmedLine.StartsWith("# "))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    var h = new Paragraph(new Run(trimmedLine.Substring(2)) { FontSize = 18, FontWeight = FontWeights.Bold });
                    h.Margin = new Thickness(0, 14, 0, 8);
                    doc.Blocks.Add(h);
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    var h = new Paragraph(new Run(trimmedLine.Substring(3)) { FontSize = 16, FontWeight = FontWeights.Bold });
                    h.Margin = new Thickness(0, 12, 0, 6);
                    doc.Blocks.Add(h);
                }
                else if (trimmedLine.StartsWith("### "))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    var h = new Paragraph(new Run(trimmedLine.Substring(4)) { FontSize = 14, FontWeight = FontWeights.Bold });
                    h.Margin = new Thickness(0, 10, 0, 5);
                    doc.Blocks.Add(h);
                }
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    if (currentParagraph != null) { doc.Blocks.Add(currentParagraph); currentParagraph = null; }
                    var item = new Paragraph();
                    AddInlines(item, "• " + trimmedLine.Substring(2));
                    item.Margin = new Thickness(20, 3, 0, 4);
                    doc.Blocks.Add(item);
                }
                else
                {
                    if (currentParagraph == null)
                        currentParagraph = new Paragraph { Margin = new Thickness(0, 6, 0, 10) };
                    else
                        currentParagraph.Inlines.Add(new LineBreak());
                    AddInlines(currentParagraph, trimmedLine);
                }
            }

            if (currentParagraph != null) doc.Blocks.Add(currentParagraph);
        }

        public static void RenderFallback(FlowDocument doc, string title)
        {
            doc.Blocks.Clear();
            doc.Blocks.Add(new Paragraph(new Run(title) { FontSize = 18, FontWeight = FontWeights.Bold })
                { Margin = new Thickness(0, 0, 0, 10) });
            doc.Blocks.Add(new Paragraph(new Run($"{title} content could not be loaded.")));
        }

        private static void AddInlines(Paragraph paragraph, string text)
        {
            var parts = System.Text.RegularExpressions.Regex.Split(text, @"(\*\*.*?\*\*|`.*?`)");
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    paragraph.Inlines.Add(new Run(part.Substring(2, part.Length - 4)) { FontWeight = FontWeights.Bold });
                }
                else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
                {
                    var r = new Run(part.Substring(1, part.Length - 2))
                        { FontFamily = new WpfFontFamily("Consolas, Courier New, monospace"), FontSize = 12 };
                    if (WpfApplication.Current.TryFindResource("ThemeCodeBlockBackground") is WpfBrush bg)
                        r.Background = bg;
                    if (WpfApplication.Current.TryFindResource("ThemeCodeBlockForeground") is WpfBrush fg)
                        r.Foreground = fg;
                    paragraph.Inlines.Add(r);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(part));
                }
            }
        }
    }
}
