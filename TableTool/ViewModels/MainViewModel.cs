
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace TableTool.ViewModels;

public class MainViewModel : ViewModelBase
{

	public SideMenuModel SideMenu { get; }

	public TabControlModel TabControl { get; }

	public ProgressBarModel ProgressBarModel { get; }

	public MainViewModel()
	{
		var fileSelectedCommand = new RelayCommand<TableFileItem>(OnTableFileSelected);
		var tabUpdateCommand = new RelayCommand<TabModel>(OnTabUpdate);

		SideMenu = new SideMenuModel(fileSelectedCommand);
		TabControl = new TabControlModel(tabUpdateCommand);
		ProgressBarModel = new ProgressBarModel();
	}

	private void OnTableFileSelected(TableFileItem? item)
	{
		if (item is null)
			return;

		TabControl.AddNewTab(new TabModel(item));
	}

	private void OnTabUpdate(TabModel? tab)
	{

	}
}
