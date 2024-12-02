using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ModernMusicPlayer.ViewModels;

namespace ModernMusicPlayer.Views.Controls
{
    public partial class SearchBarControl : UserControl
    {
        public SearchBarControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is SearchViewModel viewModel)
            {
                viewModel.RefreshDisplayedTracks();
            }
        }
    }
}