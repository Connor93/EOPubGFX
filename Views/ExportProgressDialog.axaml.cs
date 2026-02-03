using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SOE_PubEditor.Views;

public partial class ExportProgressDialog : Window
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isComplete;
    
    public bool WasCancelled { get; private set; }
    
    public ExportProgressDialog()
    {
        InitializeComponent();
    }
    
    public void SetCancellationTokenSource(CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
    }
    
    public void UpdateProgress(string status, int current, int total)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusTextBlock.Text = status;
            
            if (total > 0)
            {
                double percentage = (double)current / total * 100;
                ExportProgressBar.Value = percentage;
                PercentageText.Text = $"{percentage:F1}%";
                CountText.Text = $"{current} / {total}";
            }
        });
    }
    
    public void SetCompleted(int totalExported, string outputFolder)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isComplete = true;
            StatusTextBlock.Text = $"Export completed! {totalExported} files exported.";
            ExportProgressBar.Value = 100;
            PercentageText.Text = "100%";
            CancelButton.Content = "Close";
        });
    }
    
    public void SetError(string error)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isComplete = true;
            StatusTextBlock.Text = $"Error: {error}";
            ExportProgressBar.Foreground = Avalonia.Media.Brushes.Red;
            CancelButton.Content = "Close";
        });
    }
    
    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isComplete)
        {
            Close();
        }
        else if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            WasCancelled = true;
            _cancellationTokenSource.Cancel();
            StatusTextBlock.Text = "Cancelling...";
            CancelButton.Content = "Close";
            _isComplete = true;
        }
        else
        {
            Close();
        }
    }
}
