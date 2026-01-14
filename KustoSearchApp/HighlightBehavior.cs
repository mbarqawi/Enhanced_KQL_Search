using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KustoSearchApp;

/// <summary>
/// Attached behavior for highlighting matched characters in a TextBlock.
/// </summary>
public static class HighlightBehavior
{
    #region SourceText Property

    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.RegisterAttached(
            "SourceText",
            typeof(string),
            typeof(HighlightBehavior),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static string GetSourceText(DependencyObject obj) =>
        (string)obj.GetValue(SourceTextProperty);

    public static void SetSourceText(DependencyObject obj, string value) =>
        obj.SetValue(SourceTextProperty, value);

    #endregion

    #region HighlightPattern Property

    public static readonly DependencyProperty HighlightPatternProperty =
        DependencyProperty.RegisterAttached(
            "HighlightPattern",
            typeof(string),
            typeof(HighlightBehavior),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static string GetHighlightPattern(DependencyObject obj) =>
        (string)obj.GetValue(HighlightPatternProperty);

    public static void SetHighlightPattern(DependencyObject obj, string value) =>
        obj.SetValue(HighlightPatternProperty, value);

    #endregion

    #region HighlightBrush Property

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.RegisterAttached(
            "HighlightBrush",
            typeof(Brush),
            typeof(HighlightBehavior),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(250, 204, 21)), OnTextChanged)); // Yellow

    public static Brush GetHighlightBrush(DependencyObject obj) =>
        (Brush)obj.GetValue(HighlightBrushProperty);

    public static void SetHighlightBrush(DependencyObject obj, Brush value) =>
        obj.SetValue(HighlightBrushProperty, value);

    #endregion

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        string sourceText = GetSourceText(textBlock);
        string pattern = GetHighlightPattern(textBlock);

        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(sourceText))
            return;

        if (string.IsNullOrEmpty(pattern))
        {
            textBlock.Inlines.Add(new Run(sourceText));
            return;
        }

        // Get fuzzy match results
        var (isMatch, matchedIndices, _) = FuzzyMatcher.FuzzyMatch(sourceText, pattern);

        if (!isMatch || matchedIndices.Count == 0)
        {
            textBlock.Inlines.Add(new Run(sourceText));
            return;
        }

        Brush highlightBrush = GetHighlightBrush(textBlock);
        var matchSet = new HashSet<int>(matchedIndices);

        // Build the highlighted text
        int currentIndex = 0;
        while (currentIndex < sourceText.Length)
        {
            if (matchSet.Contains(currentIndex))
            {
                // Find consecutive highlighted characters
                int start = currentIndex;
                while (currentIndex < sourceText.Length && matchSet.Contains(currentIndex))
                {
                    currentIndex++;
                }

                // Add highlighted run
                var highlightedRun = new Run(sourceText.Substring(start, currentIndex - start))
                {
                    Background = highlightBrush,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.SemiBold
                };
                textBlock.Inlines.Add(highlightedRun);
            }
            else
            {
                // Find consecutive non-highlighted characters
                int start = currentIndex;
                while (currentIndex < sourceText.Length && !matchSet.Contains(currentIndex))
                {
                    currentIndex++;
                }

                // Add normal run
                textBlock.Inlines.Add(new Run(sourceText.Substring(start, currentIndex - start)));
            }
        }
    }
}
