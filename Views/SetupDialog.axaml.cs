using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace SOE_PubEditor.Views;

public partial class SetupDialog : Window
{
    public string? PubDirectory { get; private set; }
    public string? GfxDirectory { get; private set; }
    public bool WasCancelled { get; private set; } = true;
    
    public SetupDialog()
    {
        InitializeComponent();
    }
    
    public SetupDialog(string? existingPubDir, string? existingGfxDir) : this()
    {
        if (!string.IsNullOrEmpty(existingPubDir))
        {
            PubDirectory = existingPubDir;
            PubDirectoryTextBox.Text = existingPubDir;
        }
        if (!string.IsNullOrEmpty(existingGfxDir))
        {
            GfxDirectory = existingGfxDir;
            GfxDirectoryTextBox.Text = existingGfxDir;
        }
        UpdateOkButton();
    }
    
    private async void OnBrowsePubClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Pub File Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0)
        {
            PubDirectory = folders[0].Path.LocalPath;
            PubDirectoryTextBox.Text = PubDirectory;
            UpdateOkButton();
        }
    }
    
    private async void OnBrowseGfxClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select GFX Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0)
        {
            GfxDirectory = folders[0].Path.LocalPath;
            GfxDirectoryTextBox.Text = GfxDirectory;
            UpdateOkButton();
        }
    }
    
    private void UpdateOkButton()
    {
        OkButton.IsEnabled = !string.IsNullOrEmpty(PubDirectory) && !string.IsNullOrEmpty(GfxDirectory);
    }
    
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        WasCancelled = false;
        Close();
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        WasCancelled = true;
        Close();
    }
}
