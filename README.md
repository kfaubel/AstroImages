# AstroImages

A modern WPF application for browsing and reviewing astronomical images and FITS files. Designed specifically for astrophotographers who need to quickly review image quality and manage their captures.

## ✨ Key Features

- **Dual-pane interface**: File list with image preview

- **Multi-format support**: FITS, XISF, JPEG, PNG, BMP, TIFF, GIF, and WebP files
- **FITS file support**: Native parsing and display of astronomical FITS files
- **XISF file support**: Full support for PixInsight's XISF format with metadata extraction
- **Filename parsing**: Extract metadata from structured filenames (perfect for NINA users)
- **FITS/XISF header display**: View and sort by FITS header keywords and XISF properties
- **File metadata viewer**: Click the 📋 button on any row to view comprehensive file metadata, headers, and image properties
- **Image navigation**: Keyboard shortcuts and slideshow mode
- **Zoom controls**: Fit-to-window, actual size, and custom zoom levels
- **Batch operations**: Select and move multiple files
- **Configuration persistence**: Remembers your settings and last folder
- **Help system**: Built-in documentation and splash screen

### Perfect for NINA Users

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

## 🚀 Development Setup

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
   cd AstroImages2
   ```

2. **Open in VS Code**
   - Open the folder in VS Code: `code .`
   - Install recommended extensions when prompted:
     - **C# Dev Kit** (includes C#, C# IntelliCode, and .NET Install Tool)
     - **XAML** (for XAML syntax highlighting and IntelliSense)
   - VS Code will automatically restore NuGet packages when you open a .csproj file

3. **Build and run**
   - Press `F5` to build and run with debugger (or use Command Palette: "Debug: Start Debugging")
   - Or press `Ctrl+F5` to run without debugger
   - Alternatively, use the terminal: `dotnet run --project AstroImages.Wpf`

### VS Code Configuration

VS Code will automatically generate launch configurations in `.vscode/launch.json` and tasks in `.vscode/tasks.json`. Key configurations:

- **Launch configurations**: Debug settings for running the application
- **Build tasks**: Automated build commands accessible via `Ctrl+Shift+P` → "Tasks: Run Task"
- **Settings**: Workspace-specific settings in `.vscode/settings.json`

### Command Line Development

```powershell
# Restore dependencies
dotnet restore

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

## 🛠️ Development Guide

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

### Architecture

This application follows the **MVVM (Model-View-ViewModel)** pattern with **Dependency Injection**:

- **Models**: Data structures (`FileItem`, `AppConfig`)
- **Views**: XAML files (`MainWindow.xaml`, etc.)
- **ViewModels**: UI logic and binding (`MainWindowViewModel`)
- **Services**: Business logic (file management, configuration, dialogs)


---

## 🔨 Building and Publishing

### Debug vs Release Builds

```powershell
# Debug build (default)
dotnet build

# Release build (optimized)
dotnet build --configuration Release

# Clean and rebuild
dotnet clean
dotnet build --configuration Release
```

**Debug builds** include:
- Debug symbols for debugging
- No optimizations
- Additional runtime checks

**Release builds** include:
- Optimized code
- Smaller file size
- No debug information

### Creating Distributable Releases

#### Option 1: Self-Contained Executable

```powershell
# Create single-file executable
dotnet publish AstroImages.Wpf -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Output location: AstroImages.Wpf/bin/Release/net8.0-windows/win-x64/publish/
```

#### Option 2: Framework-Dependent Deployment

```powershell
# Requires .NET 8 runtime on target machine (smaller download)
dotnet publish AstroImages.Wpf -c Release -r win-x64 --no-self-contained
```

### Publishing to GitHub Releases

1. **Create a release build**
   ```powershell
   dotnet publish AstroImages.Wpf -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

2. **Create GitHub release**
   - Go to your GitHub repository
   - Click "Releases" → "Create a new release"
   - Tag version (e.g., `v1.0.0`)
   - Upload the executable from the publish folder
   - Add release notes

3. **Automated releases** (optional)
   - Use GitHub Actions for automated builds
   - Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  release:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Publish
      run: dotnet publish AstroImages.Wpf -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
    - name: Create Release
      uses: actions/create-release@v1
      with:
        tag_name: ${{ github.ref }}
        release_name: Release ${{ github.ref }}
        files: AstroImages.Wpf/bin/Release/net8.0-windows/win-x64/publish/AstroImages.Wpf.exe
```

