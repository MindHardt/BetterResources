# BetterResources

## What is it?

It is a simple and intuitive alternative to .NET .resx resources.
This library generates human-readable .g.cs files from your .csv resources.

## Why do I want it?

1. Default .resx files make it hard to use them as `<Func<CultureInfo, string>>`,
   which is a common and wanted feature. With this library you can directly access
   your resources as methods and properties.
2. Generated files are easily readable, you can view code paths yourself.
3. Resources are generated from .csv files which are human-readable unlike
   .resx files, which are xml. They are editable with tools like excel.

## How do I use it?

Reference source generator project as an analyzer, like any other SourceGen.
It will process files that meet the following criteria:
1. Their build action is set to `Additional Files` 
   (`<AdditionalFiles Include="Resources.csv" />` in .csproj).
2. Their extension is `.csv`
3. They have `Resources` in their name (case-sensitive).

For the generator to work correctly, files must also use `;` as a separator and not have a resource named `Default`.

Tips:
* First column is reserved for resource name
* Second column is reserved for default culture value
* Other columns must have a header equal to desired `CultureInfo.TwoLetterISOLanguageName` (`en`, `ru`, `uz`...)
* All row should have the same amount of cells.
* Generated classes are static and are in `ResourcesGenerator` namespace.