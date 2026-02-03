using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using SOE_PubEditor.Services;
using SOE_PubEditor.ViewModels;

namespace SOE_PubEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Initialize file logger for debugging
        FileLogger.Initialize();
        
        if (DataContext is MainWindowViewModel vm)
        {
            // Wire up folder picker callbacks
            vm.SetPubDirectoryPicker(SelectPubDirectoryAsync);
            vm.SetGfxDirectoryPicker(SelectGfxDirectoryAsync);
            
            // Wire up graphic picker callback
            vm.SetGraphicPickerFunc(OpenGraphicPickerAsync);
            
            // Load data if directories are already set
            await vm.InitializeAsync();
        }
    }
    
    private async Task<int?> OpenGraphicPickerAsync(GfxType gfxType, int? currentId)
    {
        if (DataContext is not MainWindowViewModel vm) return null;
        
        var pickerVm = new GraphicPickerViewModel(vm.GfxService, gfxType);
        var dialog = new GraphicPickerDialog(pickerVm);
        
        var result = await dialog.ShowDialog<bool?>(this);
        
        if (result == true && dialog.SelectedGraphicId.HasValue)
        {
            return dialog.SelectedGraphicId;
        }
        
        return null;
    }
    
    // Click handlers for graphic previews
    private async void OnItemPreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.OpenItemGraphicPickerAsync();
        }
    }
    
    private async void OnNpcPreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.OpenNpcGraphicPickerAsync();
        }
    }
    
    private async void OnSpellPreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.OpenSpellGraphicPickerAsync();
        }
    }
    
    private async void OnSpellIconPreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.OpenSpellIconPickerAsync();
        }
    }
    
    private async void OnEquipmentPreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.OpenEquipmentGraphicPickerAsync();
        }
    }
    
    private async Task SelectPubDirectoryAsync()
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm == null) return;
        
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Pub File Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            await vm.LoadPubDirectoryAsync(path);
        }
    }
    
    private async Task SelectGfxDirectoryAsync()
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm == null) return;
        
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select GFX Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            vm.LoadGfxDirectory(path);
        }
    }
    
    private async void OnConfigClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var configWindow = new ConfigWindow(vm);
            await configWindow.ShowDialog(this);
        }
    }
    
    private void OnExitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
    
    // Clear type filter handlers
    private void OnClearItemTypeFilter(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedItemTypeFilter = null;
        }
    }
    
    private void OnClearNpcTypeFilter(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedNpcTypeFilter = null;
        }
    }
    
    private void OnClearSpellTypeFilter(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedSpellTypeFilter = null;
        }
    }
    
    // GFX Export handlers
    private async void OnExportItemGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedItem == null) return;
        
        var folder = await SelectExportFolderAsync("Export Item Graphics");
        if (folder == null) return;
        
        var exportService = new GfxExportService(vm.GfxService);
        var result = await exportService.ExportItemGraphicsAsync(vm.SelectedItem, folder);
        
        if (result.Success)
        {
            System.Console.WriteLine($"GFX EXPORT: Successfully exported {result.FilesExported} files");
        }
        else
        {
            System.Console.WriteLine($"GFX EXPORT ERROR: {result.Error}");
        }
    }
    
    private async void OnExportNpcGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedNpc == null) return;
        
        var folder = await SelectExportFolderAsync("Export NPC Graphics");
        if (folder == null) return;
        
        var exportService = new GfxExportService(vm.GfxService);
        var result = await exportService.ExportNpcGraphicsAsync(vm.SelectedNpc, folder);
        
        if (result.Success)
        {
            System.Console.WriteLine($"GFX EXPORT: Successfully exported {result.FilesExported} files");
        }
        else
        {
            System.Console.WriteLine($"GFX EXPORT ERROR: {result.Error}");
        }
    }
    
    private async void OnExportSpellGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedSpell == null) return;
        
        var folder = await SelectExportFolderAsync("Export Spell Graphics");
        if (folder == null) return;
        
        var exportService = new GfxExportService(vm.GfxService);
        var result = await exportService.ExportSpellGraphicsAsync(vm.SelectedSpell, folder);
        
        if (result.Success)
        {
            System.Console.WriteLine($"GFX EXPORT: Successfully exported {result.FilesExported} files");
        }
        else
        {
            System.Console.WriteLine($"GFX EXPORT ERROR: {result.Error}");
        }
    }
    
    private async Task<string?> SelectExportFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
    
    /// <summary>
    /// Shows a simple dialog to choose output GFX folder location.
    /// Returns null if canceled, empty string to use current folder, or a path for new folder.
    /// </summary>
    private async Task<string?> PromptForOutputGfxFolderAsync(string currentGfxDirectory)
    {
        // Create a simple choice dialog using MessageBox-style approach
        // For now, we'll show a folder picker with the current directory as default
        // The user can either accept it (same folder) or navigate to a different one
        
        var dialog = new Avalonia.Controls.Window
        {
            Title = "Select Output GFX Folder",
            Width = 450,
            Height = 180,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        
        string? selectedFolder = null;
        bool useCurrentFolder = false;
        bool canceled = true;
        
        var mainPanel = new Avalonia.Controls.StackPanel
        {
            Margin = new Avalonia.Thickness(15),
            Spacing = 10
        };
        
        mainPanel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = "Where would you like to save the imported graphics?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        
        mainPanel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = $"Current: {currentGfxDirectory}",
            FontStyle = Avalonia.Media.FontStyle.Italic,
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });
        
        var buttonPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };
        
        var currentBtn = new Avalonia.Controls.Button { Content = "Use Current Folder", Width = 130 };
        var newBtn = new Avalonia.Controls.Button { Content = "Choose New Folder", Width = 130 };
        var cancelBtn = new Avalonia.Controls.Button { Content = "Cancel", Width = 80 };
        
        currentBtn.Click += (s, e) => { useCurrentFolder = true; canceled = false; dialog.Close(); };
        newBtn.Click += async (s, e) =>
        {
            var folder = await SelectExportFolderAsync("Select Output GFX Folder");
            if (folder != null)
            {
                selectedFolder = folder;
                canceled = false;
                dialog.Close();
            }
        };
        cancelBtn.Click += (s, e) => { dialog.Close(); };
        
        buttonPanel.Children.Add(currentBtn);
        buttonPanel.Children.Add(newBtn);
        buttonPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(buttonPanel);
        
        dialog.Content = mainPanel;
        
        await dialog.ShowDialog(this);
        
        if (canceled) return null;
        if (useCurrentFolder) return ""; // Empty string means use current
        return selectedFolder;
    }
    
    // GFX Import handlers
    private async void OnImportItemGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FileLogger.LogInfo("OnImportItemGraphics started");
        if (DataContext is not MainWindowViewModel vm || vm.SelectedItem == null)
        {
            FileLogger.LogWarning("OnImportItemGraphics: No selected item or invalid context");
            return;
        }
        
        FileLogger.LogInfo($"Importing graphics for item: {vm.SelectedItem.Name} (GraphicId={vm.SelectedItem.GraphicId})");
        
        // First, select the input folder with BMP files
        var inputFolder = await SelectExportFolderAsync("Select Folder Containing BMP Files");
        if (inputFolder == null)
        {
            FileLogger.LogInfo("OnImportItemGraphics: User canceled folder selection");
            return;
        }
        
        FileLogger.LogInfo($"Input folder selected: {inputFolder}");
        
        // Then, prompt for output GFX folder
        var outputChoice = await PromptForOutputGfxFolderAsync(vm.GfxService.GfxDirectory ?? "");
        if (outputChoice == null)
        {
            FileLogger.LogInfo("OnImportItemGraphics: User canceled output folder selection");
            return;
        }
        
        var outputGfxDir = string.IsNullOrEmpty(outputChoice) ? null : outputChoice;
        FileLogger.LogInfo($"Output GFX folder: {outputGfxDir ?? "(using current)"}");
        
        var importService = new GfxImportService(vm.GfxService);
        FileLogger.LogInfo($"ImportService.IsAvailable: {importService.IsAvailable}");
        
        var result = await importService.ImportItemGraphicsAsync(vm.SelectedItem, inputFolder, outputGfxDir);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT: Successfully imported {result.FilesImported} files");
            
            // Clear GFX cache and refresh the view so user sees the updated graphics
            vm.GfxService.ClearCache();
            
            // Re-trigger selection to refresh the GFX previews
            var selectedItem = vm.SelectedItem;
            vm.SelectedItem = null;
            vm.SelectedItem = selectedItem;
            
            vm.StatusText = $"Imported {result.FilesImported} graphics for {selectedItem?.Name}";
        }
        else
        {
            FileLogger.LogError($"GFX IMPORT ERROR: {result.Error}");
            vm.StatusText = $"Import failed: {result.Error}";
        }
    }
    
    private async void OnImportNpcGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FileLogger.LogInfo("OnImportNpcGraphics started");
        if (DataContext is not MainWindowViewModel vm || vm.SelectedNpc == null)
        {
            FileLogger.LogWarning("OnImportNpcGraphics: No selected NPC or invalid context");
            return;
        }
        
        FileLogger.LogInfo($"Importing graphics for NPC: {vm.SelectedNpc.Name} (GraphicId={vm.SelectedNpc.GraphicId})");
        
        // First, select the input folder with BMP files
        var inputFolder = await SelectExportFolderAsync("Select Folder Containing BMP Files");
        if (inputFolder == null) return;
        
        // Then, prompt for output GFX folder
        var outputChoice = await PromptForOutputGfxFolderAsync(vm.GfxService.GfxDirectory ?? "");
        if (outputChoice == null) return; // Canceled
        
        var outputGfxDir = string.IsNullOrEmpty(outputChoice) ? null : outputChoice;
        
        var importService = new GfxImportService(vm.GfxService);
        var result = await importService.ImportNpcGraphicsAsync(vm.SelectedNpc, inputFolder, outputGfxDir);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT (NPC): Successfully imported {result.FilesImported} files");
            
            // Clear GFX cache and refresh the view
            vm.GfxService.ClearCache();
            var selectedNpc = vm.SelectedNpc;
            vm.SelectedNpc = null;
            vm.SelectedNpc = selectedNpc;
            
            vm.StatusText = $"Imported {result.FilesImported} graphics for {selectedNpc?.Name}";
        }
        else
        {
            FileLogger.LogError($"GFX IMPORT ERROR (NPC): {result.Error}");
            vm.StatusText = $"Import failed: {result.Error}";
        }
    }
    
    private async void OnImportSpellGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FileLogger.LogInfo("OnImportSpellGraphics started");
        if (DataContext is not MainWindowViewModel vm || vm.SelectedSpell == null)
        {
            FileLogger.LogWarning("OnImportSpellGraphics: No selected spell or invalid context");
            return;
        }
        
        FileLogger.LogInfo($"Importing graphics for Spell: {vm.SelectedSpell.Name} (GraphicId={vm.SelectedSpell.GraphicId})");
        
        // First, select the input folder with BMP files
        var inputFolder = await SelectExportFolderAsync("Select Folder Containing BMP Files");
        if (inputFolder == null) return;
        
        // Then, prompt for output GFX folder
        var outputChoice = await PromptForOutputGfxFolderAsync(vm.GfxService.GfxDirectory ?? "");
        if (outputChoice == null) return; // Canceled
        
        var outputGfxDir = string.IsNullOrEmpty(outputChoice) ? null : outputChoice;
        
        var importService = new GfxImportService(vm.GfxService);
        var result = await importService.ImportSpellGraphicsAsync(vm.SelectedSpell, inputFolder, outputGfxDir);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT (Spell): Successfully imported {result.FilesImported} files");
            
            // Clear GFX cache and refresh the view
            vm.GfxService.ClearCache();
            var selectedSpell = vm.SelectedSpell;
            vm.SelectedSpell = null;
            vm.SelectedSpell = selectedSpell;
            
            vm.StatusText = $"Imported {result.FilesImported} graphics for {selectedSpell?.Name}";
        }
        else
        {
            FileLogger.LogError($"GFX IMPORT ERROR (Spell): {result.Error}");
            vm.StatusText = $"Import failed: {result.Error}";
        }
    }
}
