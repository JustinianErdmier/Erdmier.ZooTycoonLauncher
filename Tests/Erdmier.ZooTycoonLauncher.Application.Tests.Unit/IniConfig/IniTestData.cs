namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

internal static class IniTestData
{
    public static readonly DateTime Now = new(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly string Sample = Text("; Zoo Tycoon settings",
                                                "[user]",
                                                "fullscreen=1",
                                                "screenwidth=800",
                                                "lastfile=C:\\Spiele\\Zoo M\u00FCller.zoo",
                                                "",
                                                "[UI]",
                                                "tooltipDelay=1",
                                                "lastWindowX=10",
                                                "",
                                                "[scenario]",
                                                "ag=0");

    public static string Text(params string[] lines) => string.Join(separator: "\r\n", lines) + "\r\n";

    public static IniSnapshot CurrentSnapshot(string text)
    {
        Guid id = Guid.CreateVersion7();

        return new IniSnapshot
        {
            Id            = id,
            Kind          = IniSnapshotKind.Current,
            Trigger       = IniSnapshotTrigger.OriginalImport,
            CapturedUtc   = new DateTime(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc),
            StructureBlob = text,
            Values = ZooIniDefaults.ExtractValues(IniDocument.Parse(text))
                                   .Select(pair => new IniValue
                                   {
                                       SnapshotId = id,
                                       Section    = pair.Key.Section,
                                       Key        = pair.Key.Key,
                                       Value      = pair.Value,
                                       ValueKind  = ZooIniDefaults.TryGet(pair.Key, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                                       Source     = IniValueSource.OriginalImport
                                   })
                                   .ToList()
        };
    }

    public static IIniSnapshotTransaction Transaction(IniSnapshot? current)
    {
        IIniSnapshotTransaction transaction = Substitute.For<IIniSnapshotTransaction>();

        transaction.GetCurrentAsync(Arg.Any<CancellationToken>())
                   .Returns(current);

        return transaction;
    }
}
