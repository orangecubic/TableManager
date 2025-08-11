
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Collections.Generic;
using System.Windows.Input;

namespace TableTool.ViewModels;

public class TableFileItem
{

	public string Name { get; set; } = "";
	public string FullPath { get; set; } = "";
}

public class SideMenuModel(IRelayCommand<TableFileItem> fileSelectedCommand) : ViewModelBase
{
	private string _tableDirectory = "Not Set";

	public string TableDirectory
	{
		get => _tableDirectory;
		set => this.RaiseAndSetIfChanged(ref _tableDirectory, value);
	}

	private List<TableFileItem> _itemList =
	[
		new()
		{
			Name = "1.xlsx"
		},
		new()
		{
			Name = "2.xlsx"
		}
	];

	public List<TableFileItem> TableItemList
	{
		get => _itemList;
		set => this.RaiseAndSetIfChanged(ref _itemList, value);
	}

	public SideMenuModel() : this(new RelayCommand<TableFileItem>((_) => { })) { }

	public ICommand FileSelectedCommand => fileSelectedCommand;
}
