namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>Builds the editor's section view models and their field rows from the layout of an <see cref="IniEditorStyle" /> (see <see cref="IniEditorLayouts" />).</summary>
public static class IniFieldFactory
{
    /// <summary>Creates one section view model per layout section or page, in layout order.</summary>
    /// <param name="style">The editor style.</param>
    /// <returns>The sections.</returns>
    public static IReadOnlyList<IniSectionViewModel> CreateSections(IniEditorStyle style)
        => IniEditorLayouts.Sections(style)
                           .Select(section => new IniSectionViewModel(section, CreateGroups(section, style), style == IniEditorStyle.Ini))
                           .ToList();

    /// <summary>Creates the field rows of one section or page, wiring the language picker to its two number rows.</summary>
    /// <param name="section">The section or page layout entry.</param>
    /// <param name="style">The editor style.</param>
    /// <returns>The row groups.</returns>
    public static IReadOnlyList<IniFieldGroup> CreateGroups(IniSectionDescriptor section, IniEditorStyle style)
    {
        bool isIni = style == IniEditorStyle.Ini;

        Dictionary<IniKeyId, IniFieldViewModel> keyed = section.Groups
                                                               .SelectMany(group => group.Fields)
                                                               .Where(descriptor => descriptor.Id is not null)
                                                               .ToDictionary(descriptor => descriptor.Id!.Value, descriptor => InStyle(CreateKeyedField(descriptor), isIni));

        return section.Groups
                      .Select(group => new IniFieldGroup(group.SubHeader,
                                                         group.Footnote,
                                                         group.Fields
                                                              .Select(descriptor => descriptor.Id is { } id ? keyed[id] : InStyle(CreateLanguagePicker(descriptor, keyed), isIni))
                                                              .ToList(),
                                                         isIni))
                      .ToList();
    }

    private static IniFieldViewModel InStyle(IniFieldViewModel field, bool isIni)
    {
        field.IsIniStyle = isIni;

        return field;
    }

    private static IniFieldViewModel CreateKeyedField(IniFieldDescriptor descriptor)
        => descriptor.Control switch
        {
            IniControlKind.Toggle => new IniToggleFieldViewModel(descriptor),
            IniControlKind.Number => new IniNumberFieldViewModel(descriptor),
            IniControlKind.Text   => new IniTextFieldViewModel(descriptor),
            IniControlKind.Choice => new IniChoiceFieldViewModel(descriptor),
            IniControlKind.Slider => new IniSliderFieldViewModel(descriptor),
            var control           => throw new InvalidOperationException($"'{descriptor.Label}' has control {control}, which needs no key.")
        };

    private static IniLanguageFieldViewModel CreateLanguagePicker(IniFieldDescriptor descriptor, IReadOnlyDictionary<IniKeyId, IniFieldViewModel> keyed)
        => new(descriptor,
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.LanguageId],
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.SubLanguageId],
               IniEditorCatalogue.Languages);
}
