using Avalonia.Input;

using Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

public sealed class IniFieldFocusPolicyTests
{
    [ Theory ]
    [ InlineData(NavigationMethod.Pointer) ]
    [ InlineData(NavigationMethod.Unspecified) ]
    public void PointerFocus_DoesNotDriveTheFooter(NavigationMethod method) => IniFieldFocusPolicy.DrivesFooter(method).ShouldBeFalse();

    [ Theory ]
    [ InlineData(NavigationMethod.Tab) ]
    [ InlineData(NavigationMethod.Directional) ]
    public void KeyboardFocus_DrivesTheFooter(NavigationMethod method) => IniFieldFocusPolicy.DrivesFooter(method).ShouldBeTrue();

    [ Fact ]
    public void Escape_ClearsFieldFocus() => IniFieldFocusPolicy.ClearsFocus(Key.Escape).ShouldBeTrue();

    [ Theory ]
    [ InlineData(Key.Enter) ]
    [ InlineData(Key.Tab) ]
    [ InlineData(Key.A) ]
    public void OtherKeys_DoNotClearFieldFocus(Key key) => IniFieldFocusPolicy.ClearsFocus(key).ShouldBeFalse();

    [ Fact ]
    public void Enter_CommitsATypedValue() => IniFieldFocusPolicy.CommitsOnKey(Key.Enter).ShouldBeTrue();

    [ Fact ]
    public void Commit_PointerFocusedField_ReleasesFocus() => IniFieldFocusPolicy.ReleasesAfterCommit(fieldHasKeyboardFocus: false).ShouldBeTrue();

    [ Fact ]
    public void Commit_KeyboardFocusedField_KeepsFocus() => IniFieldFocusPolicy.ReleasesAfterCommit(fieldHasKeyboardFocus: true).ShouldBeFalse();
}
