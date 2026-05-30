namespace MusicDuplicateFilter.Views;

public partial class DeleteConfirmWindow : System.Windows.Window
{
    private DeleteConfirmWindow(List<string> filePaths)
    {
        InitializeComponent();
        HeaderText.Text = $"即将删除以下 {filePaths.Count} 个文件（将移至回收站），是否确认？";
        FileList.ItemsSource = filePaths
            .Select(p => new { FileName = System.IO.Path.GetFileName(p), FilePath = p })
            .ToList();
    }

    /// <summary>显示删除确认窗口，返回用户是否确认</summary>
    public static bool ShowConfirm(List<string> filePaths, System.Windows.Window? owner = null)
    {
        var window = new DeleteConfirmWindow(filePaths);
        if (owner != null) window.Owner = owner;
        return window.ShowDialog() == true;
    }

    private void ConfirmBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        => DialogResult = true;

    private void CancelBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        => DialogResult = false;
}
