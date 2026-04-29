using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IIniParserService" />
public sealed class IniParserService : IIniParserService
{
    private readonly IFileSystem _fileSystem;

    public IniParserService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public async Task<ZooIniModel> ReadAsync(string iniFilePath)
    {
        var lines = await _fileSystem.File.ReadAllLinesAsync(iniFilePath);
        var document = Tokenize(lines);
        var model = new ZooIniModel { RawDocument = document };

        var knownLookup = ZooIniDefaults.KnownKeys
            .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        foreach (var keyValue in document.Lines.OfType<IniKeyValue>())
        {
            var lookupKey = (keyValue.Section.ToLowerInvariant(), keyValue.Key.ToLowerInvariant());
            if (knownLookup.TryGetValue(lookupKey, out var spec))
            {
                spec.Write(model, keyValue.Value);
            }
            else
            {
                model.UnknownKeys[$"{keyValue.Section}.{keyValue.Key}"] = keyValue.Value;
            }
        }

        return model;
    }

    /// <inheritdoc />
    public async Task WriteAsync(string iniFilePath, ZooIniModel model)
    {
        var document = model.RawDocument ?? BuildFreshDocument(model);
        var updatedLines = MergeModelIntoDocument(document, model);

        var content = new StringBuilder();
        foreach (var line in updatedLines)
            content.AppendLine(RenderLine(line));

        var tempPath = iniFilePath + ".tmp";
        await _fileSystem.File.WriteAllTextAsync(tempPath, content.ToString());
        _fileSystem.File.Move(tempPath, iniFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public ZooIniModel GetDefaults() => new();

    private static IniDocument Tokenize(string[] lines)
    {
        var document = new IniDocument();
        var currentSection = string.Empty;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();

            if (trimmed.Length == 0)
            {
                document.Lines.Add(new IniBlank());
                continue;
            }

            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                document.Lines.Add(new IniComment(raw));
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var name = trimmed[1..^1].Trim();
                currentSection = name;
                document.Lines.Add(new IniSectionHeader(name, raw));
                continue;
            }

            var separator = raw.IndexOf('=');
            if (separator < 0)
            {
                document.Lines.Add(new IniComment(raw));
                continue;
            }

            var key = raw[..separator].Trim();
            var value = raw[(separator + 1)..].TrimEnd();
            document.Lines.Add(new IniKeyValue(currentSection, key, value, raw));
        }

        return document;
    }

    private static IniDocument BuildFreshDocument(ZooIniModel model)
    {
        var document = new IniDocument();

        foreach (var sectionGroup in ZooIniDefaults.KnownKeys.GroupBy(spec => spec.Section, StringComparer.OrdinalIgnoreCase))
        {
            document.Lines.Add(new IniSectionHeader(sectionGroup.Key, $"[{sectionGroup.Key}]"));
            foreach (var spec in sectionGroup)
            {
                var value = spec.Read(model);
                var raw = $"{spec.Key}={value}";
                document.Lines.Add(new IniKeyValue(sectionGroup.Key, spec.Key, value, raw));
            }
            document.Lines.Add(new IniBlank());
        }

        foreach (var (compoundKey, value) in model.UnknownKeys)
        {
            var dot = compoundKey.IndexOf('.');
            if (dot <= 0) continue;

            var section = compoundKey[..dot];
            var key = compoundKey[(dot + 1)..];

            document.Lines.Add(new IniSectionHeader(section, $"[{section}]"));
            document.Lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
            document.Lines.Add(new IniBlank());
        }

        return document;
    }

    private static IReadOnlyList<IniLine> MergeModelIntoDocument(IniDocument document, ZooIniModel model)
    {
        var knownLookup = ZooIniDefaults.KnownKeys
            .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        var emittedKnown = new HashSet<(string Section, string Key)>();
        var emittedUnknown = new HashSet<string>(StringComparer.Ordinal);
        var rewritten = new List<IniLine>(document.Lines.Count);

        foreach (var line in document.Lines)
        {
            if (line is IniKeyValue kv)
            {
                var lookupKey = (kv.Section.ToLowerInvariant(), kv.Key.ToLowerInvariant());
                if (knownLookup.TryGetValue(lookupKey, out var spec))
                {
                    var newValue = spec.Read(model);
                    rewritten.Add(newValue == kv.Value ? kv : kv.WithValue(newValue));
                    emittedKnown.Add((spec.Section, spec.Key));
                    continue;
                }

                var compound = $"{kv.Section}.{kv.Key}";
                if (model.UnknownKeys.TryGetValue(compound, out var unknownValue))
                {
                    rewritten.Add(unknownValue == kv.Value ? kv : kv.WithValue(unknownValue));
                    emittedUnknown.Add(compound);
                    continue;
                }

                continue;
            }

            rewritten.Add(line);
        }

        var missingKnown = ZooIniDefaults.KnownKeys
            .Where(spec => !emittedKnown.Contains((spec.Section, spec.Key)))
            .Select(spec => (spec.Section, spec.Key, spec.Read(model)));
        AppendMissingKeys(rewritten, missingKnown);

        var missingUnknown = model.UnknownKeys
            .Where(pair => !emittedUnknown.Contains(pair.Key))
            .Select(pair =>
            {
                var dot = pair.Key.IndexOf('.');
                return dot > 0
                    ? (Section: pair.Key[..dot], Key: pair.Key[(dot + 1)..], Value: pair.Value)
                    : (Section: string.Empty, Key: pair.Key, Value: pair.Value);
            })
            .Where(triple => triple.Section.Length > 0);
        AppendMissingKeys(rewritten, missingUnknown);

        return rewritten;
    }

    private static void AppendMissingKeys(List<IniLine> lines, IEnumerable<(string Section, string Key, string Value)> missing)
    {
        foreach (var group in missing.GroupBy(triple => triple.Section, StringComparer.OrdinalIgnoreCase))
        {
            var section = group.Key;
            var insertionIndex = FindSectionEnd(lines, section);

            if (insertionIndex < 0)
            {
                if (lines.Count > 0 && lines[^1] is not IniBlank)
                    lines.Add(new IniBlank());
                lines.Add(new IniSectionHeader(section, $"[{section}]"));
                foreach (var (_, key, value) in group)
                    lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
            }
            else
            {
                var keysToInsert = group
                    .Select(triple => (IniLine)new IniKeyValue(section, triple.Key, triple.Value, $"{triple.Key}={triple.Value}"))
                    .ToList();
                lines.InsertRange(insertionIndex, keysToInsert);
            }
        }
    }

    private static int FindSectionEnd(IReadOnlyList<IniLine> lines, string section)
    {
        var headerIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader header && string.Equals(header.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0) return -1;

        for (var i = headerIndex + 1; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader)
                return i;
        }

        return lines.Count;
    }

    private static string RenderLine(IniLine line) => line switch
    {
        IniKeyValue kv => RenderKeyValue(kv),
        IniSectionHeader section => section.RawText,
        IniComment comment => comment.RawText,
        IniBlank => string.Empty,
        _ => string.Empty
    };

    private static string RenderKeyValue(IniKeyValue kv)
    {
        var rawSeparator = kv.RawText.IndexOf('=');
        if (rawSeparator > 0)
        {
            var rawValuePart = kv.RawText[(rawSeparator + 1)..].TrimEnd();
            if (rawValuePart == kv.Value) return kv.RawText;
        }
        return $"{kv.Key}={kv.Value}";
    }
}
