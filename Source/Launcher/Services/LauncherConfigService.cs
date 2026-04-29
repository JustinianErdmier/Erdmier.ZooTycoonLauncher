using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="ILauncherConfigService" />
public sealed class LauncherConfigService : ILauncherConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem;

    /// <param name="fileSystem"> Abstraction over the file system; the production wiring binds this to <see cref="FileSystem" />. </param>
    /// <param name="appDataRoot"> Root directory under which <c> ZooTycoonLauncher\launcher.config </c> is stored; the production wiring binds this to <see cref="Environment.GetFolderPath(Environment.SpecialFolder)" /> with <see cref="Environment.SpecialFolder.ApplicationData" />. </param>
    public LauncherConfigService(IFileSystem fileSystem, string appDataRoot)
    {
        _fileSystem = fileSystem;
        ConfigFilePath = _fileSystem.Path.Combine(appDataRoot, "ZooTycoonLauncher", "launcher.config");
    }

    /// <inheritdoc />
    public string ConfigFilePath { get; }

    /// <inheritdoc />
    public async Task<LauncherConfig> LoadAsync()
    {
        if (!_fileSystem.File.Exists(ConfigFilePath))
            return new LauncherConfig();

        try
        {
            await using var stream = _fileSystem.File.OpenRead(ConfigFilePath);
            if (stream.Length == 0) return new LauncherConfig();

            var config = await JsonSerializer.DeserializeAsync<LauncherConfig>(stream, SerializerOptions);
            return config ?? new LauncherConfig();
        }
        catch (Exception)
        {
            return new LauncherConfig();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(LauncherConfig config)
    {
        var directory = _fileSystem.Path.GetDirectoryName(ConfigFilePath)!;
        _fileSystem.Directory.CreateDirectory(directory);

        var tempPath = ConfigFilePath + ".tmp";
        await using (var stream = _fileSystem.File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, config, SerializerOptions);
        }

        _fileSystem.File.Move(tempPath, ConfigFilePath, overwrite: true);
    }
}
