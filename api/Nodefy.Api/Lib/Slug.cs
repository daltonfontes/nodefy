using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nodefy.Api.Lib;

public static class Slug
{
    public static string Generate(string name)
    {
        var normalized = (name ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        var stripped = sb.ToString().ToLowerInvariant();
        var hyphenated = Regex.Replace(stripped, "[^a-z0-9]+", "-");
        var trimmed = hyphenated.Trim('-');
        return trimmed.Length > 50 ? trimmed[..50].TrimEnd('-') : trimmed;
    }
}
