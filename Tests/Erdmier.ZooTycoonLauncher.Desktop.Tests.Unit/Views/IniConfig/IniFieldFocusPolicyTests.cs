using Avalonia.Input;

using Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

public sealed class IniFieldFocusPolicyTests
{
    [ Theory ]
    [ InlineData(NavigationMethod.Tab, false) ]
    [ InlineData(NavigationMethod.Tab, true) ]
    [ InlineData(NavigationMethod.Directional, false) ]
    [ InlineData(NavigationMethod.Directional, true) ]
    public void KeyboardFocus_DrivesTheFooter(NavigationMethod method, bool current) => IniFieldFocusPolicy.NextFocusedState(current, method).ShouldBeTrue();

    [ Theory ]
    [ InlineData(false) ]
    [ InlineData(true) ]
    public void PointerFocus_DoesNotDriveTheFooter(bool current) => IniFieldFocusPolicy.NextFocusedState(current, NavigationMethod.Pointer).ShouldBeFalse();

    [ Theory ]
    [ InlineData(false) ]
    [ InlineData(true) ]
    public void UnspecifiedFocus_LeavesTheStateUnchanged(bool current) => IniFieldFocusPolicy.NextFocusedState(current, NavigationMethod.Unspecified).ShouldBe(current);

    [ Fact ]
    public void Escape_ClearsFieldFocus() => IniFieldFocusPolicy.ClearsFocus(Key.Escape).ShouldBeTrue();

    [ Theory ]
    [ InlineData(Key.Enter) ]
    [ InlineData(Key.Tab) ]
    [ InlineData(Key.A) ]
    public void OtherKeys_DoNotClearFieldFocus(Key key) => IniFieldFocusPolicy.ClearsFocus(key).ShouldBeFalse();

    [ Fact ]
    public void Enter_CommitsATypedValue() => IniFieldFocusPolicy.CommitsOnKey(Key.Enter).ShouldBeTrue();

    [ Theory ]
    [ InlineData(Key.Escape) ]
    [ InlineData(Key.Tab) ]
    [ InlineData(Key.A) ]
    [ InlineData(Key.Space) ]
    public void OtherKeys_DoNotCommit(Key key) => IniFieldFocusPolicy.CommitsOnKey(key).ShouldBeFalse();

    [ Fact ]
    public void Commit_PointerFocusedField_ReleasesFocus() => IniFieldFocusPolicy.ReleasesAfterCommit(fieldHasKeyboardFocus: false).ShouldBeTrue();

    [ Fact ]
    public void Commit_KeyboardFocusedField_KeepsFocus() => IniFieldFocusPolicy.ReleasesAfterCommit(fieldHasKeyboardFocus: true).ShouldBeFalse();
}
