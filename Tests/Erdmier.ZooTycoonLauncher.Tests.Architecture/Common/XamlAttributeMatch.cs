namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>One attribute or style setter found in a XAML file by <see cref="XamlAttributeScanner" />.</summary>
/// <param name="Name">The property name, without any owner qualifier (<c>FontSize</c> for <c>TextElement.FontSize</c>).</param>
/// <param name="Value">The attribute or setter value, trimmed.</param>
/// <param name="Line">The 1-based line on which the match starts.</param>
/// <param name="Text">The matched text with runs of whitespace collapsed, for failure messages.</param>
public sealed record XamlAttributeMatch(string Name, string Value, int Line, string Text);
