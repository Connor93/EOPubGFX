using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SOE_PubEditor.ViewModels;

namespace SOE_PubEditor.Views;

public partial class ConfigWindow : Window
{
    private MainWindowViewModel? _mainViewModel;
    
    public ConfigWindow()
    {
        InitializeComponent();
    }
    
    public ConfigWindow(MainWindowViewModel mainViewModel) : this()
    {
        _mainViewModel = mainViewModel;
        DataContext = mainViewModel;
    }
    
    private async void OnBrowsePubClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Pub Files Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0 && _mainViewModel != null)
        {
            var path = folders[0].Path.LocalPath;
            await _mainViewModel.LoadPubDirectoryAsync(path);
        }
    }
    
    private async void OnBrowseGfxClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select GFX Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0 && _mainViewModel != null)
        {
            var path = folders[0].Path.LocalPath;
            _mainViewModel.LoadGfxDirectory(path);
        }
    }
    
    private async void OnBrowseSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Save Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0 && _mainViewModel != null)
        {
            var path = folders[0].Path.LocalPath;
            _mainViewModel.SaveDirectory = path;
        }
    }
    
    private void OnClearSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_mainViewModel != null)
        {
            _mainViewModel.SaveDirectory = null;
        }
    }
    
    private async void OnReloadClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_mainViewModel != null && !string.IsNullOrEmpty(_mainViewModel.PubDirectory))
        {
            await _mainViewModel.LoadPubDirectoryAsync(_mainViewModel.PubDirectory);
        }
    }
    
    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
