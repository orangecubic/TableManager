
using ReactiveUI;

namespace TableTool.ViewModels;

public class ProgressBarModel : ViewModelBase
{
	private int _totalTasks = 10;
	public int TotalTasks
	{
		get => _totalTasks;
		set => this.RaiseAndSetIfChanged(ref _totalTasks, value);
	}

	private int _completedTasks = 3;
	public int CompletedTasks
	{
		get => _completedTasks;
		set => this.RaiseAndSetIfChanged(ref _completedTasks, value);
	}

	private bool _isProgress;

	public bool IsProgress
	{
		get => _isProgress;
		set => this.RaiseAndSetIfChanged(ref _isProgress, value);
	}

	public string ProgressText => $"{CompletedTasks} / {TotalTasks} \n 현재: ";
}
