#!/usr/bin/env dotnet run
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

var directory = args.Length > 0 ? args[0] : "./RxBlazorV2.GeneratorTests";
var dryRun = args.Contains("--dry-run");

Console.WriteLine($"Scanning directory: {directory}");
Console.WriteLine($"Mode: {(dryRun ? "DRY RUN" : "FIXING")}");
Console.WriteLine();

var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
var totalFixed = 0;
var filesChanged = 0;

foreach (var file in csFiles)
{
    var content = File.ReadAllText(file);
    var originalContent = content;
    var lines = content.Split('\n');
    var result = new StringBuilder();
    var inRawString = false;
    var rawStringIndent = 0;
    var fixedInFile = 0;

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];

        // Detect raw string start: var xxx = """ or var xxx = $$"""
        if (!inRawString && (line.Contains("= \"\"\"") || line.Contains("= $$\"\"\"")))
        {
            inRawString = true;
            // Find the indentation of the variable declaration
            rawStringIndent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
            result.Append(line);
            if (i < lines.Length - 1) result.Append('\n');
            continue;
        }

        // Detect raw string end
        if (inRawString && line.TrimStart().StartsWith("\"\"\";"))
        {
            inRawString = false;
            result.Append(line);
            if (i < lines.Length - 1) result.Append('\n');
            continue;
        }

        // Inside raw string - remove all leading whitespace
        if (inRawString)
        {
            var trimmedLine = line.TrimStart();

            // Keep empty lines as-is
            if (string.IsNullOrWhiteSpace(line))
            {
                result.Append(line);
            }
            else
            {
                // Remove all indentation - content should start at column 0
                result.Append(trimmedLine);
                if (line != trimmedLine)
                {
                    fixedInFile++;
                }
            }

            if (i < lines.Length - 1) result.Append('\n');
            continue;
        }

        // Normal line - keep as-is
        result.Append(line);
        if (i < lines.Length - 1) result.Append('\n');
    }

    var newContent = result.ToString();

    if (newContent != originalContent)
    {
        filesChanged++;
        totalFixed += fixedInFile;

        Console.WriteLine($"Fixed: {Path.GetFileName(file)} ({fixedInFile} lines)");

        if (!dryRun)
        {
            File.WriteAllText(file, newContent);
        }
    }
}

Console.WriteLine();
Console.WriteLine($"Summary: {filesChanged} files, {totalFixed} lines fixed");
if (dryRun)
{
    Console.WriteLine("Run without --dry-run to apply changes");
}