namespace KustoSearchApp;

/// <summary>
/// Provides fuzzy matching functionality for filtering table names.
/// Supports subsequence matching where characters must appear in order but not consecutively.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Performs fuzzy matching on the text against the pattern.
    /// </summary>
    /// <param name="text">The text to search in (e.g., table name)</param>
    /// <param name="pattern">The pattern to match (e.g., user input)</param>
    /// <returns>A tuple containing: IsMatch, list of matched character indices, and a score</returns>
    public static (bool IsMatch, List<int> MatchedIndices, int Score) FuzzyMatch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return (true, new List<int>(), 0);

        if (string.IsNullOrEmpty(text))
            return (false, new List<int>(), 0);

        var matchedIndices = new List<int>();
        int score = 0;
        int patternIndex = 0;
        int lastMatchIndex = -1;
        int consecutiveBonus = 0;

        string textLower = text.ToLower();
        string patternLower = pattern.ToLower();

        for (int i = 0; i < text.Length && patternIndex < pattern.Length; i++)
        {
            if (textLower[i] == patternLower[patternIndex])
            {
                matchedIndices.Add(i);

                // Base score for each match
                score += 1;

                // Bonus for consecutive matches
                if (lastMatchIndex == i - 1)
                {
                    consecutiveBonus++;
                    score += consecutiveBonus * 2;
                }
                else
                {
                    consecutiveBonus = 0;
                }

                // Bonus for matching at word boundaries (start of text or after underscore/dash/dot)
                if (i == 0 || IsWordBoundary(text[i - 1]))
                {
                    score += 10;
                }

                // Bonus for exact case match
                if (text[i] == pattern[patternIndex])
                {
                    score += 1;
                }

                lastMatchIndex = i;
                patternIndex++;
            }
        }

        // Check if all pattern characters were matched
        bool isMatch = patternIndex == pattern.Length;

        // Additional bonus for shorter matches (prefer exact or near-exact matches)
        if (isMatch)
        {
            int matchSpan = matchedIndices.Count > 0 
                ? matchedIndices[^1] - matchedIndices[0] + 1 
                : 0;
            
            // Bonus for compact matches
            if (matchSpan == pattern.Length)
            {
                score += 50; // Exact substring match bonus
            }
            else
            {
                score += Math.Max(0, 20 - (matchSpan - pattern.Length));
            }

            // Bonus if match starts at beginning
            if (matchedIndices.Count > 0 && matchedIndices[0] == 0)
            {
                score += 15;
            }
        }

        return (isMatch, matchedIndices, isMatch ? score : 0);
    }

    /// <summary>
    /// Checks if a character is a word boundary.
    /// </summary>
    private static bool IsWordBoundary(char c)
    {
        return c == '_' || c == '-' || c == '.' || c == ' ' || char.IsUpper(c);
    }

    /// <summary>
    /// Simple contains check for backward compatibility.
    /// </summary>
    public static bool Contains(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;
        
        if (string.IsNullOrEmpty(text))
            return false;

        return text.ToLower().Contains(pattern.ToLower());
    }
}
