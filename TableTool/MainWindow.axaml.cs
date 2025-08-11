using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TableTool.ViewModels;

namespace TableTool;

public partial class MainWindow : Window
{
	public IRelayCommand<TableFileItem> FileSelectedCommand { get; set; }
	public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();
		DataContext = vm;

		FileSelectedCommand = new RelayCommand<TableFileItem>(OnTableFileSelected);
	}
	private void OnTableFileSelected(TableFileItem? item)
	{
		if (item is null)
			return;
	}
}