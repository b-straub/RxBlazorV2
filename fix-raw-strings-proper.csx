#!/usr/bin/env dotnet run
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

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
    var rawStringLines = new List<string>();
    var fixedInFile = 0;

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];

        // Detect raw string start: var xxx = """ or var xxx = $$"""
        if (!inRawString && (line.Contains("= \"\"\"") || line.Contains("= $$\"\"\"")))
        {
            inRawString = true;
            rawStringLines.Clear();
            result.Append(line);
            if (i < lines.Length - 1) result.Append('\n');
            continue;
        }

        // Detect raw string end
        if (inRawString && line.TrimStart().StartsWith("\"\"\";"))
        {
            inRawString = false;

            // Process the collected raw string lines
            var processed = ProcessRawStringContent(rawStringLines);
            foreach (var processedLine in processed)
            {
                result.Append(processedLine);
                result.Append('\n');
            }

            if (processed.Count != rawStringLines.Count ||
                !processed.SequenceEqual(rawStringLines))
            {
                fixedInFile += rawStringLines.Count;
            }

            result.Append(line);
            if (i < lines.Length - 1) result.Append('\n');
            continue;
        }

        // Inside raw string - collect lines
        if (inRawString)
        {
            rawStringLines.Add(line);
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

static List<string> ProcessRawStringContent(List<string> lines)
{
    if (lines.Count == 0) return lines;

    // Find the minimum indentation (ignoring empty lines)
    int minIndent = int.MaxValue;
    foreach (var line in lines)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;

        int indent = 0;
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t') indent++;
            else break;
        }
        minIndent = Math.Min(minIndent, indent);
    }

    if (minIndent == int.MaxValue) minIndent = 0;

    // Remove the common indentation prefix and add base indentation of 2 tabs
    var result = new List<string>();
    const string baseIndent = "\t\t"; // 2 tabs

    foreach (var line in lines)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            // Keep empty lines as-is
            result.Add(line);
        }
        else
        {
            // Remove common prefix and add base indentation
            var trimmed = line.Length > minIndent ? line.Substring(minIndent) : line.TrimStart();
            result.Add(baseIndent + trimmed);
        }
    }

    return result;
}
