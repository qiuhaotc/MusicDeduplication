using System.Windows;
using MusicDuplicateFilter.ViewModels;

namespace MusicDuplicateFilter.Views;

public partial class DuplicatePreviewWindow : Window
{
    public DuplicatePreviewWindow(DuplicatePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseWindowAction = () => Close();
    }
}
