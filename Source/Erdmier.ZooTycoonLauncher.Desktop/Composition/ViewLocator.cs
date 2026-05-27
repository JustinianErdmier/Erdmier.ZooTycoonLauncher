using Avalonia.Controls.Templates;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Maps each <c>*ViewModel</c> instance to its corresponding <c>*View</c> by name (SDD §4.3, §9.2).</summary>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        string viewModelTypeName = data.GetType()
                                       .FullName
                                   ?? string.Empty;

        string viewTypeName = viewModelTypeName.Replace(oldValue: "ViewModel", newValue: "View", StringComparison.Ordinal);
        Type?  viewType     = Type.GetType(viewTypeName);

        if (viewType is null)
        {
            return new TextBlock
            {
                Text = $"View not found: {viewTypeName}"
            };
        }

        return (Control)Activator.CreateInstance(viewType)!;
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModelBase;
}
