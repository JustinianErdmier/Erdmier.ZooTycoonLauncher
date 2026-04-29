using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IFolderPicker" />
/// <remarks>The implementation depends on a live <see cref="TopLevel" /> (the main window). The TopLevel is supplied via <see cref="SetTopLevel" /> after <see cref="Views.MainWindow" /> loads.</remarks>
public sealed class AvaloniaFolderPicker : IFolderPicker
{
    private TopLevel? _topLevel;

    /// <summary>Wires the picker to a live window. Called from <c> MainWindow.OnLoaded </c>.</summary>
    public void SetTopLevel(TopLevel topLevel) => _topLevel = topLevel;

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string title)
    {
        if (_topLevel is null)
            throw new InvalidOperationException("AvaloniaFolderPicker has not been bound to a TopLevel. Did MainWindow.OnLoaded forget to call SetTopLevel?");

        var folders = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (folders.Count == 0) return null;

        var path = folders[0].TryGetLocalPath();
        return string.IsNullOrEmpty(path) ? null : path;
    }
}
