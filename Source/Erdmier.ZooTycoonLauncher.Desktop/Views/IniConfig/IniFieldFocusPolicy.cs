namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>
///     The rules for when an INI field counts as focused and when it gives focus back (milestone 11.1 §6a.1). Only keyboard focus is lasting: a field the pointer focused
///     must not take the editor footer over, and releases focus once its value is set. Kept free of controls so the rules are unit-testable; the views only apply them.
/// </summary>
public static class IniFieldFocusPolicy
{
    /// <summary>The field's focused state after it gained focus: keyboard focus sets it, pointer focus clears it, and focus moving inside the row leaves it alone.</summary>
    /// <param name="current">Whether the field counts as focused now.</param>
    /// <param name="method">How focus arrived (<see cref="GotFocusEventArgs.NavigationMethod" />).</param>
    /// <returns>
    ///     <see langword="true" /> for Tab and arrow-key focus; <see langword="false" /> for the pointer; <paramref name="current" /> for <see cref="NavigationMethod.Unspecified" />, which is how a
    ///     combo box moves focus to its own pop-up items and back, and must not cost a keyboard user their place.
    /// </returns>
    public static bool NextFocusedState(bool current, NavigationMethod method)
        => method switch
        {
            NavigationMethod.Tab or NavigationMethod.Directional => true,
            NavigationMethod.Pointer                             => false,
            _                                                    => current
        };

    /// <summary>Whether a field should give focus back once its value has been set.</summary>
    /// <param name="fieldHasKeyboardFocus">Whether the field's focus came from the keyboard.</param>
    /// <returns><see langword="true" /> when the focus came from the pointer, so a keyboard user keeps their place.</returns>
    public static bool ReleasesAfterCommit(bool fieldHasKeyboardFocus) => !fieldHasKeyboardFocus;

    /// <summary>Whether the key clears the focus of the INI field the keyboard is in.</summary>
    /// <param name="key">The pressed key.</param>
    /// <returns><see langword="true" /> for Esc.</returns>
    public static bool ClearsFocus(Key key) => key == Key.Escape;

    /// <summary>Whether the key commits a typed value in a text or number box.</summary>
    /// <param name="key">The pressed key.</param>
    /// <returns><see langword="true" /> for Enter.</returns>
    public static bool CommitsOnKey(Key key) => key == Key.Enter;
}
