namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>The input control an INI row renders.</summary>
public enum IniControlKind
{
    /// <summary>A caption check box over a <c>Bool</c> key (optionally inverted).</summary>
    Toggle,

    /// <summary>A <c>NumericUpDown</c> over an integer key, bounded by the key's spec.</summary>
    Number,

    /// <summary>A <c>TextBox</c> over a string key.</summary>
    Text,

    /// <summary>A <c>ComboBox</c> of labelled raw values.</summary>
    Choice,

    /// <summary>The curated language combo that drives the <c>lang</c> and <c>sublang</c> rows; it has no key of its own.</summary>
    LanguagePicker
}
