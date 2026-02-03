using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using SOE_PubEditor.Models;
using SOE_PubEditor.ViewModels;
using SOE_PubEditor.Views;

namespace SOE_PubEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();
            
            // Check if we need initial setup
            var settings = AppSettings.Load();
            
            if (string.IsNullOrEmpty(settings.PubDirectory) || string.IsNullOrEmpty(settings.GfxDirectory))
            {
                // Show setup dialog as main window first
                var setupDialog = new SetupDialog(settings.PubDirectory, settings.GfxDirectory);
                setupDialog.Closed += (s, e) =>
                {
                    if (setupDialog.WasCancelled)
                    {
                        desktop.Shutdown();
                        return;
                    }
                    
                    // Save the directories
                    settings.PubDirectory = setupDialog.PubDirectory;
                    settings.GfxDirectory = setupDialog.GfxDirectory;
                    settings.Save();
                    
                    // Now show main window
                    var mainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                };
                desktop.MainWindow = setupDialog;
            }
            else
            {
                // Directories already set, show main window directly
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}