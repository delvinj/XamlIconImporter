using System.Text;
using System.Text.RegularExpressions;

namespace XamlIconImporter;

internal static class GlobResolver
{
    public static IReadOnlyList<string> Resolve(string pattern, bool recursive)
    {
        var normalized = pattern.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var absolute = Path.IsPathRooted(normalized)
            ? Path.GetFullPath(normalized)
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, normalized));

        var wildcardAt = absolute.IndexOfAny(['*', '?']);
        if (wildcardAt < 0)
        {
            if (File.Exists(absolute))
                return [absolute];

            if (Directory.Exists(absolute))
                return Directory.EnumerateFiles(
                        absolute,
                        "*.xaml",
                        recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            return [];
        }

        var separator = absolute.LastIndexOf(Path.DirectorySeparatorChar, wildcardAt);
        var root = separator <= 0
            ? Path.GetPathRoot(absolute)!
            : absolute[..separator];
        var remainder = absolute[(separator + 1)..];

        if (!Directory.Exists(root))
            return [];

        var regex = new Regex(
            "^" + GlobToRegex(remainder) + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var searchOption = recursive || remainder.Contains("**", StringComparison.Ordinal)
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        return Directory.EnumerateFiles(root, "*", searchOption)
            .Where(path => regex.IsMatch(
                Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')))
            .Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GlobToRegex(string glob)
    {
        var normalized = glob.Replace('\\', '/');
        var builder = new StringBuilder();

        for (var i = 0; i < normalized.Length; i++)
        {
            var character = normalized[i];

            if (character == '*')
            {
                if (i + 1 < normalized.Length && normalized[i + 1] == '*')
                {
                    i++;
                    if (i + 1 < normalized.Length && normalized[i + 1] == '/')
                    {
                        i++;
                        builder.Append("(?:.*/)?");
                    }
                    else
                    {
                        builder.Append(".*");
                    }
                }
                else
                {
                    builder.Append("[^/]*");
                }
            }
            else if (character == '?')
            {
                builder.Append("[^/]");
            }
            else
            {
                builder.Append(Regex.Escape(character.ToString()));
            }
        }

        return builder.ToString();
    }
}
