# AstroImages

A modern WPF application for browsing and reviewing astronomical images and FITS files. Designed specifically for astrophotographers who need to quickly review image quality and manage their subexposures.

## Key Features

- **View FITS and XISF astronomical images** - Includes display of metadata for these file formats
- **View other common image formats** - JPG, PNG, BMP, GIF, WEBP and TIFF
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View filename metadata as well as info in FITS/XISF headers
- **Select and move images** - Select images and then move them to a processing folder or the recycle bin
- **Image statistics and analysis** - Examine pixel statistics and image properties including median values (normalized or 16-bit)
- **Histogram display** - Optional histogram panel with linear and logarithmic scales
- **Floating windows** - Detach image and histogram viewers to separate windows for multi-monitor setups
- **Dark mode** - Switch between light and dark themes for comfortable viewing

## Support for NINA Users

If you use NINA (N.I.N.A. - Nighttime Imaging 'N' Astronomy) some useful data is not stored in the FITS header but can be added to the image file name with filenames like this in Options -> Imaging:
```
$$TARGETNAME$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS:$$RMSARCSEC$$_HFR:$$HFR$$_ECC_$$ECCENTRICITY$$_FWHM_$$FWHM$$_Stars_$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
That generates filenames such as:
```
M33 › NIGHT_2015-12-31 › LIGHT › 2016-01-01_12-00-00_L_RMS_0.35_HFR_3.25_ECC_0.66_FWHM_0.00_Stars_3294_1600_10.21s_-15C_0001
```

This app can parse RMS, HFR, ECCENTRICITY, FWHM, Stars, and other quality metrics directly from the filename for quick quality assessment. Custom keyword values are displayed in green in the file list. The Image Type, Filter, Exposure Time, Sensor Temperature, and other metadata are stored in the FITS file and displayed in blue when added as columns. The median value (shown in purple) can be displayed as normalized (0.0-1.0) or 16-bit range (0-65535) format.

---

## Installation

### System Requirements
- **Windows 10/11** (64-bit)
- **.NET 8 Desktop Runtime** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0) (choose "Desktop Runtime")

+### Installation

#### Option 1: Installer (Recommended)
1. **Download** `AstroImages-Setup-x.x.x.exe` from [GitHub Releases](https://github.com/kfaubel/AstroImages/releases)
2. **Run** the installer
3. The installer will:
   - Check for .NET 8 Desktop Runtime (offers to download if missing)
   - Install to `%LOCALAPPDATA%\AstroImages`
   - Create Start Menu shortcuts
   - Optionally create a Desktop shortcut

#### Option 2: Portable ZIP
1. **Download** `AstroImages-win-x64.zip` from [GitHub Releases](https://github.com/kfaubel/AstroImages/releases)
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

See the [.github/workflows/release.yml](.github/workflows/release.yml) for the automated release workflow.

The workflow automatically creates both a ZIP file and an installer when you push a version tag.

#### Release Checklist

Follow these steps to publish a new version:

1. **Update Version Numbers** (use the same version in all locations):
   - [ ] `AstroImages.Wpf/AstroImages.Wpf.csproj` - Update `<Version>` property (e.g., `1.5.0`)
   - [ ] `installer.iss` - Update `#define MyAppVersion` (e.g., `"1.5.0"`)

2. **Update Release Notes**:
   - [ ] Edit `RELEASE_NOTES.txt` with new features, improvements, and bug fixes
   - [ ] Update version number in the first line of `RELEASE_NOTES.txt`

3. **Test Locally** (optional but recommended):
   ```powershell
   # Build and test the application
   dotnet build -c Release
   dotnet run --project AstroImages.Wpf -c Release
   
   # Test the installer build (requires Inno Setup installed)
   dotnet publish AstroImages.Wpf -c Release -r win-x64 --no-self-contained --output ./publish
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
   ```

4. **Commit Changes**:
   ```powershell
   git add .
   git commit -m "Release v1.5.0"
   ```

5. **Create and Push Tag**:
   ```powershell
   # Create a version tag (must start with 'v')
   git tag v1.5.0
   
   # Push commits and tag to trigger GitHub Actions
   git push origin main
   git push origin v1.5.0
   
   # Or push everything at once:
   git push origin main --tags
   ```

6. **Monitor GitHub Actions**:
   - [ ] Go to https://github.com/kfaubel/AstroImages/actions
   - [ ] Wait for the build to complete (~5-10 minutes)
   - [ ] Check for any errors in the workflow

7. **Verify Release**:
   - [ ] Go to https://github.com/kfaubel/AstroImages/releases
   - [ ] Verify both `AstroImages-win-x64.zip` and `AstroImages-Setup-x.x.x.exe` are present
   - [ ] Download and test the installer
   - [ ] Check that release notes are displayed correctly

**Quick Release Command** (after completing steps 1-2):
```powershell
git add . && git commit -m "Release v1.5.0" && git tag v1.5.0 && git push origin main --tags
```

### Building the Installer Locally (Testing)

To test the installer before pushing a release:

1. **Prerequisites**: Install Inno Setup from [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php)

2. **Build and create installer**:
   ```powershell
   # Build the application
   dotnet publish AstroImages.Wpf -c Release -r win-x64 --no-self-contained --output ./publish
   
   # Compile the installer (command line)
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
   
   # Or open installer.iss in Inno Setup GUI and click Build -> Compile
   ```

3. **Test the installer**: Find it at `installer-output/AstroImages-Setup-x.x.x.exe`

---

## Code Signing (Future)

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

### Third-Party Components

This application uses the following open-source libraries:

- **[MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet)** (v2.8.1) by Drew Noakes - Apache License 2.0
  - Used for extracting EXIF and other metadata from image files

### Development Tools

This application was developed with significant assistance from **GitHub Copilot**, demonstrating the collaborative potential between human creativity and AI-powered development tools.  <-- Said the AI bot 😀

