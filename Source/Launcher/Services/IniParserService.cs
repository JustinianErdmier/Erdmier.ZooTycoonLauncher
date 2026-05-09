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
        string[]    lines    = await _fileSystem.File.ReadAllLinesAsync(iniFilePath);
        IniDocument document = Tokenize(lines);

        ZooIniModel model = new()
        {
            RawDocument = document
        };

        Dictionary<(string, string), IniKeySpec> knownLookup = ZooIniDefaults.KnownKeys
                                                                             .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        foreach (IniKeyValue keyValue in document.Lines.OfType<IniKeyValue>())
        {
            (string, string) lookupKey = (keyValue.Section.ToLowerInvariant(), keyValue.Key.ToLowerInvariant());

            if (knownLookup.TryGetValue(lookupKey, out IniKeySpec? spec))
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
        IniDocument            document     = model.RawDocument ?? BuildFreshDocument(model);
        IReadOnlyList<IniLine> updatedLines = MergeModelIntoDocument(document, model);

        StringBuilder content = new();

        foreach (IniLine line in updatedLines)
        {
            content.AppendLine(RenderLine(line));
        }

        string tempPath = iniFilePath + ".tmp";
        await _fileSystem.File.WriteAllTextAsync(tempPath, content.ToString());
        _fileSystem.File.Move(tempPath, iniFilePath, overwrite: true);
    }

    private static IniDocument Tokenize(string[] lines)
    {
        IniDocument document       = new();
        string      currentSection = string.Empty;

        foreach (string raw in lines)
        {
            string trimmed = raw.Trim();

            if (trimmed.Length == 0)
            {
                document.Lines.Add(new IniBlank());

                continue;
            }

            if (trimmed.StartsWith(value: ';')
                || trimmed.StartsWith(value: '#'))
            {
                document.Lines.Add(new IniComment(raw));

                continue;
            }

            if (trimmed.StartsWith(value: '[')
                && trimmed.EndsWith(value: ']'))
            {
                string name = trimmed[1..^1]
                    .Trim();

                currentSection = name;
                document.Lines.Add(new IniSectionHeader(name, raw));

                continue;
            }

            int separator = raw.IndexOf(value: '=');

            if (separator < 0)
            {
                document.Lines.Add(new IniComment(raw));

                continue;
            }

            string key = raw[..separator]
                .Trim();

            string value = raw[(separator + 1)..]
                .TrimEnd();

            document.Lines.Add(new IniKeyValue(currentSection, key, value, raw));
        }

        return document;
    }

    private static IniDocument BuildFreshDocument(ZooIniModel model)
    {
        IniDocument document = new();

        foreach (IGrouping<string, IniKeySpec> sectionGroup in ZooIniDefaults.KnownKeys.GroupBy(spec => spec.Section, StringComparer.OrdinalIgnoreCase))
        {
            document.Lines.Add(new IniSectionHeader(sectionGroup.Key, $"[{sectionGroup.Key}]"));

            foreach (IniKeySpec spec in sectionGroup)
            {
                string value = spec.Read(model);
                string raw   = $"{spec.Key}={value}";
                document.Lines.Add(new IniKeyValue(sectionGroup.Key, spec.Key, value, raw));
            }

            document.Lines.Add(new IniBlank());
        }

        foreach ((string compoundKey, string value) in model.UnknownKeys)
        {
            int dot = compoundKey.IndexOf(value: '.');

            if (dot <= 0)
            {
                continue;
            }

            string section = compoundKey[..dot];
            string key     = compoundKey[(dot + 1)..];

            document.Lines.Add(new IniSectionHeader(section, $"[{section}]"));
            document.Lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
            document.Lines.Add(new IniBlank());
        }

        return document;
    }

    private static IReadOnlyList<IniLine> MergeModelIntoDocument(IniDocument document, ZooIniModel model)
    {
        Dictionary<(string, string), IniKeySpec> knownLookup = ZooIniDefaults.KnownKeys
                                                                             .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        HashSet<(string Section, string Key)> emittedKnown   = new();
        HashSet<string>                       emittedUnknown = new(StringComparer.Ordinal);
        List<IniLine>                         rewritten      = new(document.Lines.Count);

        foreach (IniLine line in document.Lines)
        {
            if (line is IniKeyValue kv)
            {
                (string, string) lookupKey = (kv.Section.ToLowerInvariant(), kv.Key.ToLowerInvariant());

                if (knownLookup.TryGetValue(lookupKey, out IniKeySpec? spec))
                {
                    string newValue = spec.Read(model);
                    rewritten.Add(newValue == kv.Value ? kv : kv.WithValue(newValue));
                    emittedKnown.Add((spec.Section, spec.Key));

                    continue;
                }

                string compound = $"{kv.Section}.{kv.Key}";

                if (model.UnknownKeys.TryGetValue(compound, out string? unknownValue))
                {
                    rewritten.Add(unknownValue == kv.Value ? kv : kv.WithValue(unknownValue));
                    emittedUnknown.Add(compound);
                }

                continue;
            }

            rewritten.Add(line);
        }

        IEnumerable<(string Section, string Key, string)> missingKnown = ZooIniDefaults.KnownKeys
                                                                                       .Where(spec => !emittedKnown.Contains((spec.Section, spec.Key)))
                                                                                       .Select(spec => (spec.Section, spec.Key, spec.Read(model)));

        AppendMissingKeys(rewritten, missingKnown);

        IEnumerable<(string Section, string Key, string Value)> missingUnknown = model.UnknownKeys
                                                                                      .Where(pair => !emittedUnknown.Contains(pair.Key))
                                                                                      .Select(pair =>
                                                                                      {
                                                                                          int dot = pair.Key.IndexOf(value: '.');

                                                                                          return dot > 0
                                                                                                     ? (Section: pair.Key[..dot], Key: pair.Key[(dot + 1)..], pair.Value)
                                                                                                     : (Section: string.Empty, pair.Key, pair.Value);
                                                                                      })
                                                                                      .Where(triple => triple.Section.Length > 0);

        AppendMissingKeys(rewritten, missingUnknown);

        return rewritten;
    }

    private static void AppendMissingKeys(List<IniLine> lines, IEnumerable<(string Section, string Key, string Value)> missing)
    {
        foreach (IGrouping<string, (string Section, string Key, string Value)> group in missing.GroupBy(triple => triple.Section, StringComparer.OrdinalIgnoreCase))
        {
            string section        = group.Key;
            int    insertionIndex = FindSectionEnd(lines, section);

            if (insertionIndex < 0)
            {
                if (lines.Count > 0
                    && lines[^1] is not IniBlank)
                {
                    lines.Add(new IniBlank());
                }

                lines.Add(new IniSectionHeader(section, $"[{section}]"));

                foreach ((string _, string key, string value) in group)
                {
                    lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
                }
            }
            else
            {
                List<IniLine> keysToInsert = group
                                             .Select(triple => (IniLine)new IniKeyValue(section, triple.Key, triple.Value, $"{triple.Key}={triple.Value}"))
                                             .ToList();

                lines.InsertRange(insertionIndex, keysToInsert);
            }
        }
    }

    private static int FindSectionEnd(IReadOnlyList<IniLine> lines, string section)
    {
        int headerIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader header
                && string.Equals(header.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                headerIndex = i;

                break;
            }
        }

        if (headerIndex < 0)
        {
            return -1;
        }

        for (int i = headerIndex + 1; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader)
            {
                return i;
            }
        }

        return lines.Count;
    }

    private static string RenderLine(IniLine line)
        => line switch
        {
            IniKeyValue kv           => RenderKeyValue(kv),
            IniSectionHeader section => section.RawText,
            IniComment comment       => comment.RawText,
            IniBlank                 => string.Empty,
            var _                    => string.Empty
        };

    private static string RenderKeyValue(IniKeyValue kv)
    {
        int rawSeparator = kv.RawText.IndexOf(value: '=');

        if (rawSeparator > 0)
        {
            string rawValuePart = kv.RawText[(rawSeparator + 1)..]
                                    .TrimEnd();

            if (rawValuePart == kv.Value)
            {
                return kv.RawText;
            }
        }

        return $"{kv.Key}={kv.Value}";
    }
}
