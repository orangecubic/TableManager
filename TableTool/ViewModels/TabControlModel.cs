
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Drawing.Charts;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TableGenerator.Parser;
using TableGenerator.Tables;
using DataTable = System.Data.DataTable;

namespace TableTool.ViewModels;

public class TabModel : ViewModelBase
{
    public TableFileItem Item { get; set; }

    public TabModel(TableFileItem item)
    {
        Item = item;
    }
}

public class TabControlModel(IRelayCommand<TabModel> tabEventCommand) : ViewModelBase
{
	private ObservableCollection<TabModel> _tabList = [new(new() { Name = "1.xlsx" }), new(new() { Name = "2.xlsx" })];

	public ObservableCollection<TabModel> Tabs
    {
        get => _tabList;
        set => this.RaiseAndSetIfChanged(ref _tabList, value);
    }

	private ObservableCollection<DataTable> _sheetTabs = [];

	public ObservableCollection<DataTable> SheetTabs
	{
		get => _sheetTabs;
		set => this.RaiseAndSetIfChanged(ref _sheetTabs, value);
	}

	private TabModel? _selectedTab;
    public TabModel? SelectedTab { get => _selectedTab; set => this.RaiseAndSetIfChanged(ref _selectedTab, value); }

    private DataTable? _selectedSheetTab;
	public DataTable? SelectedSheetTab { get => _selectedSheetTab;
		set {
            SelectedSheetView = value?.DefaultView;
            this.RaiseAndSetIfChanged(ref _selectedSheetTab, value);
		} }

    private DataView? _selectedSheetView;
    public DataView? SelectedSheetView { get => _selectedSheetView; set => this.RaiseAndSetIfChanged(ref _selectedSheetView, value); }

	public TabControlModel() : this(new RelayCommand<TabModel>((_) => { })) { }

    public IRelayCommand<TabModel> TabCloseEventCommand => new RelayCommand<TabModel>(OnTabCloseEvent);

	public void AddNewTab(TabModel model)
	{
		if (SelectedTab?.Item.FullPath == model.Item.FullPath)
			return;

		var existsTab = Tabs.FirstOrDefault(t => t.Item.FullPath == model.Item.FullPath);
		if (existsTab is null)
		{
			Tabs.Add(model);
			OnTabClickEvent(model);
		}
		else
		{
			OnTabClickEvent(existsTab);
		}
	}

    private void OnTabClickEvent(TabModel? model)
    {
	    if (model is null)
		    return;

	    SelectedTab = model;

        tabEventCommand.Execute(model);

		LoadExcel(model);
    }

    private void OnTabCloseEvent(TabModel? model)
    {
	    if (model is null)
		    return;

	    Tabs.Remove(model);

	    SelectedTab = Tabs.FirstOrDefault();

	    this.RaiseAndSetIfChanged(ref _tabList, _tabList);

		tabEventCommand.Execute(SelectedTab);
	}

	private void LoadExcel(TabModel model)
	{
		SheetTabs.Clear();
		SelectedSheetTab = null;

		var tables = new TableExtractor(model.Item.FullPath).ExtractTables();

		var table = DesignTable.LoadFromDirectoryAsync("").Result;
		
		SheetTabs.AddRange(tables);
		// 파일 로드 후 첫 번째 시트를 선택된 상태로 만듦
		SelectedSheetTab = SheetTabs.FirstOrDefault();
	}
}
