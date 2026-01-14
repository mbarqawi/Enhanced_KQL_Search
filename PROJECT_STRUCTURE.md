# Project Structure

```
Enhanced_KQL_Search/
│
├── README.md                           # Main documentation
├── BUILD.md                            # Build instructions
├── QUICKSTART.md                       # Quick start guide
├── EXAMPLES.md                         # Usage examples and scenarios
├── PROJECT_STRUCTURE.md                # This file
├── Enhanced_KQL_Search.sln             # Visual Studio solution file
├── screenshot.jpg                      # Application screenshot
│
└── KustoSearchApp/                     # Main application project
    ├── KustoSearchApp.csproj           # Project file with dependencies
    ├── App.xaml                        # Application resources and styles
    ├── App.xaml.cs                     # Application entry point
    ├── MainWindow.xaml                 # Main window UI design
    ├── MainWindow.xaml.cs              # Main window logic and business code
    ├── TableSelectionWindow.xaml       # Table selection dialog UI
    ├── TableSelectionWindow.xaml.cs    # Table selection dialog logic
    ├── FuzzyMatcher.cs                 # Fuzzy string matching for table search
    ├── HighlightBehavior.cs            # Text highlighting behavior
    ├── Properties/
    │   ├── Settings.settings           # Application settings definition
    │   └── Settings.Designer.cs        # Auto-generated settings code
    │
    └── bin/Debug/net8.0-windows/       # Build output
        └── KustoSearchApp.exe          # Executable application
```

## Key Files

### Source Code Files

**App.xaml / App.xaml.cs**
- Application entry point and startup
- Global styles and resources (dark theme)
- Color definitions and button styles

**MainWindow.xaml / MainWindow.xaml.cs**
- Main application window UI
- Kusto connection management
- Search functionality implementation
- CSV export logic
- Connection history management
- Event handlers for all UI interactions

**TableSelectionWindow.xaml / TableSelectionWindow.xaml.cs**
- Table selection dialog
- Fuzzy search filtering
- Multi-select table list

**FuzzyMatcher.cs**
- Fuzzy string matching algorithm
- Used for table name filtering

**HighlightBehavior.cs**
- Text highlighting for search matches
- WPF attached behavior

**KustoSearchApp.csproj**
- Project configuration
- NuGet package dependencies:
  - Microsoft.Azure.Kusto.Data (v14.0.3)
  - Azure.Identity (v1.17.1)
  - CsvHelper (v33.1.0)
- Target framework: .NET 8.0 Windows

### Documentation Files

**README.md**
- Overview and features
- Installation instructions
- Usage guide
- Troubleshooting tips

**BUILD.md**
- Compilation instructions
- Build configurations
- Distribution options

**EXAMPLES.md**
- Common usage scenarios
- Sample searches
- Tips and best practices

## Build Output

The compiled application is located at:
```
KustoSearchApp\bin\Debug\net8.0-windows\KustoSearchApp.exe
```

For release builds:
```
KustoSearchApp\bin\Release\net8.0-windows\KustoSearchApp.exe
```

## Dependencies (NuGet Packages)

All dependencies are automatically downloaded during build:

1. **Microsoft.Azure.Kusto.Data** - Official Kusto client library
2. **Azure.Identity** - Azure Active Directory authentication
3. **CsvHelper** - CSV file generation and export
4. **Azure.Core** - Azure SDK core functionality (transitive)
5. **Microsoft.Identity.Client** - MSAL authentication (transitive)

## Opening in Visual Studio

Double-click `Enhanced_KQL_Search.sln` to open the entire solution in Visual Studio 2022.

## Opening in VS Code

```bash
cd Enhanced_KQL_Search
code .
```

Install recommended extension: C# Dev Kit

## File Sizes (Approximate)

- Source code: ~30 KB
- Compiled EXE: ~15 KB
- Total with dependencies: ~15 MB