---

## 🔐 Code Signing

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

### Best Practices for Signing

1. **Protect your certificate**: Store in secure location, use strong passwords
2. **Use Hardware Security Modules (HSM)**: For enhanced security
3. **Timestamp all signatures**: Ensures validity after certificate expires
4. **Test signed executables**: Verify they run without warnings
5. **Automate the process**: Include signing in your build pipeline

---

## 🧪 Testing and Debugging

### Sample Data

The `TestData/` folder contains sample FITS files for testing:
- Various filters (R, G, B, H, O, S, L)
- Different exposure times and temperatures
- Filename parsing test cases

### Debugging Tips

1. **Use breakpoints**: Set breakpoints in ViewModels and Services
2. **Watch variables**: Use the Variables panel and Watch expressions
3. **Debug Console**: Check for binding errors and exceptions
4. **XAML debugging**: Limited XAML debugging compared to Visual Studio, but basic inspection available
5. **Performance profiling**: Use dotnet diagnostic tools or external profilers
6. **Problems panel**: View compiler errors, warnings, and linting issues

### Common Issues

- **Binding errors**: Check Output window for binding warnings
- **File access**: Ensure proper permissions for file operations
- **FITS parsing**: Verify FITS files are valid and accessible
- **Configuration**: Check AppData folder for config issues

---

## 📦 Dependencies

- **.NET 8**: Latest LTS version of .NET
- **Microsoft.Extensions.DependencyInjection**: For dependency injection
- **System.Text.Json**: For configuration serialization
- **WPF**: Windows Presentation Foundation (included in .NET)

---

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Follow MVVM patterns**: Keep business logic in services
4. **Add comprehensive comments**: Help other developers understand the code
5. **Test thoroughly**: Verify changes work with various FITS files
6. **Submit a pull request**: Describe your changes clearly

---

## 📄 License

MIT License - see LICENSE file for details.

---

## 🙏 Attribution

This application was developed with significant assistance from **GitHub Copilot**, demonstrating the collaborative potential between human creativity and AI-powered development tools.

## 📖 User Guide

### Getting Started

1. **Launch the application** - The splash screen will appear briefly
2. **Automatic folder restore** - Your last opened folder will be restored (if it still exists)
3. **Open a folder** - Use File → Open Folder (Ctrl+O) to browse to your image directory
4. **Configure parsing** - Set up custom keywords and FITS headers for your workflow

### Navigation and Viewing

- **File list**: Left pane shows all images with metadata columns
- **Image viewer**: Right pane displays the selected image
- **Zoom controls**: Use toolbar buttons or mouse wheel to zoom
- **Navigation**: Use arrow keys or navigation buttons to move between images
- **Slideshow**: Click Play button for automatic progression through images

### File Management

- **Selection**: Click on files to select them (multi-select with Ctrl+Click)
- **Batch operations**: Select multiple files for group operations
- **Move files**: Use File → Move Selected Files to relocate or organize images
- **Quality assessment**: Sort by RMS, HFR, or star count to identify best images
- **Metadata viewer**: Click the 📋 button next to any file to view detailed information:
  - **FITS files**: Complete header information with search/filter capabilities
  - **All files**: File properties, dimensions, creation dates
  - **Custom keywords**: Extracted values from filenames
  - **Export functionality**: Save all metadata to text files

### Configuration

**Options → Custom Keywords**: Configure filename parsing for your naming convention
**Options → FITS Keywords**: Select which FITS header values to display as columns
**Options → General**: Control application behavior and display preferences

### Keyboard Shortcuts

- **Ctrl+O**: Open Folder
- **Ctrl+M**: Move Selected Files
- **Space**: Play/Pause slideshow
- **↑/↓ Arrow Keys**: Navigate between images
- **+/-**: Zoom in/out
- **Ctrl+0**: Fit image to window
- **Ctrl+1**: Actual size (1:1)
- **F1**: Show help documentation



## 🔧 Configuration Features

### Filename Parsing

Perfect for NINA users or anyone with structured filenames:

1. **Configure keywords**: Go to Options → Custom Keywords
2. **Add your patterns**: Define keywords like "RMS", "HFR", "Stars", "EXPTIME"
3. **Test parsing**: Preview how your filenames will be parsed
4. **Auto-columns**: Parsed values automatically appear as sortable columns

