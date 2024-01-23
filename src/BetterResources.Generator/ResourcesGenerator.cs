using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BetterResources;

[Generator]
public class ResourcesGenerator : ISourceGenerator
{
    private const string Namespace = nameof(ResourcesGenerator);

    private const char Separator = ';';

    public void Initialize(GeneratorInitializationContext context)
    {
        // Nothing required
    }

    public void Execute(GeneratorExecutionContext context)
    {
        foreach (var file in context.AdditionalFiles)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var fileName = Path.GetFileName(file.Path);

            var isResourceFile = Path.GetExtension(fileName) is ".csv" && fileName.Contains("Resources");
            if (isResourceFile is false)
            {
                continue;
            }

            var lines = file.GetText(context.CancellationToken)?.Lines;
            if (lines is null)
            {
                continue;
            }

            List<string> resourceNames = [];
            var header = lines[0].ToString().Split(Separator);

            const string nameIdentifier = "Name";
            const string defaultCultureIdentifier = "";

            var nameIndex = Array.IndexOf(header, nameIdentifier);
            var defaultIndex = Array.IndexOf(header, defaultCultureIdentifier);

            var cultureIndices = header
                .Select((culture, index) => (culture, index))
                .Where(x => x.culture is not nameIdentifier and not defaultCultureIdentifier)
                .ToDictionary(x => x.index, x => x.culture);

            var className = Path.GetFileNameWithoutExtension(fileName);
            var classHeader =
                $$$"""
                  #nullable enable
                  
                  using System;
                  using System.Threading;
                  using System.Globalization;
                  
                  namespace {{{Namespace}}};
                    
                  public static class {{{className}}}
                  {
                  
                  """;

            var fileBuilder = new StringBuilder(classHeader);
            foreach (var resourceLine in lines.Skip(1))
            {
                var row = resourceLine.ToString();
                if (row.StartsWith("#") || string.IsNullOrWhiteSpace(row))
                {
                    continue;
                }
                var resourceRow = row.Split(Separator);

                var resourceName = resourceRow[nameIndex];
                resourceNames.Add(resourceName);

                var defaultResource = resourceRow[defaultIndex];
                var cultureResources = resourceRow
                    .Select((resource, index) => (resource, index))
                    .Where(x => cultureIndices.ContainsKey(x.index))
                    .Select(x => (culture: cultureIndices[x.index], x.resource));

                var method = $$"""
                                    /// <summary>
                                    /// A resource with default value
                                    /// <code>{{defaultResource}}</code>
                                    /// </summary>
                                    public static string {{resourceName}}(string culture) => culture switch
                                    {{{string.Concat(cultureResources.Select(x => $"\n\t\t\"{x.culture}\" => \"{x.resource}\","))}}
                                        _ => "{{defaultResource}}"
                                    };
                                    /// <inheritdoc cref="{{resourceName}}(string)"/>
                                    public static string {{resourceName}}(CultureInfo? culture = null)
                                        => {{resourceName}}((culture ?? Thread.CurrentThread.CurrentCulture).TwoLetterISOLanguageName);
                                
                                
                                """;

                fileBuilder.Append(method);
            }

            var defaultResourceClass = $$"""
                                         
                                             /// <summary>
                                             /// Allows quick accessing resources of {{className}} resources for current culture.
                                             /// </summary>
                                             public static class Default
                                             {
                                         {{string.Join("\n", resourceNames.Select(x => $"\t\tpublic static string {x} => {x}();"))}}
                                             }
                                         """;
            fileBuilder.Append(defaultResourceClass);

            var findMethod = $$"""
                                        
                                        /// <summary>
                                        /// Allows accessing resources in this class dynamically.
                                        /// </summary>
                                        public static Func<CultureInfo?, string> Find(string resourceName) => resourceName switch
                                        {
                                    {{string.Join("\n", resourceNames.Select(x => $"\t\t\"{x}\" => {x},"))}}
                                            _ => throw new ArgumentException($"Resource with name {resourceName} not found")
                                        };
                                        
                                        /// <summary>
                                        /// Allows accessing resources in this class dynamically.
                                        /// </summary>
                                        public static string Find(string resourceName, CultureInfo? culture = null)
                                            => Find(resourceName).Invoke(culture);
                                    """;
            fileBuilder.Append(findMethod);
            fileBuilder.Append("\n}");


            context.AddSource($"{className}.g.cs", SourceText.From(fileBuilder.ToString(), Encoding.UTF8));
        }
    }
}