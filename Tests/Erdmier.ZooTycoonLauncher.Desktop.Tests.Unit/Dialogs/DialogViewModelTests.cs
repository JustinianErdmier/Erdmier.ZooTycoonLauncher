using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class DialogViewModelTests
{
    [ Fact ]
    public void SaveChangesPrompt_AsksAboutZooIni() => new SaveChangesPromptViewModel().Message.ShouldBe(expected: "Do you want to save the changes to zoo.ini?");

    [ Fact ]
    public void ErrorMessage_KeepsTitleAndMessage()
    {
        ErrorMessageViewModel viewModel = new(title: "Cannot Save zoo.ini", message: "Access denied.");

        viewModel.Title.ShouldBe(expected: "Cannot Save zoo.ini");
        viewModel.Message.ShouldBe(expected: "Access denied.");
    }
}
