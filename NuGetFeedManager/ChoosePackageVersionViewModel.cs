using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NuGet.Packaging.Core;

namespace NuGetFeedManager
{
    public class ChoosePackageVersionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PackageIdentity> PackageList { get; set; } = new ObservableCollection<PackageIdentity>();

        public PackageIdentity ChosenVersion { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
