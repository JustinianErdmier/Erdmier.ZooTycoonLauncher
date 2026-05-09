using System;
using System.Globalization;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>
///     Discriminator for the underlying typed value of an <see cref="IniKeySpec" />. Used by display-side formatters that need to turn the spec's INI-string output back into
///     something human (e.g. <c> "1" </c> → <c> "On" </c> for bools).
/// </summary>
internal enum IniSpecKind { Bool, Int, NullableInt, Str, NullableStr }

/// <summary>
///     Binds a single INI section + key to a typed property on <see cref="ZooIniModel" />. Used by <see cref="ZooIniDefaults.KnownKeys" /> as the registry of all keys the
///     launcher understands.
/// </summary>
internal sealed class IniKeySpec
{
    private IniKeySpec(string section, string key, IniSpecKind kind, Func<ZooIniModel, string> read, Action<ZooIniModel, string> write)
    {
        Section = section;
        Key     = key;
        Kind    = kind;
        Read    = read;
        Write   = write;
    }

    /// <summary>Key name, e.g. <c> "fullscreen" </c>. Compared case-insensitively when matching INI lines.</summary>
    public string Key { get; }

    /// <summary>Underlying typed kind. Display-side formatters consult this to decide whether <c> Read </c>'s output should be shown verbatim or translated (e.g. bools to "On"/"Off").</summary>
    public IniSpecKind Kind { get; }

    /// <summary>Reads the current typed value from the model and serialises it to its INI string form.</summary>
    public Func<ZooIniModel, string> Read { get; }

    /// <summary>Section name (without brackets), e.g. <c> "user" </c> or <c> "UI" </c>. Compared case-insensitively when matching INI lines.</summary>
    public string Section { get; }

    /// <summary>Parses an INI string value and assigns it to the model. Falls back to the typed property's default on parse failure.</summary>
    public Action<ZooIniModel, string> Write { get; }

    public static IniKeySpec Bool(string section, string key, Func<ZooIniModel, bool> get, Action<ZooIniModel, bool> set)
        => new(section,
               key,
               IniSpecKind.Bool,
               model => get(model) ? "1" : "0",
               (model, raw) => set(model, ParseBool(raw, get(model))));

    public static IniKeySpec Int(string section, string key, Func<ZooIniModel, int> get, Action<ZooIniModel, int> set, int? min = null, int? max = null)
        => new(section,
               key,
               IniSpecKind.Int,
               model => get(model)
                   .ToString(CultureInfo.InvariantCulture),
               (model, raw) => set(model, ParseInt(raw, get(model), min, max)));

    public static IniKeySpec NullableInt(string section, string key, Func<ZooIniModel, int?> get, Action<ZooIniModel, int?> set)
        => new(section,
               key,
               IniSpecKind.NullableInt,
               model => get(model)
                            ?.ToString(CultureInfo.InvariantCulture)
                        ?? "",
               (model, raw) => set(model, ParseNullableInt(raw)));

    public static IniKeySpec Str(string section, string key, Func<ZooIniModel, string> get, Action<ZooIniModel, string> set)
        => new(section,
               key,
               IniSpecKind.Str,
               get,
               (model, raw) => set(model, raw));

    public static IniKeySpec NullableStr(string section, string key, Func<ZooIniModel, string?> get, Action<ZooIniModel, string?> set)
        => new(section,
               key,
               IniSpecKind.NullableStr,
               model => get(model) ?? "",
               (model, raw) => set(model, string.IsNullOrEmpty(raw) ? null : raw));

    private static bool ParseBool(string raw, bool fallback)
    {
        string trimmed = raw.Trim();

        return trimmed switch
        {
            "0"   => false,
            "1"   => true,
            var _ => bool.TryParse(trimmed, out bool parsed) ? parsed : fallback
        };
    }

    private static int ParseInt(string raw, int fallback, int? min, int? max)
    {
        if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return fallback;
        }

        if (min is not null
            && parsed < min)
        {
            return fallback;
        }

        if (max is not null
            && parsed > max)
        {
            return fallback;
        }

        return parsed;
    }

    private static int? ParseNullableInt(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;
    }
}