**Example**: `2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits`
- Extracts: RMS=0.75, HFR=2.26, Stars=2029, Gain=100, Exposure=10.00s, Temp=-9.60C

### FITS Header Display

Display astronomical metadata directly from FITS files:

1. **Configure headers**: Go to Options → FITS Keywords
2. **Select common headers**: Choose from predefined astronomical keywords
3. **Add custom headers**: Type any FITS keyword name
4. **View as columns**: Headers appear as sortable columns in the file list

**Common FITS Keywords**:
- `OBJECT`: Target name
- `EXPTIME`: Exposure duration
- `FILTER`: Filter used
- `GAIN`: Camera gain setting
- `OFFSET`: Camera offset
- `FOCPOS`: Focuser position

### Supported File Formats

### Standard Image Formats
- **JPEG** (.jpg, .jpeg) - Standard photography format
- **PNG** (.png) - Lossless compression with transparency
- **BMP** (.bmp) - Windows bitmap format
- **TIFF** (.tif, .tiff) - High-quality archival format
- **GIF** (.gif) - Graphics Interchange Format
- **WebP** (.webp) - Modern web image format

### Astronomical Formats
- **FITS** (.fits, .fit, .fts) - Flexible Image Transport System
  - Full header parsing and display
  - Automatic histogram stretching
  - Scientific data preservation
  - Monochrome and color support

- **XISF** (.xisf) - Extensible Image Serialization Format
  - PixInsight's modern astronomical image format
  - XML-based metadata with rich properties
  - Support for UInt8/16/32 and Float32/64 data types
  - Comprehensive metadata extraction and display
  - Little-endian byte order (XISF native format)

## 🔬 FITS File Processing

Professional-grade handling of astronomical image data with a comprehensive custom FITS implementation:

### Core Features
- **Standards-compliant parser**: Custom FITS parser built to FITS 4.0 specifications
- **Complete data type support**: All FITS data types (8/16/32-bit integers, 32/64-bit floats)
- **Proper scaling**: Full BZERO/BSCALE parameter support per FITS standard
- **Big-endian handling**: Correct byte order conversion for all numeric data
- **Enhanced header parsing**: Robust keyword extraction with proper type conversion
- **File validation**: Multi-layer FITS structure and header validation
- **Error resilience**: Graceful handling of malformed or non-standard files

### Astronomical Features
- **Metadata extraction**: Automatic identification of 50+ astronomical keywords
- **WCS information**: World Coordinate System data extraction and validation
- **Image statistics**: Comprehensive statistics calculation (min/max/mean/stddev)
- **Format detection**: Smart pre-validation of FITS file structure
- **Debugging support**: Detailed diagnostic information and validation reports

### Performance & Quality
- **Memory optimization**: Efficient handling of large astronomical images
- **Fast parsing**: Minimal memory allocations and optimized algorithms
- **Auto-stretching**: Advanced histogram adjustment for optimal viewing
- **Background processing**: Non-blocking file operations
- **Smart validation**: Quick format detection before full processing

## 🏗️ Technology Stack

### Core Technologies
- **.NET 8**: Latest Long Term Support version of .NET
- **WPF**: Windows Presentation Foundation for rich desktop UI
- **C#**: Modern, type-safe programming language

### Architecture Patterns
- **MVVM**: Model-View-ViewModel for clean separation of concerns
- **Dependency Injection**: Loose coupling and testable design
- **Service Layer**: Business logic abstraction
- **Command Pattern**: User action handling

### Key Libraries
- **System.Text.Json**: High-performance JSON serialization
- **Microsoft.Extensions.DependencyInjection**: Built-in DI container
- **System.IO.MemoryMappedFiles**: Efficient large file handling

### FITS Processing Architecture
- **Custom FITS Parser**: Professional-grade implementation built from scratch
- **Standards Compliance**: Full adherence to FITS 4.0 specifications
- **Enhanced Utilities**: Comprehensive astronomical metadata processing
- **Zero Dependencies**: No external FITS libraries required

## License

MIT License

## Attribution

This application was developed with significant assistance from **GitHub Copilot**, an AI-powered code completion tool. The AI engine contributed to:

- Feature design and implementation
- Code architecture and best practices
- User interface development
- Documentation and README creation
- Bug fixes and optimizations
- FITS file processing implementation

The collaborative development between human creativity and AI assistance helped create a comprehensive astronomical image viewer with advanced features for the scientific imaging community.