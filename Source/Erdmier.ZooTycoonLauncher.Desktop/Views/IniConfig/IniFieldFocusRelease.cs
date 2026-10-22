namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>Applies <see cref="IniFieldFocusPolicy" /> to a live control: moves focus off the INI field the pointer focused, without landing on another input.</summary>
public static class IniFieldFocusRelease
{
    /// <summary>Clears focus when the control sits in an INI field whose focus did not come from the keyboard.</summary>
    /// <param name="control">A control inside the field's row; its inherited <c>DataContext</c> is the field.</param>
    public static void AfterCommit(Control control)
    {
        bool keyboardFocused = control.DataContext is IniFieldViewModel { IsFocused: true };

        if (IniFieldFocusPolicy.ReleasesAfterCommit(keyboardFocused))
        {
            Clear(control);
        }
    }

    /// <summary>Clears focus from whichever control holds it, so the focused field loses focus (committing any typed value) and the footer returns to the save status.</summary>
    /// <param name="control">Any control in the window.</param>
    public static void Clear(Control control) => TopLevel.GetTopLevel(control)?.FocusManager?.ClearFocus();
}
