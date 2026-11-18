namespace XamlIconImporter;

internal static class CommandLine
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            var options = Parse(args);
            var result = await new IconImporter().ImportAsync(options);

            foreach (var item in result.Items)
            {
                var status = item.Status switch
                {
                    ImportStatus.Added => "ADD",
                    ImportStatus.Replaced => "REPLACE",
                    ImportStatus.Unchanged => "SKIP",
                    ImportStatus.Error => "ERROR",
                    _ => "INFO"
                };
                Console.WriteLine($"[{status}] {item.Source}: {item.Message}");
            }

            Console.WriteLine();
            Console.WriteLine($"Matched: {result.Matched}, added: {result.Added}, replaced: {result.Replaced}, skipped: {result.Skipped}, errors: {result.Errors}");

            if (!options.DryRun && result.Errors == 0 && result.Changed)
                Console.WriteLine($"Written: {Path.GetFullPath(options.TargetPath)}");
            else if (options.DryRun)
                Console.WriteLine("Dry run: no files were written.");

            return result.Errors > 0 ? 1 : 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Console.Error.WriteLine("Run 'xaml-icon-importer --help' for usage.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static ImportOptions Parse(string[] args)
    {
        string? input = null;
        string? output = null;
        string? dictionary = null;
        string? prefix = null;
        var overwrite = false;
        var dryRun = false;
        var recursive = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            string Value()
            {
                if (++i >= args.Length)
                    throw new ArgumentException($"Missing value for {arg}.");
                return args[i];
            }

            switch (arg)
            {
                case "--input":
                case "-i":
                    input = Value();
                    break;
                case "--output":
                case "-o":
                    output = Value();
                    break;
                case "--dictionary":
                case "-d":
                    dictionary = Value();
                    break;
                case "--prefix":
                    prefix = Value();
                    break;
                case "--overwrite":
                    overwrite = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--recursive":
                    recursive = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("--input is required.");

        if ((string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(dictionary)) ||
            (!string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(dictionary)))
            throw new ArgumentException("Specify exactly one of --output or --dictionary.");

        var target = output ?? dictionary!;
        if (!target.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The target dictionary must have a .xaml extension.");

        return new ImportOptions(input!, target, overwrite, dryRun, recursive, prefix ?? "");
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
@"XamlIconImporter - import standalone WPF vector XAML files into a ResourceDictionary.

Usage:
  xaml-icon-importer --input <glob> --output <new-dictionary.xaml> [options]
  xaml-icon-importer --input <glob> --dictionary <existing-dictionary.xaml> [options]

Options:
  -i, --input       Input glob. Relative patterns are resolved from the current directory.
  -o, --output      Create a new ResourceDictionary at this path.
  -d, --dictionary  Add resources to an existing ResourceDictionary.
      --prefix      Prefix added to generated keys.
      --overwrite   Replace resources with matching x:Key values.
      --recursive   Recurse into subdirectories when the pattern has no ** segment.
      --dry-run     Report planned changes without writing.
  -h, --help        Show this help.

Supported root elements: Viewbox, Canvas, DrawingImage, DrawingGroup.
Resource keys default to the input filename without its .xaml extension.
Glob syntax supports *, ?, and ** (for example Icons/**/*.xaml).");
    }
}
