using System.Windows;

namespace NuGetFeedManager
{
    /// <summary>
    /// Lógica de interacción para ChoosePackageVersion.xaml
    /// </summary>
    public partial class ChoosePackageVersion : Window
    {
        public ChoosePackageVersion()
        {
            InitializeComponent();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChoosePackageVersionViewModel viewModel)
            {
                viewModel.ChosenVersion = null;
            }

            Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
