using System.Windows;
using MusicDuplicateFilter.ViewModels;

namespace MusicDuplicateFilter.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseWindowAction = () =>
        {
            DialogResult = true;
            Close();
        };
    }
}
