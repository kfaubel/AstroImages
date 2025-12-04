# AstroImages - Application Architecture

## Table of Contents
- [Overview](#overview)
- [MVVM Pattern Implementation](#mvvm-pattern-implementation)
- [Dependency Injection](#dependency-injection)
- [Class Diagrams](#class-diagrams)
- [Sequence Diagrams](#sequence-diagrams)
- [Threading Model](#threading-model)
- [Key Design Patterns](#key-design-patterns)

---

## Overview

AstroImages is a WPF desktop application built with .NET 8.0 following the **Model-View-ViewModel (MVVM)** architectural pattern. The application uses **Dependency Injection** for loose coupling and testability, and implements parallel processing for optimal performance.

### Technology Stack
- **.NET 8.0** - Latest LTS framework
- **WPF** - Windows Presentation Foundation for rich desktop UI
- **MVVM** - Architectural pattern for separation of concerns
- **Dependency Injection** - Manual DI pattern (constructor injection)
- **Parallel Processing** - Multi-threaded file processing with `Parallel.ForEach`
- **WPF Data Binding** - Two-way binding with `INotifyPropertyChanged`

---

## MVVM Pattern Implementation

### What is MVVM?

MVVM (Model-View-ViewModel) is an architectural pattern that separates the application into three distinct layers:

```
┌────────────────────────────────────────────────────────────┐
│                         MVVM Pattern                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────┐          ┌──────────┐          ┌──────────┐  │
│  │          │  Binding │          │  Updates │          │  │
│  │   View   │◄────────►│ ViewModel│◄────────►│  Model   │  │
│  │          │          │          │          │          │  │
│  └──────────┘          └──────────┘          └──────────┘  │
│   (XAML +              (UI Logic +           (Data +       │
│   Code-Behind)         Commands)             Business      │
│                                              Logic)        │
│                                                            │
│  User Interaction  →  Commands/Events    →  Data Changes   │
│  Data Changes      →  Property Notify    →  UI Updates     │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### MVVM Components in AstroImages

#### 1. **Model Layer**
Represents data and business logic. Models are simple data containers that implement `INotifyPropertyChanged` when UI binding is needed.

**Key Models:**
- `FileItem` - Represents an image file with metadata
- `AppConfig` - Application configuration and settings
- `UpdateInfo` - Information about available updates

```csharp
// Example: FileItem Model
public class FileItem : INotifyPropertyChanged
{
    public string Name { get; set; }
    public long Size { get; set; }
    public string Path { get; set; }
    public Dictionary<string, string> CustomKeywords { get; set; }
    public Dictionary<string, string> FitsKeywords { get; set; }
    public bool IsSelected { get; set; }  // Implements property change notification
}
```

#### 2. **View Layer**
The user interface defined in XAML and code-behind. Views are responsible for:
- Displaying data through data binding
- Handling UI-specific logic (animations, visual states)
- Raising events for ViewModel to handle

**Key Views:**
- `MainWindow.xaml` - Primary application window
- `SplashWindow.xaml` - Startup splash screen
- `DocumentationWindow.xaml` - Help documentation viewer
- `MoveFilesDialog.xaml` - File move/organize dialog
- Various configuration dialogs

```xaml
<!-- Example: View Data Binding -->
<TextBlock Text="{Binding WindowTitle}" />
<ListView ItemsSource="{Binding Files}" SelectedIndex="{Binding SelectedIndex}" />
<Button Command="{Binding PlayPauseCommand}" />
```

#### 3. **ViewModel Layer**
The bridge between View and Model. ViewModels:
- Expose data for the View to bind to
- Implement commands for user actions
- Handle UI logic without direct UI manipulation
- Communicate with services for business logic

**Key ViewModels:**
- `MainWindowViewModel` - Primary application logic

```csharp
// Example: ViewModel with Property Change Notification
public class MainWindowViewModel : INotifyPropertyChanged
{
    private double _zoomLevel = 1.0;
    
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (Math.Abs(_zoomLevel - value) > 0.0001)
            {
                _zoomLevel = value;
                OnPropertyChanged(nameof(ZoomLevel));  // Notify UI
                OnPropertyChanged(nameof(ZoomDisplayText));  // Dependent property
            }
        }
    }
    
    public string ZoomDisplayText => 
        FitMode ? $"Fit ({ZoomLevel:P0})" : $"Zoom: {ZoomLevel:P0}";
}
```

### MVVM Benefits in AstroImages

1. **Separation of Concerns**
   - UI logic is separate from business logic
   - Changes to UI don't affect business logic and vice versa

2. **Testability**
   - ViewModels can be tested without UI
   - Services can be mocked for isolated testing

3. **Maintainability**
   - Clear structure makes code easier to understand
   - Each component has a single responsibility

4. **Reusability**
   - ViewModels can be reused with different Views
   - Services are decoupled and reusable

---

## Dependency Injection

### What is Dependency Injection?

Dependency Injection (DI) is a design pattern where an object receives its dependencies from external sources rather than creating them itself. This promotes loose coupling and makes testing easier.

### DI Implementation in AstroImages

AstroImages uses **Constructor Injection** - dependencies are provided through the constructor.

```
┌─────────────────────────────────────────────────────────────┐
│                  Dependency Injection Flow                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐                                           │
│  │  App.xaml.cs │  Application Entry Point                  │
│  │              │                                           │
│  │  OnStartup() │                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│         │ Creates                                           │
│         ▼                                                   │
│  ┌──────────────────┐                                       │
│  │  MainWindow      │  View (Code-Behind)                   │
│  │                  │                                       │
│  │  Constructor     │                                       │
│  └──────┬───────────┘                                       │
│         │                                                   │
│         │ Creates Services                                  │
│         │                                                   │
│         ├─────► FileManagementService                       │
│         ├─────► KeywordExtractionService                    │
│         ├─────► FolderDialogService                         │
│         ├─────► ListViewColumnService                       │
│         └─────► Other Services...                           │
│         │                                                   │
│         │ Injects Dependencies                              │
│         ▼                                                   │
│  ┌──────────────────────┐                                   │
│  │  MainWindowViewModel │  ViewModel                        │
│  │                      │                                   │
│  │  Constructor(        │                                   │
│  │    services...)      │                                   │
│  └──────────────────────┘                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Example: Constructor Injection

```csharp
// MainWindow.xaml.cs creates all services
public partial class MainWindow : Window
{
    public MainWindow(string[]? filePaths = null)
    {
        InitializeComponent();
        
        var config = AppConfig.Load();
        
        // Create service instances
        var fileManagementService = new FileManagementService();
        var keywordExtractionService = new KeywordExtractionService();
        var folderDialogService = new FolderDialogService();
        var listViewColumnService = new ListViewColumnService(FileListView, config, ...);
        
        // Inject dependencies into ViewModel
        _viewModel = new MainWindowViewModel(
            fileManagementService,
            keywordExtractionService,
            config,
            folderDialogService,
            generalOptionsDialogService,
            customKeywordsDialogService,
            fitsKeywordsDialogService,
            listViewColumnService
        );
        
        // Set DataContext for binding
        this.DataContext = _viewModel;
    }
}

// ViewModel receives dependencies
public class MainWindowViewModel
{
    private readonly FileManagementService _fileManagementService;
    private readonly KeywordExtractionService _keywordExtractionService;
    // ... other services
    
    public MainWindowViewModel(
        FileManagementService fileManagementService,
        KeywordExtractionService keywordExtractionService,
        // ... other services
    )
    {
        _fileManagementService = fileManagementService;
        _keywordExtractionService = keywordExtractionService;
        // ... store other services
        
        // Initialize commands and state
        LoadFilesCommand = new RelayCommand(param => LoadFiles(...));
        // ... other initialization
    }
}
```

### Benefits of DI in AstroImages

1. **Loose Coupling**
   - ViewModel doesn't create services directly
   - Easy to swap implementations

2. **Testability**
   - Services can be mocked/faked for testing
   - ViewModel can be tested in isolation

3. **Flexibility**
   - Different service implementations can be used
   - Easy to add logging, caching, etc.

4. **Single Responsibility**
   - Each service has one clear purpose
   - Easy to understand and maintain

---

## Class Diagrams

### Core Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                        Core Class Structure                        │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ┌──────────────┐                                                  │
│  │     App      │  Application Entry Point                         │
│  └──────┬───────┘                                                  │
│         │ creates                                                  │
│         ▼                                                          │
│  ┌──────────────────────────────────────────────────────┐          │
│  │               MainWindow                             │          │
│  ├──────────────────────────────────────────────────────┤          │
│  │ - _viewModel: MainWindowViewModel                    │          │
│  │ - _listViewColumnService: IListViewColumnService     │          │
│  ├──────────────────────────────────────────────────────┤          │
│  │ + MainWindow(filePaths?: string[])                   │          │
│  │ + LoadFilesWithProgressAsync(path)                   │          │
│  │ + UpdateImageDisplay()                               │          │
│  │ + FileListView_SelectionChanged(...)                 │          │
│  │ - ImageScrollViewer_PreviewMouseWheel(...)           │          │
│  │ - DisplayImage_MouseMove(...)                        │          │
│  └────────┬────────────────────────────────────┬────────┘          │
│           │ uses                               │ creates           │
│           │                                    │                   │
│  ┌────────▼──────────────────────┐     ┌───────▼──────────────┐    │
│  │  MainWindowViewModel          │     │  Services            │    │
│  ├───────────────────────────────┤     ├──────────────────────┤    │
│  │ Properties:                   │     │ FileManagementService│    │
│  │ - Files: ObservableCollection │     │ KeywordExtraction    │    │
│  │ - ZoomLevel: double           │     │ FolderDialogService  │    │
│  │ - SelectedIndex: int          │     │ ListViewColumn       │    │
│  │ - CurrentDirectory: string    │     │ ThemeService         │    │
│  │ - AutoStretch: bool           │     │ UpdateService        │    │
│  │                               │     └──────────────────────┘    │
│  │ Commands:                     │                                 │
│  │ - LoadFilesCommand            │                                 │
│  │ - ZoomInCommand               │                                 │
│  │ - PlayPauseCommand            │                                 │
│  │ - MoveSelectedCommand         │                                 │
│  │                               │                                 │
│  │ Methods:                      │                                 │
│  │ + LoadFiles(path): List       │                                 │
│  │ + UpdateFilesCollection(...)  │                                 │
│  │ + SortByColumn(name)          │                                 │
│  │ - OnPropertyChanged(name)     │                                 │
│  └───────┬───────────────────────┘                                 │
│          │ manages                                                 │
│          │                                                         │
│  ┌───────▼───────────────────┐                                     │
│  │  FileItem (Model)         │                                     │
│  ├───────────────────────────┤                                     │
│  │ + Name: string            │                                     │
│  │ + Path: string            │                                     │
│  │ + Size: long              │                                     │
│  │ + IsSelected: bool        │                                     │
│  │ + CustomKeywords: Dict    │                                     │
│  │ + FitsKeywords: Dict      │                                     │
│  └───────────────────────────┘                                     │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

### Service Layer Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                         Service Layer                              │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │              FileManagementService                      │       │
│  ├─────────────────────────────────────────────────────────┤       │
│  │ + LoadFilesFromDirectory(path): List<FileItem>          │       │
│  │ + GetFileInfo(filePath): FileItem                       │       │
│  │ + MoveSelectedFiles(files, targetDir, toTrash)          │       │
│  │ + MoveToRecycleBin(filePath): bool                      │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │           KeywordExtractionService                      │       │
│  ├─────────────────────────────────────────────────────────┤       │
│  │ + PopulateKeywords(fileItem, customKeys, fitsKeys)      │       │
│  │ + ExtractCustomKeywordsFromFilename(...): Dictionary    │       │
│  │ + ExtractFitsKeywords(path, keywords): Dictionary       │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │              ListViewColumnService                      │       │
│  ├─────────────────────────────────────────────────────────┤       │
│  │ + UpdateListViewColumns()                               │       │
│  │ + AutoResizeColumns()                                   │       │
│  │ + UpdateFileColumnWidth()                               │       │
│  │ + AdjustSplitterForOptimalWidth()                       │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │                  ThemeService                           │       │
│  ├─────────────────────────────────────────────────────────┤       │
│  │ + Initialize()                                          │       │
│  │ + SetThemeMode(mode)                                    │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │           Dialog Services (Interfaces)                  │       │
│  ├─────────────────────────────────────────────────────────┤       │
│  │ IFolderDialogService                                    │       │
│  │ IGeneralOptionsDialogService                            │       │
│  │ ICustomKeywordsDialogService                            │       │
│  │ IFitsKeywordsDialogService                              │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## Sequence Diagrams

### Application Startup Flow

```
┌──────────┐  ┌──────────┐   ┌─────────────┐   ┌──────────────────┐
│   User   │  │   App    │   │ MainWindow  │   │ MainWindowVM     │
└─────┬────┘  └────┬─────┘   └──────┬──────┘   └────────┬─────────┘
      │            │                │                   │
      │ Launch     │                │                   │
      ├───────────►│                │                   │
      │            │                │                   │
      │            │ OnStartup()    │                   │
      │            ├───────────────►│                   │
      │            │                │                   │
      │            │  Check Mutex   │                   │
      │            │  (Single       │                   │
      │            │   Instance)    │                   │
      │            │                │                   │
      │            │  Initialize    │                   │
      │            │  ThemeService  │                   │
      │            │                │                   │
      │            │  new           │                   │
      │            │  MainWindow()  │                   │
      │            │                │                   │
      │            │                │ Load AppConfig    │
      │            │                ├──────────────────►│
      │            │                │                   │
      │            │                │ Create Services   │
      │            │                │ - FileManagement  │
      │            │                │ - Keyword Extract │
      │            │                │ - Dialogs         │
      │            │                │                   │
      │            │                │ new MainWindowVM  │
      │            │                │   (services)      │
      │            │                ├──────────────────►│
      │            │                │                   │
      │            │                │                   │ Initialize
      │            │                │                   │ Commands
      │            │                │                   │
      │            │                │ Set DataContext   │
      │            │                │◄──────────────────┤
      │            │                │                   │
      │            │  Show()        │                   │
      │            │◄───────────────┤                   │
      │            │                │                   │
      │            │  Loaded Event  │                   │
      │            │                ├──►LoadFilesAsync()│
      │            │                │   (if last dir    │
      │            │                │    exists)        │
      │            │                │                   │
      │◄───────────┴────────────────┴───────────────────┤
      │                                                 │
      │  Application Ready                              │
      │                                                 │
```

### File Loading Sequence (with Threading)

```
┌──────┐  ┌─────────┐   ┌─────────────────┐   ┌──────────────────┐
│ User │  │  View   │   │  ViewModel      │   │  Services        │
└───┬──┘  └────┬────┘   └────────┬────────┘   └────────┬─────────┘
    │          │                 │                     │
    │ Click    │                 │                     │
    │ "Open    │                 │                     │
    │ Folder"  │                 │                     │
    ├─────────►│                 │                     │
    │          │                 │                     │
    │          │ OpenFolderDialog│                     │
    │          │ Command         │                     │
    │          ├────────────────►│                     │
    │          │                 │                     │
    │          │                 │ ShowFolderDialog()  │
    │          │                 ├────────────────────►│
    │          │                 │                     │
    │          │                 │◄────────────────────┤
    │          │                 │ selectedFolder      │
    │          │                 │                     │
    │          │                 │ Raise Event:        │
    │          │                 │ LoadFilesWithProgress│
    │          │◄────────────────┤                     │
    │          │                 │                     │
    │          │ Show Loading    │                     │
    │          │ Dialog          │                     │
    │          │                 │                     │
    │          │ Task.Run()      │                     │
    │          │   Background    │                     │
    │          │   Thread ────►  │                     │
    │          │                 │                     │
    │          │                 │ LoadFiles()         │
    │          │                 ├────────────────────►│
    │          │                 │                     │
    │          │                 │                     │ LoadFilesFrom
    │          │                 │                     │ Directory()
    │          │                 │                     │
    │          │                 │                     │ Parallel
    │          │                 │                     │ .ForEach()
    │          │                 │                     │   File 1 ───┐
    │          │                 │                     │   File 2 ───┤
    │          │                 │                     │   File 3 ───┤
    │          │                 │                     │   File N ───┘
    │          │                 │                     │   Extract
    │          │                 │                     │   Keywords
    │          │                 │                     │
    │          │ Dispatcher      │◄────────────────────┤
    │          │ .Invoke()       │ List<FileItem>      │
    │          │ Update Progress │                     │
    │          │                 │                     │
    │          │◄────────────────┤                     │
    │          │ Update UI       │                     │
    │          │                 │                     │
    │          │ UpdateFiles     │                     │
    │          │ Collection()    │                     │
    │          │ (UI Thread)     │                     │
    │          │                 │                     │
    │          │                 │ ObservableCollection│
    │          │                 │ .Add()              │
    │          │                 │ (UI thread only!)   │
    │          │                 │                     │
    │          │ Data Binding    │                     │
    │          │ Updates         │                     │
    │◄─────────┤ ListView        │                     │
    │          │                 │                     │
    │          │ Close Loading   │                     │
    │          │ Dialog          │                     │
    │          │                 │                     │
    │◄─────────┴─────────────────┴─────────────────────┘
    │
    │ Files Displayed
    │
```

### Image Display and Zoom Flow

```
┌──────┐  ┌─────────┐   ┌─────────────┐   ┌──────────────┐
│ User │  │  View   │   │ ViewModel   │   │ Renderer     │
└───┬──┘  └────┬────┘   └──────┬──────┘   └──────┬───────┘
    │          │               │                 │
    │ Select   │               │                 │
    │ File in  │               │                 │
    │ List     │               │                 │
    ├─────────►│               │                 │
    │          │               │                 │
    │          │ Selection     │                 │
    │          │ Changed       │                 │
    │          ├──────────────►│                 │
    │          │               │                 │
    │          │               │ SelectedIndex   │
    │          │               │ = newValue      │
    │          │               │                 │
    │          │               │ OnProperty      │
    │          │               │ Changed()       │
    │          │               │                 │
    │          │ Update        │                 │
    │          │ ImageDisplay()│                 │
    │          │               │                 │
    │          │ Get Selected  │                 │
    │          │ FileItem      │                 │
    │          │               │                 │
    │          │ RenderFitsFile│                 │
    │          │ (path, auto   │                 │
    │          │  stretch)     ├────────────────►│
    │          │               │                 │
    │          │               │                 │ Load Image
    │          │               │                 │ (FITS/XISF/
    │          │               │                 │  Standard)
    │          │               │                 │
    │          │               │                 │ Apply Auto
    │          │               │                 │ Stretch if
    │          │               │                 │ enabled
    │          │               │                 │
    │          │               │◄────────────────┤
    │          │               │ BitmapSource    │
    │          │               │                 │
    │          │ DisplayImage  │                 │
    │          │ .Source =     │                 │
    │          │  bitmap       │                 │
    │          │               │                 │
    │          │ FitMode = true│                 │
    │          │               │                 │
    │          │ Fit to        │                 │
    │          │ ScrollViewer  │                 │
    │          │               │                 │
    │◄─────────┤               │                 │
    │          │               │                 │
    │ Image    │               │                 │
    │ Displayed│               │                 │
    │          │               │                 │
    │ Mouse    │               │                 │
    │ Wheel Up │               │                 │
    ├─────────►│               │                 │
    │          │               │                 │
    │          │ PreviewMouse  │                 │
    │          │ Wheel Event   │                 │
    │          │               │                 │
    │          │ Exit FitMode  │                 │
    │          │ ZoomLevel *=  │                 │
    │          │  1.25         │                 │
    │          │               │                 │
    │          │ Center Image  │                 │
    │◄─────────┤               │                 │
    │          │               │                 │
    │ Zoomed   │               │                 │
    │ Image    │               │                 │
    │          │               │                 │
```

---

## Threading Model

AstroImages uses a **hybrid threading model** to balance performance with UI responsiveness:

```
┌──────────────────────────────────────────────────────────────────┐
│                        Threading Architecture                    │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐      │
│  │                   UI Thread (Main Thread)              │      │
│  ├────────────────────────────────────────────────────────┤      │
│  │                                                        │      │
│  │  • All WPF UI operations                               │      │
│  │  • ObservableCollection modifications                  │      │
│  │  • Data binding updates                                │      │
│  │  • User input handling                                 │      │
│  │  • Window rendering                                    │      │
│  │                                                        │      │
│  │  MUST stay responsive - never block!                   │      │
│  └────────────────┬───────────────────────────────────────┘      │
│                   │                                              │
│                   │ Dispatcher.Invoke()                          │
│                   │ (Marshal to UI thread)                       │
│                   │                                              │
│  ┌────────────────▼───────────────────────────────────────┐      │
│  │              Background Thread Pool                    │      │
│  ├────────────────────────────────────────────────────────┤      │
│  │                                                        │      │
│  │  • File I/O operations                                 │      │
│  │  • FITS/XISF parsing                                   │      │
│  │  • Keyword extraction                                  │      │
│  │  • Image processing                                    │      │
│  │                                                        │      │
│  │  ┌──────────────────────────────────────────┐          │      │
│  │  │     Parallel.ForEach() - File Loading    │          │      │
│  │  ├──────────────────────────────────────────┤          │      │
│  │  │                                          │          │      │
│  │  │  Thread 1: Process File 1 → FileItem 1   │          │      │
│  │  │  Thread 2: Process File 2 → FileItem 2   │          │      │
│  │  │  Thread 3: Process File 3 → FileItem 3   │          │      │
│  │  │  Thread N: Process File N → FileItem N   │          │      │
│  │  │                                          │          │      │
│  │  │  MaxDegreeOfParallelism =                │          │      │
│  │  │    Environment.ProcessorCount            │          │      │
│  │  │                                          │          │      │
│  │  │  ConcurrentBag for thread-safe results   │          │      │
│  │  └──────────────────────────────────────────┘          │      │
│  │                                                        │      │
│  └─────────────────────────────────────────────────────────      │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Threading Rules

1. **UI Thread Only:**
   - `ObservableCollection` modifications
   - Any WPF control property changes
   - Data binding updates

2. **Background Threads:**
   - File system operations
   - FITS/XISF parsing
   - Metadata extraction
   - Image loading (initial bitmap creation)

3. **Thread Marshaling:**
   - Use `Dispatcher.Invoke()` to update UI from background threads
   - Progress callbacks wrapped in `Dispatcher.Invoke()`

### Example: Thread-Safe File Loading

```csharp
// Called on UI thread
private async Task LoadFilesWithProgressAsync(string directoryPath)
{
    // Show loading dialog (UI thread)
    loadingWindow = new LoadingWindow("Loading...");
    loadingWindow.Show();
    
    List<FileItem>? loadedFiles = null;
    
    // Switch to background thread
    await Task.Run(() =>
    {
        // Parallel processing on thread pool
        loadedFiles = _viewModel.LoadFiles(directoryPath, (current, total) =>
        {
            // Marshal progress update to UI thread
            Dispatcher.Invoke(() =>
            {
                loadingWindow.UpdateProgress(current, total);
            });
        });
    });
    
    // Back on UI thread - update ObservableCollection
    if (loadedFiles != null)
    {
        _viewModel.UpdateFilesCollection(loadedFiles);
    }
    
    loadingWindow.Close();
}

// ViewModel method - called on background thread
public List<FileItem> LoadFiles(string path, Action<int, int>? progress)
{
    var files = _fileManagementService.LoadFilesFromDirectory(path);
    var results = new ConcurrentBag<FileItem>();  // Thread-safe
    
    // Parallel processing
    Parallel.ForEach(files,
        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
        file =>
        {
            _keywordExtractionService.PopulateKeywords(file, ...);
            results.Add(file);
            
            // Progress callback (will be marshaled to UI thread)
            progress?.Invoke(current, total);
        });
    
    return results.OrderBy(f => f.Name).ToList();
}
```

---

## Key Design Patterns

### 1. Command Pattern (RelayCommand)

The Command pattern encapsulates user actions as objects that can be executed, parameterized, and queued.

```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;
    
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public bool CanExecute(object? parameter) => 
        _canExecute?.Invoke(parameter) ?? true;
    
    public void Execute(object? parameter) => _execute(parameter);
    
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

// Usage in ViewModel
ZoomInCommand = new RelayCommand(
    _ => ZoomLevel = Math.Min(ZoomLevel * 1.25, 5.0),
    _ => DisplayImage.Source != null  // CanExecute predicate
);
```

### 2. Observer Pattern (INotifyPropertyChanged)

The Observer pattern notifies subscribers when an object's state changes. Essential for WPF data binding.

```csharp
public class MainWindowViewModel : INotifyPropertyChanged
{
    private double _zoomLevel = 1.0;
    
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (Math.Abs(_zoomLevel - value) > 0.0001)
            {
                _zoomLevel = value;
                OnPropertyChanged(nameof(ZoomLevel));  // Notify observers
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### 3. Service Locator Pattern

Services are created in `MainWindow` constructor and injected into ViewModel:

```csharp
// MainWindow acts as simple service locator
public MainWindow()
{
    // Create services
    var fileService = new FileManagementService();
    var keywordService = new KeywordExtractionService();
    
    // Inject into ViewModel
    _viewModel = new MainWindowViewModel(fileService, keywordService, ...);
}
```

### 4. Repository Pattern (FileManagementService)

Abstracts data access (file system) from business logic:

```csharp
public class FileManagementService
{
    public List<FileItem> LoadFilesFromDirectory(string directory)
    {
        // Encapsulates file system access
        var files = Directory.GetFiles(directory, "*.fits");
        return files.Select(f => new FileItem { Path = f, ... }).ToList();
    }
}
```

### 5. Strategy Pattern (Image Rendering)

Different rendering strategies for different image formats:

```csharp
public static class FitsImageRenderer
{
    public static BitmapSource? RenderFitsFile(string path, bool autoStretch)
    {
        var ext = Path.GetExtension(path).ToLower();
        
        return ext switch
        {
            ".fits" or ".fit" or ".fts" => RenderFitsImage(path, autoStretch),
            ".xisf" => RenderXisfImage(path, autoStretch),
            _ => RenderStandardImage(path)  // JPEG, PNG, etc.
        };
    }
}
```

### 6. Singleton Pattern (ThemeService, Mutex)

Single instance shared across application:

```csharp
public static class ThemeService
{
    private static ThemeMode _currentMode;
    
    public static void Initialize()
    {
        // Single initialization
    }
    
    public static void SetThemeMode(ThemeMode mode)
    {
        // Shared state
    }
}

// Single instance enforcement with Mutex
private static Mutex? _mutex;
protected override void OnStartup(StartupEventArgs e)
{
    _mutex = new Mutex(true, "AstroImages_SingleInstance", out bool createdNew);
    if (!createdNew)
    {
        // Already running
        MessageBox.Show("Already running");
        Shutdown();
    }
}
```

---

## Performance Optimizations

### 1. Parallel Processing
- **File Loading**: Uses `Parallel.ForEach` to process multiple files simultaneously
- **MaxDegreeOfParallelism**: Set to `Environment.ProcessorCount` for optimal CPU utilization
- **Thread-Safe Collections**: `ConcurrentBag<FileItem>` for safe parallel access

### 2. Lazy Loading
- **Image Rendering**: Images loaded only when selected
- **Metadata Extraction**: Keywords extracted during file load, not on-demand
- **Column Visibility**: Only visible columns are rendered in ListView

### 3. UI Responsiveness
- **Background Threads**: Long-running operations on background threads
- **Progress Dialogs**: Visual feedback during lengthy operations
- **Dispatcher Throttling**: Progress updates throttled (every 5 files) to reduce UI overhead

### 4. Memory Management
- **Image Caching**: Only current image kept in memory
- **Bitmap Disposal**: Old images released when new ones loaded
- **Collection Clearing**: ObservableCollection cleared before repopulating

---

## Data Flow Summary

```
User Action → Command → ViewModel Method → Service → 
    → Background Processing (if needed) → 
    → Data/Results → 
    → UI Thread (Dispatcher.Invoke) → 
    → ObservableCollection/Property Update → 
    → INotifyPropertyChanged → 
    → WPF Binding Engine → 
    → UI Update
```

---

## Testing Considerations

### Unit Testing ViewModels
```csharp
[Test]
public void ZoomIn_IncreasesZoomLevel()
{
    var mockFileService = new MockFileManagementService();
    var vm = new MainWindowViewModel(mockFileService, ...);
    
    var initialZoom = vm.ZoomLevel;
    vm.ZoomInCommand.Execute(null);
    
    Assert.Greater(vm.ZoomLevel, initialZoom);
}
```

### Integration Testing
- Test file loading with actual filesystem
- Test FITS parsing with sample files
- Test UI interactions with coded UI tests

### Mocking Services
```csharp
public interface IFileManagementService
{
    List<FileItem> LoadFilesFromDirectory(string path);
}

public class MockFileManagementService : IFileManagementService
{
    public List<FileItem> LoadFilesFromDirectory(string path) =>
        new List<FileItem> { new FileItem { Name = "test.fits" } };
}
```

---

## Future Architecture Enhancements

1. **Full Dependency Injection Container**
   - Consider Microsoft.Extensions.DependencyInjection
   - Centralized service registration
   - Lifetime management (Singleton, Transient, Scoped)

2. **Event Aggregator Pattern**
   - Decouple ViewModels further
   - Publish/subscribe messaging between components

3. **Repository Abstraction**
   - Abstract file system access behind interface
   - Enable unit testing without filesystem

4. **Caching Layer**
   - Cache parsed FITS headers
   - Cache rendered thumbnails
   - Reduce repeated file I/O

5. **Async/Await Throughout**
   - Convert all I/O to async
   - Better cancellation support
   - Improved responsiveness

---

## Conclusion

AstroImages demonstrates a well-structured WPF application following industry-standard patterns:

- **MVVM** for separation of concerns and testability
- **Dependency Injection** for loose coupling
- **Parallel Processing** for performance
- **Service Layer** for business logic encapsulation
- **Command Pattern** for user actions
- **Observer Pattern** for reactive UI updates

This architecture provides:
✅ Maintainability - Clear structure and responsibilities  
✅ Testability - Mockable dependencies and isolated units  
✅ Scalability - Easy to add features without breaking existing code  
✅ Performance - Multi-threaded processing where appropriate  
✅ Responsiveness - UI never blocks on long operations  

The combination of these patterns creates a robust, professional desktop application suitable for real-world use and future enhancement.
