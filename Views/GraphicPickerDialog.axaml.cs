using Avalonia.Controls;
using Avalonia.Interactivity;
using SOE_PubEditor.ViewModels;

namespace SOE_PubEditor.Views;

public partial class GraphicPickerDialog : Window
{
    public int? SelectedGraphicId { get; private set; }
    
    public GraphicPickerDialog()
    {
        InitializeComponent();
    }
    
    public GraphicPickerDialog(GraphicPickerViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Load graphics when the window opens
        Opened += (_, _) => viewModel.LoadGraphics();
    }
    
    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GraphicPickerViewModel vm && vm.SelectedGraphic != null)
        {
            SelectedGraphicId = vm.SelectedGraphic.Id;
            Close(true);
        }
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SelectedGraphicId = null;
        Close(false);
    }
}
