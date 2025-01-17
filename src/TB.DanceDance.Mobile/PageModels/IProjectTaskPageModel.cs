using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Models;

namespace TB.DanceDance.Mobile.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
    bool IsBusy { get; }
}