namespace Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     The curated language combo. It has no key of its own: choosing an entry writes the <c>lang</c> and <c>sublang</c> rows, and it shows the entry matching their pair (none
///     when the pair is not curated). It is never dirty — change tracking lives on the two number rows.
/// </summary>
public sealed class IniLanguageFieldViewModel : IniFieldViewModel
{
    private readonly IniNumberFieldViewModel _language;

    private readonly IniNumberFieldViewModel _subLanguage;

    private bool _writing;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    /// <param name="language">The <c>[language]/lang</c> row.</param>
    /// <param name="subLanguage">The <c>[language]/sublang</c> row.</param>
    /// <param name="options">The curated languages.</param>
    public IniLanguageFieldViewModel(IniFieldDescriptor descriptor, IniNumberFieldViewModel language, IniNumberFieldViewModel subLanguage, IReadOnlyList<IniLanguageOption> options)
        : base(descriptor)
    {
        _language    = language;
        _subLanguage = subLanguage;

        Options = options;

        _language.PropertyChanged    += OnSourcePropertyChanged;
        _subLanguage.PropertyChanged += OnSourcePropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniLanguageFieldViewModel()
        : this(new IniFieldDescriptor(Id: null, Label: "lang / sublang", IniControlKind.LanguagePicker, Hint: "Curated LANGID + SUBLANGID", Help: "Game language."),
               new IniNumberFieldViewModel(new IniFieldDescriptor(IniEditorCatalogue.LanguageId, Label: "lang", IniControlKind.Number, Hint: null, Help: "LANGID")),
               new IniNumberFieldViewModel(new IniFieldDescriptor(IniEditorCatalogue.SubLanguageId, Label: "sublang", IniControlKind.Number, Hint: null, Help: "SUBLANGID")),
               IniEditorCatalogue.Languages)
    {
        _language.Load(raw: null);
        _subLanguage.Load(raw: null);
    }

    /// <summary>The curated languages.</summary>
    public IReadOnlyList<IniLanguageOption> Options { get; }

    /// <summary>Whether the muted codes line under the picker is shown (the Friendly style).</summary>
    public bool ShowCodesText => !IsIniStyle;

    /// <summary>The muted line under the picker naming the current codes, on two lines so the pair never wraps apart: <c>Windows language codes:</c> then <c>LANGID 9 · SUBLANGID 1</c> (spec C13, §6.6).</summary>
    public string CodesText => $"Windows language codes:\nLANGID {Format(_language.Value)} · SUBLANGID {Format(_subLanguage.Value)}";

    /// <summary>The curated entry matching the two rows, or <see langword="null" />. Setting a non-null entry writes both rows.</summary>
    public IniLanguageOption? SelectedOption
    {
        get => Options.FirstOrDefault(option => _language.Value == option.Lang && _subLanguage.Value == option.SubLang);
        set
        {
            if (value is null)
            {
                return;
            }

            _writing = true;

            try
            {
                _language.Value    = value.Lang;
                _subLanguage.Value = value.SubLang;
            }
            finally
            {
                _writing = false;
            }

            OnPropertyChanged(nameof(SelectedOption));
            OnPropertyChanged(nameof(CodesText));
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (!_writing && eventArgs.PropertyName == nameof(IniNumberFieldViewModel.Value))
        {
            OnPropertyChanged(nameof(SelectedOption));
            OnPropertyChanged(nameof(CodesText));
        }
    }

    private static string Format(decimal? code) => code is { } value ? decimal.ToInt32(decimal.Truncate(value)).ToString(CultureInfo.InvariantCulture) : "—";
}
