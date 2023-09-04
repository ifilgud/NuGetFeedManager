using NuGet.Packaging.Core;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;

namespace NuGetFeedManager
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainWindowViewModel viewModel)
                viewModel.Logs.CollectionChanged += LogsOnCollectionChanged;
        }

        private void LogsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                LogsView.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
        }

        private void Feed_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView view)
                view.ScrollIntoView(view.SelectedItem);
        }

        private void FeedToCompare_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView && DataContext is MainWindowViewModel viewModel)
                viewModel.SelectedPackagesToUpdate = listView.SelectedItems.Cast<PackageIdentity>().ToList();
        }
    }
}