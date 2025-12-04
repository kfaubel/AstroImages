# AstroImages

A modern WPF application for browsing and reviewing astronomical images and FITS files. Designed specifically for astrophotographers who need to quickly review image quality and manage their captures.

## Key Features

- **View FITS and XISF astronomical images** - Includes display of metadata for these file formats
- **View other common image foramts** - JPG, PNG, BMP, GIF, WEBP and TIFF
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View filename metadata as well as info in FITS/XISF headers
- **Select and move images** - Select images and then move them to a processing folder or the recycle bin
- **Image statistics and analysis** - Examine pixel statistics and image properties
- **Dark mode** - Switch between light and dark themes for comfortable viewing

### Support for NINA Users

If you use NINA (N.I.N.A. - Nighttime Imaging 'N' Astronomy) with structured specified filenames like:
```
$$SEQUENCETITLE$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS:$$RMS$$_HFR:$$HFR$$_Stars:$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
That generates filenames such as:
```
2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits
```

This app can parse RMS, HFR, star count, and other quality metrics directly from the filename for quick quality assessment.

---

## Installation

### System Requirements
- **Windows 10/11** (64-bit)
- **.NET 8 Desktop Runtime** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0) (choose "Desktop Runtime")

### Quick Install
1. **Download** the latest release ZIP from [GitHub Releases](https://github.com/kfaubel/AstroImages/releases)
2. **Extract** all files to a folder of your choice
3. **Run** `AstroImages.exe`

*First-time users: If you don't have .NET 8 Desktop Runtime, Windows will prompt you to install it automatically.*

---

## Development Setup

### Prerequisites

- **Visual Studio Code** (free, cross-platform editor)
  - Extensions: "C# Dev Kit" (includes C# extension and .NET debugging)
- **.NET 8 SDK** (download from Microsoft)
- **Windows 10/11** (WPF is Windows-only)
- **Git** for version control

### Quick Start

1. **Clone the repository**
   ```powershell
   git clone https://github.com/kfaubel/AstroImages.git
   cd AstroImages
   ```

2. **Open in VS Code**
   - Open the folder in VS Code: `code .`
   - Install recommended extensions when prompted:
     - **C# Dev Kit** (includes C#, C# IntelliCode, and .NET Install Tool)
     - **XAML** (for XAML syntax highlighting and IntelliSense)
   - VS Code will automatically restore NuGet packages when you open a .csproj file
   - VS Code will automatically generate launch configurations in `.vscode/launch.json` and tasks in `.vscode/tasks.json`.

3. **Build and run**
   - Press `F5` to build and run with debugger (or use Command Palette: "Debug: Start Debugging")
   - Or press `Ctrl+F5` to run without debugger
   - Alternatively, use the terminal: `dotnet run --project AstroImages.Wpf`

### Command Line Development

```powershell
# Restore dependencies
dotnet restore

# Remove derived files
dotnet clean

# Build the solution
dotnet build

# Run the application
dotnet run --project AstroImages.Wpf

# Build specific configuration
dotnet build --configuration Release

# Watch for changes and auto-rebuild
dotnet watch --project AstroImages.Wpf
```
---

## Development Guide

### Project Structure

```
AstroImages2/
├── AstroImages.Wpf/          # Main WPF application
│   ├── Views/                # XAML windows and user controls
│   ├── ViewModels/           # MVVM view models
│   ├── Services/             # Business logic services
│   ├── Models/               # Data models
│   └── App.xaml              # Application entry point
├── AstroImages.Core/         # Core library (FITS parsing, etc.)
├── AstroImages.Utils/        # Utility classes
└── TestData/                 # Sample FITS files for testing
```

---

### Creating Distributable Releases

#### Recommended: Framework-Dependent Deployment

```powershell
# Small, fast download - requires .NET 8 Desktop Runtime
dotnet publish AstroImages.Wpf -c Release -r win-x64 --no-self-contained
```

### Publishing to GitHub Releases

See the .gethub/wrokflow/release.yaml for setup to deploy a release to github 

#### Manually publish a new version

Update the version number in the project files in AstroImages.Wpf.cproj

```powershell
# Apply a tag starting with a 'v'
git tag v1.0.0

# Push the tag to the remote repository to trigger a release
git push origin v1.0.0
```

---

## Code Signing

### Why Sign Your Application?

- **Windows SmartScreen**: Unsigned apps trigger warnings
- **User trust**: Digital signatures verify authenticity
- **Professional distribution**: Required for some deployment scenarios

### Getting a Code Signing Certificate

#### Option 1: Commercial Certificate (Recommended)
- **Sectigo** (formerly Comodo): ~$200/year
- **DigiCert**: ~$400/year
- **GlobalSign**: ~$300/year

#### Option 2: Self-Signed Certificate (Testing Only)
```powershell
# Create self-signed certificate (development only)
New-SelfSignedCertificate -DnsName "YourCompany" -Type CodeSigning -CertStoreLocation cert:\CurrentUser\My
```

### Signing Your Application

Maybe: https://learn.microsoft.com/en-gb/windows/msix/

#### Using SignTool (Windows SDK)

```powershell
# Install Windows SDK or Visual Studio (includes SignTool)

# Sign the executable
signtool sign /f "certificate.pfx" /p "password" /fd SHA256 /tr http://timestamp.sectigo.com /td SHA256 "AstroImages.Wpf.exe"

# Verify signature
signtool verify /pa "AstroImages.Wpf.exe"
```

#### Using MSBuild (Automated)

Add to your `.csproj` file:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>certificate.pfx</AssemblyOriginatorKeyFile>
  <SignToolPath>C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe</SignToolPath>
</PropertyGroup>

<Target Name="SignOutput" AfterTargets="Publish">
  <Exec Command="&quot;$(SignToolPath)&quot; sign /f certificate.pfx /p $(CertPassword) /fd SHA256 &quot;$(PublishDir)AstroImages.Wpf.exe&quot;" />
</Target>
```

> **Note**: You can edit `.csproj` files directly in VS Code with XML syntax highlighting and IntelliSense support.

#### Timestamping

Always include timestamping when signing:
- **Sectigo**: `http://timestamp.sectigo.com`
- **DigiCert**: `http://timestamp.digicert.com`
- **GlobalSign**: `http://timestamp.globalsign.com/tsa/r6advanced1`

---

## User Guide

See the AstroImages.Wpf/Documentation/Help.md file or run the app and go to Help -> Documentation

### Keyboard Shortcuts

- **Ctrl+O**: Open Folder
- **Ctrl+M**: Move Selected Files
- **Space**: Play/Pause slideshow
- **↑/↓ Arrow Keys**: Navigate between images
- **+/-**: Zoom in/out
- **Ctrl+0**: Fit image to window
- **Ctrl+1**: Actual size (1:1)
- **F1**: Show help documentation

#

## License

MIT License - see LICENSE file for details.

---

## Attribution

This application was developed with significant assistance from **GitHub Copilot**, demonstrating the collaborative potential between human creativity and AI-powered development tools.  <-- Said the AI bot 😀

