# NuGetFeedManager
Simple application to manager your private NuGet feed on TFS or Azure DevOps. It allows push packages and updating them without having to download manually packages.

I created it because we have an on-premise TFS in the company with a private feed because and the build environment and deployment are in isolated network without internet access.

For those cases, when you want to push packages to the feed or update them, you have to do everything using command line, so this application helps you keeping it updated.

On the left panel you connect to your private feed and the in the right panel, the one to compare to, usually the official nuget repository.

You can check for updates for single packages or for all of them, and it takes a while.

In the app.config file you can set the default feeds uris and credentials for private feed to avoid having to set it everytime you launch the application.
