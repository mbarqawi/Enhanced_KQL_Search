# Build Instructions

## Quick Start

### Run the Application

```bash
cd KustoSearchApp
dotnet run
```

The application will build and launch automatically.

## Build Only

```bash
cd KustoSearchApp
dotnet build -c Release
```

Executable location: `bin\Release\net8.0-windows\KustoSearchApp.exe`

## Create Standalone Executable

For distribution without requiring .NET installation:

```bash
cd KustoSearchApp
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\KustoSearchApp.exe`

## Development Build

```bash
cd KustoSearchApp
dotnet build
dotnet run
```

## Clean Build

```bash
cd KustoSearchApp
dotnet clean
dotnet build -c Release
```

## Prerequisites

- .NET 8.0 SDK (download from https://dotnet.microsoft.com/download)
- Windows 10 or later
- Visual Studio 2022 (optional, for GUI development)

## Verify Installation

```bash
dotnet --version
```

Should show version 8.0.x or later.

## NuGet Package Restore

Packages are automatically restored during build. To manually restore:

```bash
cd KustoSearchApp
dotnet restore
```

## Troubleshooting Build Issues

### Missing SDK

```bash
dotnet --list-sdks
```

If .NET 8.0 is not listed, download and install from Microsoft.

### NuGet Package Errors

Clear the NuGet cache:

```bash
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### Build Errors

View detailed error messages:

```bash
dotnet build -v detailed
```
