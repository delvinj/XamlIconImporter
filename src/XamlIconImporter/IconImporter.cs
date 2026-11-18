using System.Xml.Linq;

namespace XamlIconImporter;

internal sealed class IconImporter
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly HashSet<string> SupportedRoots =
        new(StringComparer.Ordinal)
        {
            "Viewbox",
            "Canvas",
            "DrawingImage",
            "DrawingGroup"
        };

    public async Task<ImportResult> ImportAsync(ImportOptions options)
    {
        var sources = GlobResolver.Resolve(options.InputPattern, options.Recursive);
        if (sources.Count == 0)
            throw new ArgumentException($"No .xaml files matched '{options.InputPattern}'.");

        var targetPath = Path.GetFullPath(options.TargetPath);

        XDocument dictionary;
        if (File.Exists(targetPath))
        {
            dictionary = SafeXamlReader.Load(targetPath);
        }
        else
        {
            dictionary = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    Presentation + "ResourceDictionary",
                    new XAttribute(XNamespace.Xmlns + "x", Xaml)));
        }

        if (dictionary.Root?.Name != Presentation + "ResourceDictionary")
            throw new InvalidDataException(
                $"'{targetPath}' does not have a WPF ResourceDictionary root.");

        var root = dictionary.Root;
        var resources = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var element in root.Elements())
        {
            var existingKey = (string?)element.Attribute(Xaml + "Key");
            if (existingKey is null)
                continue;

            if (!resources.TryAdd(existingKey, element))
                throw new InvalidDataException(
                    $"The target dictionary already contains duplicate x:Key '{existingKey}'.");
        }

        var result = new ImportResult();

        foreach (var source in sources)
        {
            try
            {
                var iconDocument = SafeXamlReader.Load(source);
                var iconRoot = iconDocument.Root
                    ?? throw new InvalidDataException("The XAML document has no root element.");

                if (iconRoot.Name.Namespace != Presentation ||
                    !SupportedRoots.Contains(iconRoot.Name.LocalName))
                {
                    throw new InvalidDataException(
                        $"Unsupported root '{iconRoot.Name}'. Expected Viewbox, Canvas, DrawingImage, or DrawingGroup in the WPF presentation namespace.");
                }

                var key = options.KeyPrefix + Path.GetFileNameWithoutExtension(source);
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidDataException("Generated resource key is empty.");

                var importedElement = new XElement(iconRoot);
                importedElement.SetAttributeValue(Xaml + "Key", key);

                if (resources.TryGetValue(key, out var existingElement))
                {
                    if (!options.Overwrite)
                    {
                        result.Items.Add(new ImportItem(
                            source,
                            key,
                            ImportStatus.Unchanged,
                            "A resource with this key already exists; use --overwrite to replace it."));
                        continue;
                    }

                    if (!options.DryRun)
                        existingElement.ReplaceWith(importedElement);

                    resources[key] = importedElement;
                    result.Items.Add(new ImportItem(
                        source,
                        key,
                        ImportStatus.Replaced,
                        "Existing resource will be replaced."));
                }
                else
                {
                    if (!options.DryRun)
                        root.Add(importedElement);

                    resources.Add(key, importedElement);
                    result.Items.Add(new ImportItem(
                        source,
                        key,
                        ImportStatus.Added,
                        "Resource will be added."));
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                result.Items.Add(new ImportItem(
                    source,
                    Path.GetFileNameWithoutExtension(source),
                    ImportStatus.Error,
                    ex.Message));
            }
        }

        if (result.Changed && !options.DryRun)
        {
            var targetDirectory = Path.GetDirectoryName(targetPath)!;
            Directory.CreateDirectory(targetDirectory);
            await WriteAtomicallyAsync(dictionary, targetPath);
        }

        return result;
    }

    private static async Task WriteAtomicallyAsync(XDocument document, string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath)!;
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var settings = new System.Xml.XmlWriterSettings
            {
                Async = true,
                Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                IndentChars = "    ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = System.Xml.NewLineHandling.Replace
            };

            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            using (var writer = System.Xml.XmlWriter.Create(stream, settings))
            {
                document.Save(writer);
            }

            if (File.Exists(targetPath))
                File.Move(temporaryPath, targetPath, overwrite: true);
            else
                File.Move(temporaryPath, targetPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
