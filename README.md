# XamlIconImporter

A dependency-free .NET 10 CLI for importing standalone WPF vector XAML files into a `ResourceDictionary`.

Supported input roots:

- `Viewbox`, including the common `Viewbox` ? `Canvas` structure
- `Canvas`
- `DrawingImage`
- `DrawingGroup`

The source visual tree and attributes are preserved. The importer adds `x:Key` using the source filename.

## Requirements

.NET 10 SDK.

## Build

```powershell
dotnet build .\src\XamlIconImporter\XamlIconImporter.csproj
```

## Run without installing

```powershell
dotnet run --project .\src\XamlIconImporter -- --input "samples/Icons/**/*.xaml" --output "Resources/Icons.xaml"
```

## Examples

Create a dictionary:

```powershell
xaml-icon-importer --input "Assets/Icons/**/*.xaml" --output "Resources/Icons.xaml"
```

Merge into an existing dictionary:

```powershell
xaml-icon-importer --input "Assets/Icons/*.xaml" --dictionary "Resources/Icons.xaml"
```

Replace existing keys:

```powershell
xaml-icon-importer --input "Assets/Icons/**/*.xaml" --dictionary "Resources/Icons.xaml" --overwrite
```

Preview without writing:

```powershell
xaml-icon-importer --input "Assets/Icons/**/*.xaml" --output "Resources/Icons.xaml" --dry-run
```

Prefix keys:

```powershell
xaml-icon-importer --input "Assets/Icons/*.xaml" --output "Resources/Icons.xaml" --prefix "Icon"
```

A file named `Home.xaml` generates key `Home`; with `--prefix Icon`, it generates `IconHome`.

## Glob syntax

Patterns are resolved from the current working directory unless absolute.

- `*` matches within one path segment.
- `?` matches one character within one path segment.
- `**` matches across directory segments. `**/` also matches zero directory segments.

Examples: `Assets/Icons/*.xaml`, `Assets/Icons/**/*.xaml`, `Assets/**/Navigation/*.xaml`.

`--recursive` makes a pattern without `**` search subdirectories too. A directory supplied as `--input` imports `.xaml` files directly within it; add `--recursive` to include descendants.

## Duplicate keys

Existing keys are skipped unless `--overwrite` is specified. Duplicate filenames in different directories therefore collide because keys default to the filename alone.

## Safety and limitations

- Existing targets must have a WPF `ResourceDictionary` root.
- Imported roots must use the WPF presentation namespace.
- XML parsing prohibits DTD processing and external resolution.
- The tool manipulates XML; it does not instantiate or execute XAML and cannot guarantee every resource will load successfully in WPF.
- Existing dictionary formatting is normalized when written.
- The tool does not add the dictionary to `App.xaml` or modify the project file.
- Changes are serialized to a temporary file before replacing the target.

## Install as a global .NET tool

```powershell
dotnet pack .\src\XamlIconImporter\XamlIconImporter.csproj -c Release
dotnet tool install --global --add-source .\nupkg XamlIconImporter
```

Invoke it using `xaml-icon-importer`.
