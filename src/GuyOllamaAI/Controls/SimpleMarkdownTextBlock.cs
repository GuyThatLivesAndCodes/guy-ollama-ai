using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GuyOllamaAI.Controls;

/// <summary>
/// A simple markdown renderer that handles basic formatting:
/// - Code blocks (```)
/// - Inline code (`)
/// - Bold (**text** or __text__)
/// - Italic (*text* or _text_)
/// - Headers (# ## ###)
/// </summary>
public class SimpleMarkdownTextBlock : StackPanel
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<SimpleMarkdownTextBlock, string?>(nameof(Markdown));

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<SimpleMarkdownTextBlock, IBrush?>(nameof(TextBrush));

    public static readonly StyledProperty<IBrush?> CodeBackgroundProperty =
        AvaloniaProperty.Register<SimpleMarkdownTextBlock, IBrush?>(nameof(CodeBackground));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public IBrush? CodeBackground
    {
        get => GetValue(CodeBackgroundProperty);
        set => SetValue(CodeBackgroundProperty, value);
    }

    public SimpleMarkdownTextBlock()
    {
        Spacing = 8;
        Orientation = Orientation.Vertical;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MarkdownProperty ||
            change.Property == TextBrushProperty ||
            change.Property == CodeBackgroundProperty)
        {
            RenderMarkdown();
        }
    }

    private void RenderMarkdown()
    {
        Children.Clear();

        var text = Markdown;
        if (string.IsNullOrEmpty(text))
            return;

        var textBrush = TextBrush ?? new SolidColorBrush(Color.Parse("#E2E8F0"));
        var codeBg = CodeBackground ?? new SolidColorBrush(Color.Parse("#1E293B"));

        // Split by code blocks first
        var codeBlockPattern = new Regex(@"```(\w*)\n?([\s\S]*?)```", RegexOptions.Multiline);
        var lastIndex = 0;
        var matches = codeBlockPattern.Matches(text);

        foreach (Match match in matches)
        {
            // Add text before this code block
            if (match.Index > lastIndex)
            {
                var beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                AddFormattedText(beforeText, textBrush);
            }

            // Add the code block
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value.Trim();
            AddCodeBlock(code, language, codeBg);

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last code block
        if (lastIndex < text.Length)
        {
            var remainingText = text.Substring(lastIndex);
            AddFormattedText(remainingText, textBrush);
        }
    }

    private void AddFormattedText(string text, IBrush textBrush)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Split into paragraphs
        var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var para in paragraphs)
        {
            var trimmedPara = para.Trim();
            if (string.IsNullOrEmpty(trimmedPara))
                continue;

            // Check for headers
            if (trimmedPara.StartsWith("### "))
            {
                AddHeader(trimmedPara.Substring(4), 16, textBrush);
            }
            else if (trimmedPara.StartsWith("## "))
            {
                AddHeader(trimmedPara.Substring(3), 18, textBrush);
            }
            else if (trimmedPara.StartsWith("# "))
            {
                AddHeader(trimmedPara.Substring(2), 20, textBrush);
            }
            else
            {
                // Regular paragraph - process inline formatting
                AddInlineFormattedText(trimmedPara, textBrush);
            }
        }
    }

    private void AddHeader(string text, double fontSize, IBrush textBrush)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            Foreground = textBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 4)
        };
        Children.Add(tb);
    }

    private void AddInlineFormattedText(string text, IBrush textBrush)
    {
        // For simplicity, just handle inline code with ` and render as single TextBlock
        // More complex inline formatting would require InlineCollection which is complex in Avalonia

        // Replace inline code with markers we can style differently
        var inlineCodePattern = new Regex(@"`([^`]+)`");

        if (inlineCodePattern.IsMatch(text))
        {
            // Has inline code - create a WrapPanel with mixed content
            var wrapPanel = new WrapPanel();

            var lastIndex = 0;
            foreach (Match match in inlineCodePattern.Matches(text))
            {
                // Add text before
                if (match.Index > lastIndex)
                {
                    var beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                    wrapPanel.Children.Add(CreateTextRun(beforeText, textBrush, false));
                }

                // Add inline code
                var codeText = match.Groups[1].Value;
                wrapPanel.Children.Add(CreateInlineCode(codeText));

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                wrapPanel.Children.Add(CreateTextRun(text.Substring(lastIndex), textBrush, false));
            }

            Children.Add(wrapPanel);
        }
        else
        {
            // No inline code - just add as TextBlock
            var tb = new TextBlock
            {
                Text = ProcessBoldItalic(text),
                Foreground = textBrush,
                TextWrapping = TextWrapping.Wrap
            };
            Children.Add(tb);
        }
    }

    private TextBlock CreateTextRun(string text, IBrush textBrush, bool isCode)
    {
        return new TextBlock
        {
            Text = ProcessBoldItalic(text),
            Foreground = textBrush,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private Border CreateInlineCode(string code)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#374151")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2),
            Margin = new Thickness(2, 0),
            Child = new TextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#F472B6"))
            }
        };
    }

    private string ProcessBoldItalic(string text)
    {
        // For now, just strip the markdown markers
        // Full implementation would need Inlines support
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = Regex.Replace(text, @"__(.+?)__", "$1");
        text = Regex.Replace(text, @"\*(.+?)\*", "$1");
        text = Regex.Replace(text, @"_(.+?)_", "$1");
        return text;
    }

    private void AddCodeBlock(string code, string language, IBrush codeBg)
    {
        var border = new Border
        {
            Background = codeBg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4)
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            MaxHeight = 400
        };

        var codeBlock = new SelectableTextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#E2E8F0")),
            TextWrapping = TextWrapping.NoWrap
        };

        scrollViewer.Content = codeBlock;
        border.Child = scrollViewer;

        // Add language label if specified
        if (!string.IsNullOrEmpty(language))
        {
            var container = new StackPanel { Spacing = 4 };
            var languageLabel = new TextBlock
            {
                Text = language.ToUpperInvariant(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                Margin = new Thickness(4, 0, 0, 0)
            };
            container.Children.Add(languageLabel);
            container.Children.Add(border);
            Children.Add(container);
        }
        else
        {
            Children.Add(border);
        }
    }
}
