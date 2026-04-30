using System.Globalization;
using System.Text;

namespace LearnToCode.API.Services;

public static class TheoryTopicKey
{
    public static string FromTitle(string title)
    {
        var normalized = title.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousWasDash = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'a' and <= 'z' || character is >= '0' and <= '9')
            {
                builder.Append(character);
                previousWasDash = false;
                continue;
            }

            if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var key = builder.ToString().Trim('-');
        if (key.Length > 80)
        {
            key = key[..80].Trim('-');
        }

        return string.IsNullOrWhiteSpace(key) ? "tema" : key;
    }

    public static string Normalize(string topicId) => topicId.Trim().ToLowerInvariant();

    public static bool Matches(string title, string topicId) =>
        string.Equals(FromTitle(title), Normalize(topicId), StringComparison.OrdinalIgnoreCase);
}
