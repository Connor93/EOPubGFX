using System;
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
    
    private async void OnExportAllGfxClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        
        if (string.IsNullOrEmpty(vm.GfxService.GfxDirectory))
        {
            vm.StatusText = "Error: No GFX directory configured";
            return;
        }
        
        // Let user pick the output folder
        var folder = await SelectExportFolderAsync("Select Output Folder for Bulk GFX Export");
        if (folder == null) return;
        
        var exportService = new GfxExportService(vm.GfxService);
        
        // Create and show progress dialog
        var progressDialog = new ExportProgressDialog();
        var cts = new System.Threading.CancellationTokenSource();
        progressDialog.SetCancellationTokenSource(cts);
        
        // Show dialog non-modally and run export
        progressDialog.Show(this);
        
        // Update progress in dialog
        var progress = new Progress<(string status, int current, int total)>(p =>
        {
            progressDialog.UpdateProgress(p.status, p.current, p.total);
        });
        
        try
        {
            var result = await exportService.ExportAllGraphicsAsync(
                folder, 
                vm.Items, 
                vm.Npcs, 
                vm.Spells, 
                progress);
            
            if (result.Success)
            {
                progressDialog.SetCompleted(result.FilesExported, folder);
                vm.StatusText = $"Exported {result.FilesExported} graphics to {folder}";
                FileLogger.LogInfo($"BULK EXPORT: Successfully exported {result.FilesExported} files to {folder}");
            }
            else
            {
                progressDialog.SetError(result.Error ?? "Unknown error");
                vm.StatusText = $"Export failed: {result.Error}";
                FileLogger.LogError($"BULK EXPORT ERROR: {result.Error}");
            }
        }
        catch (System.Exception ex)
        {
            progressDialog.SetError(ex.Message);
            vm.StatusText = $"Export error: {ex.Message}";
            FileLogger.LogError($"BULK EXPORT EXCEPTION: {ex}");
        }
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
        
        var item = vm.SelectedItem;
        FileLogger.LogInfo($"Importing graphics for item: {item.Name} (GraphicId={item.GraphicId}, Spec1={item.Spec1})");
        
        // Determine if this is equipment (needs both inventory and doll graphics)
        var isEquipment = item.Type == Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Weapon ||
                          item.Type == Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Shield ||
                          item.Type == Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Armor ||
                          item.Type == Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Hat ||
                          item.Type == Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Boots;
        
        // Determine if item has existing graphics
        var hasExistingGraphics = item.GraphicId > 0;
        var hasExistingDollGraphics = isEquipment && item.Spec1 > 0;
        
        // Prepare target IDs
        int? targetGraphicId = null;
        int? targetDollGraphicId = null;
        
        // For items with existing graphics, show import mode dialog
        if (hasExistingGraphics || hasExistingDollGraphics)
        {
            // Get next available IDs for display in dialog
            var nextGraphicId = vm.GfxService.GetNextAvailableItemGraphicId();
            GfxType? equipGfxType = isEquipment ? GetEquipmentGfxType(item.Type) : null;
            var nextDollId = equipGfxType.HasValue ? vm.GfxService.GetNextAvailableDollGraphicId(equipGfxType.Value) : 0;
            
            var dialog = new ImportModeDialog(
                currentSlot: hasExistingGraphics ? item.GraphicId : item.Spec1,
                nextAvailableSlot: hasExistingGraphics ? nextGraphicId : nextDollId,
                entityType: "Item"
            );
            
            var mode = await dialog.ShowDialog<ImportMode>(this);
            
            if (mode == ImportMode.Cancel)
            {
                FileLogger.LogInfo("OnImportItemGraphics: User canceled import mode dialog");
                return;
            }
            
            if (mode == ImportMode.Replace)
            {
                // Use existing IDs
                FileLogger.LogInfo("OnImportItemGraphics: User chose Replace mode");
                targetGraphicId = hasExistingGraphics ? item.GraphicId : nextGraphicId;
                targetDollGraphicId = isEquipment ? (hasExistingDollGraphics ? item.Spec1 : nextDollId) : null;
            }
            else // Append
            {
                // Use next available IDs
                FileLogger.LogInfo("OnImportItemGraphics: User chose Append mode");
                targetGraphicId = nextGraphicId;
                targetDollGraphicId = isEquipment ? nextDollId : null;
            }
        }
        else
        {
            // New item with no graphics - auto-append to next available slots
            FileLogger.LogInfo("OnImportItemGraphics: New item, auto-appending to next available slots");
            targetGraphicId = vm.GfxService.GetNextAvailableItemGraphicId();
            if (isEquipment)
            {
                var equipGfxType = GetEquipmentGfxType(item.Type);
                targetDollGraphicId = vm.GfxService.GetNextAvailableDollGraphicId(equipGfxType);
            }
        }
        
        FileLogger.LogInfo($"Target IDs: GraphicId={targetGraphicId}, DollGraphicId={targetDollGraphicId}");
        
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
        
        var result = await importService.ImportItemGraphicsAsync(
            vm.SelectedItem, inputFolder, outputGfxDir, targetGraphicId, targetDollGraphicId);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT: Successfully imported {result.FilesImported} files");
            
            // Update item properties with assigned IDs
            if (result.AssignedGraphicId.HasValue && result.AssignedGraphicId.Value != item.GraphicId)
            {
                FileLogger.LogInfo($"Updating item GraphicId: {item.GraphicId} -> {result.AssignedGraphicId.Value}");
                item.GraphicId = result.AssignedGraphicId.Value;
            }
            if (result.AssignedDollGraphicId.HasValue && isEquipment && result.AssignedDollGraphicId.Value != item.Spec1)
            {
                FileLogger.LogInfo($"Updating item Spec1: {item.Spec1} -> {result.AssignedDollGraphicId.Value}");
                item.Spec1 = result.AssignedDollGraphicId.Value;
            }
            
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
    
    private static GfxType GetEquipmentGfxType(Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType itemType)
    {
        return itemType switch
        {
            Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Weapon => GfxType.MaleWeapon, // Default to male for detection
            Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Shield => GfxType.MaleBack,
            Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Armor => GfxType.MaleArmor,
            Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Hat => GfxType.MaleHat,
            Moffat.EndlessOnline.SDK.Protocol.Pub.ItemType.Boots => GfxType.MaleBoots,
            _ => GfxType.Items
        };
    }
    
    private async void OnImportNpcGraphics(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FileLogger.LogInfo("OnImportNpcGraphics started");
        if (DataContext is not MainWindowViewModel vm || vm.SelectedNpc == null)
        {
            FileLogger.LogWarning("OnImportNpcGraphics: No selected NPC or invalid context");
            return;
        }
        
        var npc = vm.SelectedNpc;
        FileLogger.LogInfo($"Importing graphics for NPC: {npc.Name} (GraphicId={npc.GraphicId})");
        
        // Determine target graphic ID
        int? targetGraphicId = null;
        var hasExistingGraphics = npc.GraphicId > 0;
        
        if (hasExistingGraphics)
        {
            var nextGraphicId = vm.GfxService.GetNextAvailableDollGraphicId(GfxType.NPC);
            
            var dialog = new ImportModeDialog(
                currentSlot: npc.GraphicId,
                nextAvailableSlot: nextGraphicId,
                entityType: "NPC"
            );
            
            var mode = await dialog.ShowDialog<ImportMode>(this);
            
            if (mode == ImportMode.Cancel)
            {
                FileLogger.LogInfo("OnImportNpcGraphics: User canceled import mode dialog");
                return;
            }
            
            if (mode == ImportMode.Replace)
            {
                FileLogger.LogInfo("OnImportNpcGraphics: User chose Replace mode");
                targetGraphicId = npc.GraphicId;
            }
            else // Append
            {
                FileLogger.LogInfo("OnImportNpcGraphics: User chose Append mode");
                targetGraphicId = nextGraphicId;
            }
        }
        else
        {
            // New NPC - auto-append
            FileLogger.LogInfo("OnImportNpcGraphics: New NPC, auto-appending to next available slot");
            targetGraphicId = vm.GfxService.GetNextAvailableDollGraphicId(GfxType.NPC);
        }
        
        FileLogger.LogInfo($"Target GraphicId: {targetGraphicId}");
        
        // First, select the input folder with BMP files
        var inputFolder = await SelectExportFolderAsync("Select Folder Containing BMP Files");
        if (inputFolder == null) return;
        
        // Then, prompt for output GFX folder
        var outputChoice = await PromptForOutputGfxFolderAsync(vm.GfxService.GfxDirectory ?? "");
        if (outputChoice == null) return; // Canceled
        
        var outputGfxDir = string.IsNullOrEmpty(outputChoice) ? null : outputChoice;
        
        var importService = new GfxImportService(vm.GfxService);
        var result = await importService.ImportNpcGraphicsAsync(npc, inputFolder, outputGfxDir, targetGraphicId);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT (NPC): Successfully imported {result.FilesImported} files");
            
            // Update NPC GraphicId if changed
            if (result.AssignedGraphicId.HasValue && result.AssignedGraphicId.Value != npc.GraphicId)
            {
                FileLogger.LogInfo($"Updating NPC GraphicId: {npc.GraphicId} -> {result.AssignedGraphicId.Value}");
                npc.GraphicId = result.AssignedGraphicId.Value;
            }
            
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
        
        var spell = vm.SelectedSpell;
        FileLogger.LogInfo($"Importing graphics for Spell: {spell.Name} (GraphicId={spell.GraphicId})");
        
        // Determine target graphic ID
        int? targetGraphicId = null;
        var hasExistingGraphics = spell.GraphicId > 0;
        
        if (hasExistingGraphics)
        {
            var nextGraphicId = vm.GfxService.GetNextAvailableDollGraphicId(GfxType.Spells);
            
            var dialog = new ImportModeDialog(
                currentSlot: spell.GraphicId,
                nextAvailableSlot: nextGraphicId,
                entityType: "Spell"
            );
            
            var mode = await dialog.ShowDialog<ImportMode>(this);
            
            if (mode == ImportMode.Cancel)
            {
                FileLogger.LogInfo("OnImportSpellGraphics: User canceled import mode dialog");
                return;
            }
            
            if (mode == ImportMode.Replace)
            {
                FileLogger.LogInfo("OnImportSpellGraphics: User chose Replace mode");
                targetGraphicId = spell.GraphicId;
            }
            else // Append
            {
                FileLogger.LogInfo("OnImportSpellGraphics: User chose Append mode");
                targetGraphicId = nextGraphicId;
            }
        }
        else
        {
            // New spell - auto-append
            FileLogger.LogInfo("OnImportSpellGraphics: New spell, auto-appending to next available slot");
            targetGraphicId = vm.GfxService.GetNextAvailableDollGraphicId(GfxType.Spells);
        }
        
        FileLogger.LogInfo($"Target GraphicId: {targetGraphicId}");
        
        // First, select the input folder with BMP files
        var inputFolder = await SelectExportFolderAsync("Select Folder Containing BMP Files");
        if (inputFolder == null) return;
        
        // Then, prompt for output GFX folder
        var outputChoice = await PromptForOutputGfxFolderAsync(vm.GfxService.GfxDirectory ?? "");
        if (outputChoice == null) return; // Canceled
        
        var outputGfxDir = string.IsNullOrEmpty(outputChoice) ? null : outputChoice;
        
        var importService = new GfxImportService(vm.GfxService);
        var result = await importService.ImportSpellGraphicsAsync(spell, inputFolder, outputGfxDir, targetGraphicId);
        
        if (result.Success)
        {
            FileLogger.LogInfo($"GFX IMPORT (Spell): Successfully imported {result.FilesImported} files");
            
            // Update spell GraphicId if changed
            if (result.AssignedGraphicId.HasValue && result.AssignedGraphicId.Value != spell.GraphicId)
            {
                FileLogger.LogInfo($"Updating Spell GraphicId: {spell.GraphicId} -> {result.AssignedGraphicId.Value}");
                spell.GraphicId = (short)result.AssignedGraphicId.Value;
            }
            
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
