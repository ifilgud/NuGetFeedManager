using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using CommunityToolkit.Mvvm.Input;

namespace NuGetFeedManager
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILogger _logger;
        private readonly List<Lazy<INuGetResourceProvider>> _providers;
        private const int PageSize = 500;
        private bool _connected;

        public MainWindowViewModel()
        {
            Logs = new ObservableCollection<LogEntry>();
            FeedPackages = new ObservableCollection<PackageIdentity>();
            PackagesToUpdate = new ObservableCollection<PackageIdentity>();
            SelectedPackagesToUpdate = new List<PackageIdentity>();

            ConnectToFeedCommand = new AsyncRelayCommand(ConnectToFeed);
            ConnectToUpdateFeedCommand = new AsyncRelayCommand(ConnectToUpdateFeed);
            CancelUpdateCheckCommand = new RelayCommand(() => CheckingUpdates = false);
            UpdatePackageCommand = new AsyncRelayCommand(async () => await UpdatePackage(SelectedPackage, SelectedPackageToUpdate));
            UpdatePackageToVersionCommand = new AsyncRelayCommand(async () => await UpdatePackage(SelectedPackage, null));
            UpdatePackagesCommand = new AsyncRelayCommand(async () => await UpdatePackages());
            PushPackageCommand = new AsyncRelayCommand(PushPackage);
            CheckUpdatesPackageCommand = new AsyncRelayCommand(CheckUpdatesForSelectedPackage);
            SearchAndPushCommand = new AsyncRelayCommand(async () => await UpdatePackage(null, null));

            _logger = new Logger(Logs);
            _providers = new List<Lazy<INuGetResourceProvider>>();
            _providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API support

            FeedUri = Properties.Settings.Default.FeedUri;
            FeedToCompareUri = Properties.Settings.Default.FeedToCompareUri;
            IncludePreReleaseFeed = Properties.Settings.Default.IncludePreRelease;
            IncludePreReleaseUpdateFeed = Properties.Settings.Default.IncludePreRelease;
            FeedApiKey = Properties.Settings.Default.FeedApiKey;
            UserName = Properties.Settings.Default.UserName;
            Token = Properties.Settings.Default.Token;
        }

        public ObservableCollection<LogEntry> Logs { get; set; }

        private string _userName;

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _token;

        public string Token
        {
            get => _token;
            set
            {
                _token = value;
                OnPropertyChanged();
            }
        }

        private string _feedUri;
        public string FeedUri
        {
            get => _feedUri;
            set
            {
                _feedUri = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConnectToFeed));
            }
        }

        private string _feedToCompareUri;
        public string FeedToCompareUri
        {
            get => _feedToCompareUri;
            set
            {
                _feedToCompareUri = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConnectToUpdateFeed));
            }
        }

        public string FeedApiKey { get; set; }

        public ObservableCollection<PackageIdentity> FeedPackages { get; set; }
        public ObservableCollection<PackageIdentity> PackagesToUpdate { get; set; }

        private bool _readingFeed;
        public bool ReadingFeed
        {
            get => _readingFeed;
            set
            {
                _readingFeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConnectToFeed));
                OnPropertyChanged(nameof(CanConnectToUpdateFeed));
                OnPropertyChanged(nameof(CanPushPackage));
                OnPropertyChanged(nameof(FeedPackages));
            }
        }

        private bool _checkingUpdates;
        public bool CheckingUpdates
        {
            get => _checkingUpdates;
            set
            {
                _checkingUpdates = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConnectToUpdateFeed));
                OnPropertyChanged(nameof(CanPushPackage));
            }
        }

        private bool _updateInProgress;
        public bool UpdateInProgress
        {
            get => _updateInProgress;
            set
            {
                _updateInProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpdatePackage));
                OnPropertyChanged(nameof(CanUpdatePackageToVersion));
                OnPropertyChanged(nameof(CanPushPackage));
            }
        }

        public bool CanConnectToFeed => !string.IsNullOrEmpty(FeedUri)
                                        && Uri.IsWellFormedUriString(FeedUri, UriKind.Absolute)
                                        && !ReadingFeed;
        public bool CanConnectToUpdateFeed => !string.IsNullOrEmpty(FeedToCompareUri)
                                              && Uri.IsWellFormedUriString(FeedToCompareUri, UriKind.Absolute)
                                              && FeedPackages.Any()
                                              && !CheckingUpdates;

        public bool CanUpdatePackage => SelectedPackageToUpdate != null && !UpdateInProgress && !CanUpdatePackages;
        public bool CanUpdatePackageToVersion => SelectedPackage != null && !UpdateInProgress && !CanUpdatePackages;
        public bool CanUpdatePackages => SelectedPackagesToUpdate.Count > 1 && !UpdateInProgress;
        public bool CanCheckUpdatePackage => SelectedPackage != null && !UpdateInProgress && !CanUpdatePackages;
        public bool CanPushPackage => _connected && !_readingFeed && !CheckingUpdates && !UpdateInProgress;

        private bool _includePreReleaseFeed;
        public bool IncludePreReleaseFeed
        {
            get => _includePreReleaseFeed;
            set
            {
                _includePreReleaseFeed = value;
                if (FeedPackages.Any())
                {
                    FeedPackages.Clear();
                    Task.Run(async () => await ConnectToFeed());
                }
            }
        }

        private bool _includePreReleaseUpdateFeed;
        public bool IncludePreReleaseUpdateFeed
        {
            get => _includePreReleaseUpdateFeed;
            set
            {
                _includePreReleaseUpdateFeed = value;
                if (PackagesToUpdate.Any())
                {
                    PackagesToUpdate.Clear();
                    Task.Run(async() => await ConnectToUpdateFeed());
                }
            }
        }

        public AsyncRelayCommand ConnectToFeedCommand { get; set; }
        public AsyncRelayCommand ConnectToUpdateFeedCommand { get; set; }
        public RelayCommand CancelUpdateCheckCommand { get; set; }
        public AsyncRelayCommand UpdatePackageCommand { get; set; }
        public AsyncRelayCommand UpdatePackageToVersionCommand { get; set; }
        public AsyncRelayCommand UpdatePackagesCommand { get; set; }
        public AsyncRelayCommand PushPackageCommand { get; set; }
        public AsyncRelayCommand CheckUpdatesPackageCommand { get; set; }
        public AsyncRelayCommand SearchAndPushCommand { get; set; }

        private PackageIdentity _selectedPackage;

        public PackageIdentity SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                _selectedPackage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCheckUpdatePackage));
                OnPropertyChanged(nameof(CanUpdatePackageToVersion));
            }
        }

        private PackageIdentity _selectedPackageToUpdate;

        public PackageIdentity SelectedPackageToUpdate
        {
            get => _selectedPackageToUpdate;
            set
            {
                _selectedPackageToUpdate = value;
                if (value != null)
                {
                    SelectedPackage = FeedPackages.FirstOrDefault(p => p.Id == value.Id);
                    OnPropertyChanged(nameof(SelectedPackage));
                }

                OnPropertyChanged(nameof(CanUpdatePackage));
            }
        }

        private IList<PackageIdentity> _selectedPackagesToUpdate;

        public IList<PackageIdentity> SelectedPackagesToUpdate
        {
            get => _selectedPackagesToUpdate;

            set
            {
                _selectedPackagesToUpdate = value;
                OnPropertyChanged(nameof(CanUpdatePackage));
                OnPropertyChanged(nameof(CanUpdatePackages));
            }
        }

        public string PackageToSearch { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public async Task ConnectToFeed()
        {
            ReadingFeed = true;

            FeedPackages.Clear();

            try
            {
                var packageSource = new PackageSource(FeedUri);
                if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token))
                    packageSource.Credentials = PackageSourceCredential.FromUserInput(FeedUri, UserName, Token, true, "");
                var sourceRepository = new SourceRepository(packageSource, _providers);
                var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
                IList<IPackageSearchMetadata> searchMetadata;

                int currentPage = 0;
                do
                {
                    searchMetadata = (await searchResource.SearchAsync("", new SearchFilter(IncludePreReleaseFeed),
                        PageSize * currentPage, PageSize, _logger, CancellationToken.None)).ToList();

                    foreach (IPackageSearchMetadata packageSearchMetadata in searchMetadata)
                    {
                        FeedPackages.Add(packageSearchMetadata.Identity);
                    }

                    currentPage++;
                } while (searchMetadata.Count == PageSize);

                _connected = true;
            }
            catch (FatalProtocolException e)
            {
                if (e.InnerException != null && e.InnerException.Message.Contains("401"))
                {
                    MessageBox.Show("Authorization error. Fill or check credentials");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                ReadingFeed = false;
            }

            OnPropertyChanged(nameof(CanPushPackage));
        }

        public async Task ConnectToUpdateFeed()
        {
            CheckingUpdates = true;

            var packageSource = new PackageSource(FeedToCompareUri);
            var sourceRepository = new SourceRepository(packageSource, _providers);
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

            PackagesToUpdate.Clear();

            foreach (PackageIdentity packageIdentity in FeedPackages)
            {
                if (!CheckingUpdates)
                    return;

                IList<IPackageSearchMetadata> foundPackage = (await metadataResource.GetMetadataAsync(packageIdentity.Id,
                    IncludePreReleaseUpdateFeed, false, new SourceCacheContext(),
                    _logger, CancellationToken.None)).ToList();

                if (foundPackage.Any())
                {
                    PackageSearchMetadataRegistration latestVersionOfPackage = foundPackage.Cast<PackageSearchMetadataRegistration>().OrderByDescending(p => p.Version).First();
                    if (latestVersionOfPackage.Version > packageIdentity.Version)
                    {
                        PackagesToUpdate.Add(latestVersionOfPackage.Identity);
                    }
                }
            }

            CheckingUpdates = false;
        }

        public async Task<PackageIdentity> ChooseVersion(PackageIdentity existingPackage)
        {
            var chooseVersionDialog = new ChoosePackageVersion();

            if (!(chooseVersionDialog.DataContext is ChoosePackageVersionViewModel viewModel))
                return null;

            var packageSource = new PackageSource(FeedToCompareUri);
            var sourceRepository = new SourceRepository(packageSource, _providers);
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

            IList<PackageSearchMetadataRegistration> foundPackages = (await metadataResource.GetMetadataAsync(existingPackage != null ? existingPackage.Id : PackageToSearch.Trim(),
                IncludePreReleaseUpdateFeed, false, new SourceCacheContext(),
                _logger, CancellationToken.None)).Cast<PackageSearchMetadataRegistration>().ToList();

            if (foundPackages.Any())
            {
                viewModel.PackageList.AddRange(foundPackages.Select(fp => fp.Identity).OrderByDescending(fp => fp.Version));
            }

            if (viewModel.PackageList.Any())
                chooseVersionDialog.ShowDialog();
            
            return viewModel.ChosenVersion;
        }

        public async Task UpdatePackage(PackageIdentity existingPackage, PackageIdentity packageToUpdate)
        {
            if (packageToUpdate == null)
                packageToUpdate = await ChooseVersion(existingPackage);

            if (packageToUpdate == null)
                return;

            UpdateInProgress = true;

            var packageSource = new PackageSource(FeedToCompareUri);
            var sourceRepository = new SourceRepository(packageSource, _providers);
            var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>();

            var destinationPackageSource = new PackageSource(FeedUri);

            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token))
                destinationPackageSource.Credentials = PackageSourceCredential.FromUserInput(FeedUri, UserName, Token, true, "");
            var destinationSourceRepository = new SourceRepository(destinationPackageSource, _providers);
            var packageUpdateResource = await destinationSourceRepository.GetResourceAsync<PackageUpdateResource>();

            const string downloadFolder = "DownloadedPackages";

            bool noErrors = true;

            try
            {
                var downloadResourceResult = await downloadResource.GetDownloadResourceResultAsync(packageToUpdate,
                    new PackageDownloadContext(new NullSourceCacheContext(), downloadFolder, true), string.Empty, _logger, CancellationToken.None);

                string packagePath = Path.Combine(Environment.CurrentDirectory, downloadFolder);
                downloadResourceResult.PackageStream.CopyToFile(packagePath + Path.DirectorySeparatorChar + packageToUpdate + ".nupkg");

                await packageUpdateResource.Push(new List<string> { packagePath + Path.DirectorySeparatorChar + packageToUpdate + ".nupkg" },
                    string.Empty, 300, false, s => FeedApiKey, null, true, true, null, _logger);
            }
            catch (Exception e)
            {
                noErrors = false;
                MessageBox.Show("Error updating package: " + e);
            }
            finally
            {
                if (noErrors)
                {
                    // Refresh the updated package
                    var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
                    PackageSearchMetadataRegistration updatedPackage = (await packageMetadataResource.GetMetadataAsync(
                            packageToUpdate.Id, IncludePreReleaseFeed, false, new SourceCacheContext(),
                            _logger, CancellationToken.None))
                        .Cast<PackageSearchMetadataRegistration>()
                        .OrderByDescending(p => p.Version).First(p => p.Version == packageToUpdate.Version);

                    int position = FeedPackages.IndexOf(existingPackage);
                    if (position >= 0)
                    {
                        FeedPackages.RemoveAt(position);
                        FeedPackages.Insert(position, updatedPackage.Identity);
                    }
                    else
                    {
                        FeedPackages.Add(updatedPackage.Identity);
                        // Reorder
                        FeedPackages = new ObservableCollection<PackageIdentity>(FeedPackages.OrderBy(p => p.Id));
                        OnPropertyChanged(nameof(FeedPackages));
                    }

                    SelectedPackage = updatedPackage.Identity;
                    OnPropertyChanged(nameof(SelectedPackage));

                    PackagesToUpdate.Remove(packageToUpdate);
                }

                UpdateInProgress = false;
            }
        }

        public async Task UpdatePackages()
        {
            foreach (PackageIdentity packageToUpdate in SelectedPackagesToUpdate)
            {
                await UpdatePackage(FeedPackages.FirstOrDefault(p => p.Id == packageToUpdate.Id), packageToUpdate);
            }
        }

        public async Task PushPackage()
        {
            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "NuGet package file|*.nupkg",
                Title = "Select package file to push",
                RestoreDirectory = true
            };

            bool? fileSelected = openFileDialog.ShowDialog();
            if (fileSelected.HasValue && fileSelected.Value)
            {
                ReadingFeed = true;

                var destinationPackageSource = new PackageSource(FeedUri);
                if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token))
                    destinationPackageSource.Credentials = PackageSourceCredential.FromUserInput(FeedUri, UserName, Token, true, "");
                var destinationSourceRepository = new SourceRepository(destinationPackageSource, _providers);
                var packageUpdateResource = await destinationSourceRepository.GetResourceAsync<PackageUpdateResource>();

                try
                {
                    await packageUpdateResource.Push(new List<string> { openFileDialog.FileName }, string.Empty,
                        300, false, s => FeedApiKey, null, true, true, null, _logger);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error updating package: " + e);
                }
                finally
                {
                    // Refresh the feed
                    ReadingFeed = false;
                    await ConnectToFeed();
                }
            }
        }

        public async Task CheckUpdatesForSelectedPackage()
        {
            CheckingUpdates = true;

            var packageSource = new PackageSource(FeedToCompareUri);
            var sourceRepository = new SourceRepository(packageSource, _providers);
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

            PackagesToUpdate.Clear();

            IList<IPackageSearchMetadata> foundPackage = (await metadataResource.GetMetadataAsync(SelectedPackage.Id,
                IncludePreReleaseUpdateFeed, false, new SourceCacheContext(),
                _logger, CancellationToken.None)).ToList();

            if (foundPackage.Any())
            {
                PackageSearchMetadataRegistration latestVersionOfPackage = foundPackage.Cast<PackageSearchMetadataRegistration>().OrderByDescending(p => p.Version).First();
                if (latestVersionOfPackage.Version > SelectedPackage.Version)
                {
                    PackagesToUpdate.Add(latestVersionOfPackage.Identity);
                }
            }

            CheckingUpdates = false;
        }

        public async Task SearchAndPushPackage()
        {
            if (string.IsNullOrEmpty(PackageToSearch))
                return;

            ReadingFeed = true;

            PackageIdentity chosenPackage = await ChooseVersion(null);

            var destinationPackageSource = new PackageSource(FeedUri);
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token))
                destinationPackageSource.Credentials = PackageSourceCredential.FromUserInput(FeedUri, UserName, Token, true, "");
            var destinationSourceRepository = new SourceRepository(destinationPackageSource, _providers);
            var packageUpdateResource = await destinationSourceRepository.GetResourceAsync<PackageUpdateResource>();

            try
            {
                //await packageUpdateResource.Push(new List<string> { openFileDialog.FileName }, string.Empty,
                    //300, false, s => FeedApiKey, null, true, true, null, _logger);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error updating package: " + e);
            }
            finally
            {
                // Refresh the feed
                ReadingFeed = false;
                await ConnectToFeed();
            }
        }
    }
}
