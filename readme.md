## Setting up the development environment

Open Email for Windows is built by Windows App SDK and WinUI3. Here are the steps to be able to configure your development environment to build the project properly:

> [!NOTE]
> Open Email for Windows requires [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or later to build and Windows 10 or later to execute. If you're building an app with WinUI and Windows App SDK for the first time, follow these [installation instructions](https://learn.microsoft.com/windows/apps/get-started/start-here).

### Install requires Visual Studio components

**Required [Visual Studio components](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=vs-2022-17-10#required-workloads-and-components):**
- Windows application development

Make sure to install this component during Visual Studio 2022 installation.

### Download and Install Windows App SDK from Experimental channel

Currently Open Email for Windows uses WinAppSDK 1.7-experimental2 from channel. This may change in the future once the functionality needed in the application will be released in stable channel.

[Click here](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads#experimental-release) to go to WinAppSDK downloads page. Locate the version "1.7 Experimental2 (1.7.250109001-experimental2)" and install it.

### Add Community Toolkit Labs Nuget Source

Go to Visual Studio -> Tools -> Options -> Nuget Package Manager -> Package Sources menu. Add a new nuget source with the following URL:

https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json

This will make sure some of the required components from Community Toolkit's experimental channel are imported for project to use.

### Clone the repository and launch

```shell
git clone https://github.com/Open-Email/OpenEmail-Windows.git
```

You can now proceed to launch OpenEmail.sln. In the Visual Studio, make sure to select Debug configuration with your CPU architecture (x86, x64, ARM64) and OpenEmail (Package) launch option.
