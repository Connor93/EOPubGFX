using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SOE_PubEditor.Views;

/// <summary>
/// Enum for import mode selection result.
/// </summary>
public enum ImportMode
{
    Cancel,
    Replace,
    Append
}

/// <summary>
/// Dialog for choosing between Replace and Append modes when importing graphics.
/// </summary>
public partial class ImportModeDialog : Window
{
    private int _currentSlot;
    private int _nextAvailableSlot;
    private string _entityType = "Item";
    
    public ImportModeDialog()
    {
        InitializeComponent();
    }
    
    public ImportModeDialog(int currentSlot, int nextAvailableSlot, string entityType = "Item") : this()
    {
        _currentSlot = currentSlot;
        _nextAvailableSlot = nextAvailableSlot;
        _entityType = entityType;
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdateDescriptions();
    }
    
    /// <summary>
    /// Gets or sets the current doll graphic slot ID.
    /// </summary>
    public int CurrentSlot
    {
        get => _currentSlot;
        set
        {
            _currentSlot = value;
            UpdateDescriptions();
        }
    }
    
    /// <summary>
    /// Gets or sets the next available slot ID for appending.
    /// </summary>
    public int NextAvailableSlot
    {
        get => _nextAvailableSlot;
        set
        {
            _nextAvailableSlot = value;
            UpdateDescriptions();
        }
    }
    
    private void UpdateDescriptions()
    {
        if (ReplaceDescription != null)
            ReplaceDescription.Text = $"Overwrite graphics at slot {_currentSlot}";
        if (AppendDescription != null)
            AppendDescription.Text = $"Create at next available slot ({_nextAvailableSlot})";
    }
    
    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(ImportMode.Cancel);
    }
    
    private void OnImport(object? sender, RoutedEventArgs e)
    {
        var mode = ReplaceOption?.IsChecked == true ? ImportMode.Replace : ImportMode.Append;
        Close(mode);
    }
}
