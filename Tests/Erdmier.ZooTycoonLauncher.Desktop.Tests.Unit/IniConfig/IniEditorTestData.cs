namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

internal static class IniEditorTestData
{
    public static readonly DateTime LastWrite = new(year: 2026, month: 5, day: 24, hour: 19, minute: 2, second: 0, DateTimeKind.Utc);

    public static IniConfigResult Result(params (string Section, string Key, string? Value)[] values)
        => new(values.ToDictionary(value => new IniKeyId(value.Section, value.Key), value => value.Value), LastWrite);

    public static T Field<T>(IniEditorViewModel editor, string section, string key)
        where T : IniFieldViewModel
        => editor.Sections.SelectMany(s => s.Fields).OfType<T>().Single(field => field.Id == new IniKeyId(section, key));

    public static void ReturnsForGet(IMediator mediator, ErrorOr<IniConfigResult> result)
        => mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                   .Returns(new ValueTask<ErrorOr<IniConfigResult>>(result));

    public static void ReturnsForSave(IMediator mediator, ErrorOr<IniConfigResult> result)
        => mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                   .Returns(new ValueTask<ErrorOr<IniConfigResult>>(result));

    public static InstallationSummary Installation(bool hasIni = true)
        => new(Guid.CreateVersion7(), Name: "Main", Path: @"C:\ZT", InstallationValidity.From(hasExe: true, hasIni), IsDefault: true, LastWrite, ModifiedUtc: null,
               LastPlayedUtc: null, LastOpenedUtc: null);
}
