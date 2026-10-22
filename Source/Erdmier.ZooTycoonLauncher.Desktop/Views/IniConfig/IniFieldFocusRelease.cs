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
    /// <remarks>
    ///     Avalonia marks <c>IFocusManager.ClearFocus</c> obsolete (CS0618: "This API might be removed in 11.x minor updates. Please consider focusing another
    ///     element instead of removing focus at all for better UX."). It is used on purpose: spec 11.1 §6a.1 wants no input to keep focus after a pointer commit,
    ///     a click on empty editor space or Esc, and focusing another element would land on an input or need a focusable stand-in. If an Avalonia update removes
    ///     the method, or focus stops clearing after a drop-down pick, a spinner step, a toggle click, Enter or Esc, this is the cause: focus a non-tab-stop,
    ///     focusable container in <c>IniEditorView</c> instead.
    /// </remarks>
    public static void Clear(Control control)
    {
#pragma warning disable CS0618 // Obsolete in Avalonia 11.3 but still the only way to leave nothing focused; see remarks.
        TopLevel.GetTopLevel(control)?.FocusManager?.ClearFocus();
#pragma warning restore CS0618
    }

    /// <summary>Whether the element is the row or lives inside it, counting a combo box's pop-up items (which sit in their own window) as inside.</summary>
    /// <param name="row">The INI row.</param>
    /// <param name="element">The element to test; <see langword="null" /> means nothing has focus, which is outside.</param>
    /// <returns><see langword="true" /> when the row contains the element.</returns>
    public static bool IsWithin(Control row, IInputElement? element)
    {
        for (StyledElement? current = element as StyledElement; current is not null; current = (current as Visual)?.GetVisualParent() as StyledElement ?? current.Parent)
        {
            if (ReferenceEquals(current, row))
            {
                return true;
            }
        }

        return false;
    }
}
