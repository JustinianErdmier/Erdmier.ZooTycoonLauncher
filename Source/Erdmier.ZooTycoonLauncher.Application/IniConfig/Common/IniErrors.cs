namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>The expected failures of the INI Config slice. Descriptions are user-readable because the Desktop layer shows them verbatim.</summary>
public static class IniErrors
{
    /// <summary>The installation id does not resolve to a row.</summary>
    /// <param name="installationId">The missing id.</param>
    /// <returns>A not-found error coded <c>Installation.NotFound</c>.</returns>
    public static Error InstallationNotFound(Guid installationId) => Error.NotFound(code: "Installation.NotFound", $"No installation with id {installationId}.");

    /// <summary><c>zoo.ini</c> is absent from the installation folder.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <returns>A not-found error coded <c>Ini.Missing</c>.</returns>
    public static Error Missing(string installationPath) => Error.NotFound(code: "Ini.Missing", $"zoo.ini was not found in \"{installationPath}\".");

    /// <summary>Reading <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.ReadFailed</c>.</returns>
    public static Error ReadFailed(string message) => Error.Failure(code: "Ini.ReadFailed", $"zoo.ini could not be read: {message}");

    /// <summary>Writing <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.WriteFailed</c>.</returns>
    public static Error WriteFailed(string message) => Error.Failure(code: "Ini.WriteFailed", $"zoo.ini could not be saved: {message}");

    /// <summary>The installation's snapshot database failed.</summary>
    /// <param name="message">The underlying exception's message.</param>
    /// <returns>An unexpected error coded <c>Ini.StoreFailed</c>.</returns>
    public static Error StoreFailed(string message) => Error.Unexpected(code: "Ini.StoreFailed", $"The installation's settings history could not be opened: {message}");
}
