using System.Collections.Immutable;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the transient "looking for Zoo Tycoon" state shown while <c>BootCommand</c> is in flight.</summary>
public sealed partial class LookingForZooTycoonViewModel : ViewModelBase
{
    private readonly DispatcherTimer _phraseTimer;

    private int _phraseIndex;

    /// <summary>Initialises a new instance and primes the displayed phrase to the first entry in <see cref="ProgressPhrases" />.</summary>
    public LookingForZooTycoonViewModel()
    {
        CurrentPhrase = ProgressPhrases[index: 0];

        _phraseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds: 250)
        };

        _phraseTimer.Tick += OnPhraseTimerTick;
    }

    /// <summary>The phrase currently displayed; advanced through <see cref="ProgressPhrases" /> every two seconds while cycling is active.</summary>
    [ ObservableProperty ]
    public partial string CurrentPhrase { get; set; }

    /// <summary>The ordered, cyclic set of status phrases shown one at a time beneath the progress bar.</summary>
    private IImmutableList<string> ProgressPhrases { get; } = ImmutableList.Create("Searching for Zoo Tycoon…",
                                                                                   "Locating Zoo Tycoon installation…",
                                                                                   "Verifying Zoo Tycoon files…",
                                                                                   "Loading Launcher.db…",
                                                                                   "Reading launcher settings…",
                                                                                   @"Querying registry: HKLM\…\Zoo Tycoon\1.0",
                                                                                   @"Probing C:\Program Files (x86)\Zoo Tycoon\1.0",
                                                                                   "Verifying Zoo Tycoon installation integrity…",
                                                                                   "Verifying zoo.exe…",
                                                                                   "Parsing zoo.ini…",
                                                                                   "Verifying zoo.ini…");

    /// <summary>Begins cycling the displayed phrase. Called when the host view is attached to the visual tree.</summary>
    public void StartCycling() => _phraseTimer.Start();

    /// <summary>Stops cycling the displayed phrase. Called when the host view is detached so the timer does not outlive the view.</summary>
    public void StopCycling() => _phraseTimer.Stop();

    private void OnPhraseTimerTick(object? sender, EventArgs e)
    {
        _phraseIndex = (_phraseIndex + 1) % ProgressPhrases.Count;

        CurrentPhrase = ProgressPhrases[_phraseIndex];
    }
}
