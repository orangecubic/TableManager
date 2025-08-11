using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using TableTool.ViewModels;

namespace TableTool.Views;

public partial class SideMenu : UserControl
{
	public static readonly StyledProperty<IRelayCommand<TableFileItem>> FileSelectedCommandProperty =
		AvaloniaProperty.Register<SideMenu, IRelayCommand<TableFileItem>>(nameof(FileSelectedCommand));

	public IRelayCommand<TableFileItem> FileSelectedCommand
	{
		get => GetValue(FileSelectedCommandProperty);
		set => SetValue(FileSelectedCommandProperty, value);
	}

	public SideMenu()
    {
        InitializeComponent();
    }

    private async void OnPathButtonClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
	        Title = "폴더를 선택하세요"
        };

        string? folderPath = await dialog.ShowAsync((VisualRoot as Window)!);

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            var sideMenuModel = (DataContext as SideMenuModel)!;
			sideMenuModel.TableDirectory = folderPath;

			var files = Directory.GetFiles(folderPath, "*.xlsx");

			sideMenuModel.TableItemList = files.Select(f => new TableFileItem { Name = f.Split(Path.DirectorySeparatorChar)[^1], FullPath = f}).ToList();
        }
    }

}