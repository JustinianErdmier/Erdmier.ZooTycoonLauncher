namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>Builds the editor's section view models and their field rows from <see cref="IniEditorCatalogue" />.</summary>
public static class IniFieldFactory
{
    /// <summary>Creates one section view model per catalogue section, in catalogue order.</summary>
    /// <returns>The sections.</returns>
    public static IReadOnlyList<IniSectionViewModel> CreateSections()
        => IniEditorCatalogue.Sections.Select(section => new IniSectionViewModel(section, CreateGroups(section)))
                             .ToList();

    /// <summary>Creates the field rows of one section, wiring the language picker to its two number rows.</summary>
    /// <param name="section">The section's catalogue entry.</param>
    /// <returns>The row groups.</returns>
    public static IReadOnlyList<IniFieldGroup> CreateGroups(IniSectionDescriptor section)
    {
        Dictionary<IniKeyId, IniFieldViewModel> keyed = section.Groups
                                                               .SelectMany(group => group.Fields)
                                                               .Where(descriptor => descriptor.Id is not null)
                                                               .ToDictionary(descriptor => descriptor.Id!.Value, CreateKeyedField);

        return section.Groups
                      .Select(group => new IniFieldGroup(group.SubHeader,
                                                         group.Fields
                                                              .Select(descriptor => descriptor.Id is { } id ? keyed[id] : CreateLanguagePicker(descriptor, keyed))
                                                              .ToList()))
                      .ToList();
    }

    private static IniFieldViewModel CreateKeyedField(IniFieldDescriptor descriptor)
        => descriptor.Control switch
        {
            IniControlKind.Toggle => new IniToggleFieldViewModel(descriptor),
            IniControlKind.Number => new IniNumberFieldViewModel(descriptor),
            IniControlKind.Text   => new IniTextFieldViewModel(descriptor),
            IniControlKind.Choice => new IniChoiceFieldViewModel(descriptor),
            var control           => throw new InvalidOperationException($"'{descriptor.Label}' has control {control}, which needs no key.")
        };

    private static IniLanguageFieldViewModel CreateLanguagePicker(IniFieldDescriptor descriptor, IReadOnlyDictionary<IniKeyId, IniFieldViewModel> keyed)
        => new(descriptor,
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.LanguageId],
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.SubLanguageId],
               IniEditorCatalogue.Languages);
}
