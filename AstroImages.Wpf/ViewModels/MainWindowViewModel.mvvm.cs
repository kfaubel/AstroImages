
// This is the main ViewModel for the application, following the MVVM (Model-View-ViewModel) architectural pattern
// In MVVM:
// - Model: Represents data and business logic (FileItem, AppConfig, etc.)
// - View: The user interface (MainWindow.xaml)
// - ViewModel: The bridge between View and Model, handling UI logic and data binding

using AstroImages.Wpf.Models;
using AstroImages.Wpf.Services;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Globalization;

namespace AstroImages.Wpf.ViewModels
{
    /// <summary>
    /// MainWindowViewModel handles all the UI logic and data for the main application window.
    /// It implements INotifyPropertyChanged to support WPF data binding - when properties change,
    /// the UI automatically updates to reflect those changes.
    /// 
    /// Key MVVM Concepts Demonstrated:
    /// - Property Change Notification: Properties notify the UI when they change
    /// - Command Pattern: User actions are handled through RelayCommand objects
    /// - Data Binding: Properties are bound to UI elements in the XAML
    /// - Dependency Injection: Services are injected through the constructor
    /// - Separation of Concerns: UI logic is separate from business logic
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        // Private fields store the actual data. These are the "backing fields" for properties.
        // By convention, private fields start with underscore (_) and use camelCase naming.
        private double _zoomLevel = 1.0;       // Current zoom level for image display
        private double _fitScale = 1.0;        // Scale factor when fitting image to window
        #endregion

        #region Image Display Properties
        // These properties control how images are displayed and zoomed.
        // The get/set pattern with OnPropertyChanged is crucial for WPF data binding.
        
        /// <summary>
        /// Scale factor used when fitting the image to the window.
        /// This is calculated automatically based on image and window dimensions.
        /// </summary>
        public double FitScale
        {
            get => _fitScale;
            set
            {
                // Only update if the value actually changed (avoid unnecessary UI updates)
                if (Math.Abs(_fitScale - value) > 0.0001)
                {
                    _fitScale = value;
                    // Notify the UI that this property changed so bound controls can update
                    OnPropertyChanged(nameof(FitScale));
                }
            }
        }

        private bool _autoStretch = true;  // Default to enabled
        
        /// <summary>
        /// Controls whether FITS and XISF images should be automatically stretched to enhance visibility.
        /// When enabled, applies histogram stretching and gamma correction to astronomical images.
        /// When disabled, shows raw pixel values without enhancement.
        /// </summary>
        public bool AutoStretch
        {
            get => _autoStretch;
            set
            {
                if (_autoStretch != value)
                {
                    _autoStretch = value;
                    // Save to configuration
                    _appConfig.AutoStretch = value;
                    _appConfig.Save();
                    OnPropertyChanged(nameof(AutoStretch));
                    // Trigger image refresh when stretching mode changes
                    AutoStretchChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Event that notifies the View when auto-stretch setting changes.
        /// The View can use this to refresh the current image with the new setting.
        /// </summary>
        public event Action? AutoStretchChanged;
        /// <summary>
        /// Current zoom level for the image (1.0 = 100%, 2.0 = 200%, etc.)
        /// When this changes, we also notify ZoomDisplayText since it depends on ZoomLevel.
        /// </summary>
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (Math.Abs(_zoomLevel - value) > 0.0001)
                {
                    _zoomLevel = value;
                    OnPropertyChanged(nameof(ZoomLevel));
                    // Also notify dependent properties that use ZoomLevel in their calculations
                    OnPropertyChanged(nameof(ZoomDisplayText));
                }
            }
        }

        private bool _fitMode = true;  // Whether image should fit to window or use manual zoom
        
        /// <summary>
        /// Determines whether the image should automatically fit to the window (true)
        /// or use manual zoom levels (false). This affects scrollbar visibility.
        /// </summary>
        public bool FitMode
        {
            get => _fitMode;
            set
            {
                if (_fitMode != value)
                {
                    _fitMode = value;
                    OnPropertyChanged(nameof(FitMode));
                    // ScrollBarVisibility depends on FitMode, so notify it too
                    OnPropertyChanged(nameof(ScrollBarVisibility));
                }
            }
        }

        /// <summary>
        /// Computed property that formats zoom level for display in the UI.
        /// Uses string interpolation and percentage formatting (:P0 means percentage with 0 decimal places).
        /// This demonstrates how ViewModels can provide formatted data for the View.
        /// </summary>
        public string ZoomDisplayText => FitMode ? $"Fit ({ZoomLevel:P0})" : $"Zoom: {ZoomLevel:P0}";

        /// <summary>
        /// Computed property that determines whether scrollbars should be visible.
        /// In fit mode, scrollbars are hidden because the image always fits the window.
        /// In manual zoom mode, scrollbars appear automatically when needed.
        /// </summary>
        public System.Windows.Controls.ScrollBarVisibility ScrollBarVisibility => 
            FitMode ? System.Windows.Controls.ScrollBarVisibility.Hidden : System.Windows.Controls.ScrollBarVisibility.Auto;

        // Commands are the MVVM way of handling user actions (button clicks, menu selections, etc.)
        // Each command encapsulates an action and can specify when it's enabled/disabled
        public RelayCommand ZoomInCommand { get; }           // Increase zoom level
        public RelayCommand ZoomOutCommand { get; }          // Decrease zoom level  
        public RelayCommand FitToWindowCommand { get; }      // Fit image to window
        public RelayCommand ResetZoomCommand { get; }        // Reset to 100% zoom
        public RelayCommand ZoomActualCommand { get; }       // Set to actual size (1:1 pixels)
        #endregion
        #region File Management Methods
        /// <summary>
        /// Loads all files from the specified directory and populates the Files collection.
        /// This method demonstrates the Service Layer pattern - instead of doing file I/O directly,
        /// we delegate to specialized services that handle the specific tasks.
        /// </summary>
        /// <param name="directoryPath">Path to the directory containing files to load</param>
        public void LoadFiles(string directoryPath)
        {
            // Clear existing files from the observable collection
            // First, unhook event handlers from existing items
            foreach (var fileItem in Files)
            {
                fileItem.PropertyChanged -= FileItem_PropertyChanged;
            }
            
            // ObservableCollection automatically notifies the UI when items are added/removed
            Files.Clear();
            
            // Use the file management service to get file information
            var fileItems = _fileManagementService.LoadFilesFromDirectory(directoryPath);
            
            // For each file, extract keywords and add to our collection
            foreach (var fileItem in fileItems)
            {
                // Extract both custom and FITS keywords for each file
                _keywordExtractionService.PopulateKeywords(fileItem, _appConfig.CustomKeywords, _appConfig.FitsKeywords);
                
                // Listen for IsSelected property changes to update HasSelectedFiles
                fileItem.PropertyChanged += FileItem_PropertyChanged;
                
                Files.Add(fileItem);  // Adding to ObservableCollection triggers UI update
            }

            // Set default sort by filename
            SortByColumn("File");
            
            // Notify the View that files have been loaded so it can update the UI
            FilesLoaded?.Invoke();
        }

        /// <summary>
        /// Refreshes the custom keywords for all files in the current list.
        /// Called when custom keyword configuration changes.
        /// </summary>
        public void RefreshFileListKeywords()
        {
            foreach (var fileItem in Files)
            {
                fileItem.CustomKeywords = _keywordExtractionService.ExtractCustomKeywordsFromFilename(fileItem.Name, _appConfig.CustomKeywords);
            }
        }

        /// <summary>
        /// Refreshes the FITS keywords for all files in the current list.
        /// Called when FITS keyword configuration changes.
        /// </summary>
        public void RefreshFileListFitsKeywords()
        {
            foreach (var fileItem in Files)
            {
                fileItem.FitsKeywords = _keywordExtractionService.ExtractFitsKeywords(fileItem.Path, _appConfig.FitsKeywords);
            }
        }
        #endregion
        #region Navigation and Playback Properties
        // These fields support image navigation and automatic playback functionality
        private int _selectedIndex = -1;       // Index of currently selected file (-1 = none selected)
        private bool _isPlaying;               // Whether automatic playback is active
        private System.Timers.Timer? _playTimer; // Timer for automatic playback (nullable because it's created on demand)
        private bool _awaitingImageRender;     // Whether we're waiting for image rendering to complete before starting next timer

        /// <summary>
        /// Index of the currently selected file in the Files collection.
        /// -1 indicates no file is selected. This property is bound to the ListView's SelectedIndex.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnPropertyChanged(nameof(SelectedIndex));
                }
            }
        }

        /// <summary>
        /// Indicates whether automatic playback (slideshow mode) is currently active.
        /// This property can be bound to UI elements to show/hide play/pause buttons.
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        /// <summary>
        /// Indicates whether any files are currently selected (checked) in the file list.
        /// Used to enable/disable the Move Selected button.
        /// </summary>
        public bool HasSelectedFiles => Files.Any(f => f.IsSelected);

        // Navigation commands - these handle moving through the file list
        public RelayCommand GotoFirstCommand { get; }     // Jump to first file
        public RelayCommand GotoPreviousCommand { get; }  // Go to previous file (wraps around)
        public RelayCommand PlayPauseCommand { get; }     // Toggle automatic playback
        public RelayCommand GotoNextCommand { get; }      // Go to next file (wraps around)
        public RelayCommand GotoLastCommand { get; }      // Jump to last file
        #endregion

        #region Collections and Events
        /// <summary>
        /// Collection of files currently loaded in the application.
        /// ObservableCollection is a special WPF collection that automatically notifies the UI
        /// when items are added, removed, or changed. This enables automatic UI updates
        /// when the file list changes. The collection is bound to the ListView in the UI.
        /// </summary>
        public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();
        
        /// <summary>
        /// Current sort column for tracking sort state
        /// </summary>
        private string _currentSortColumn = "Name";
        
        /// <summary>
        /// Current sort direction for tracking sort state
        /// </summary>
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
        
        /// <summary>
        /// Gets the count of files for command CanExecute evaluation
        /// </summary>
        public int FileCount => Files.Count;
        
        /// <summary>
        /// Sorts the file collection by the specified column.
        /// Automatically detects numeric vs text sorting and handles sort direction toggling.
        /// </summary>
        /// <param name="columnName">The property name or header text to sort by</param>
        public void SortByColumn(string columnName)
        {
            if (string.IsNullOrEmpty(columnName) || Files.Count == 0) return;
            
            // Toggle sort direction if clicking the same column
            if (_currentSortColumn == columnName)
            {
                _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending 
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }
            else
            {
                _currentSortColumn = columnName;
                _currentSortDirection = ListSortDirection.Ascending;
            }
            
            // Create a sorted list based on the column
            var sortedFiles = SortFilesByColumn(Files.ToList(), columnName, _currentSortDirection);
            
            // Clear and repopulate the ObservableCollection to maintain the same references
            // but in sorted order. This preserves selection and other UI state.
            for (int i = 0; i < sortedFiles.Count; i++)
            {
                var currentIndex = Files.IndexOf(sortedFiles[i]);
                if (currentIndex != i && currentIndex != -1)
                {
                    Files.Move(currentIndex, i);
                }
            }
        }
        
        /// <summary>
        /// Sorts a list of FileItem objects by the specified column
        /// </summary>
        private List<FileItem> SortFilesByColumn(List<FileItem> files, string columnName, ListSortDirection direction)
        {
            return columnName switch
            {
                "File" => direction == ListSortDirection.Ascending 
                    ? files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList()
                    : files.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                    
                "Size" => direction == ListSortDirection.Ascending 
                    ? files.OrderBy(f => f.Size).ToList()
                    : files.OrderByDescending(f => f.Size).ToList(),
                    
                _ when _appConfig.CustomKeywords.Contains(columnName) =>
                    SortByDictionaryValue(files, f => f.CustomKeywords, columnName, direction),
                    
                _ when _appConfig.FitsKeywords.Contains(columnName) =>
                    SortByDictionaryValue(files, f => f.FitsKeywords, columnName, direction),
                    
                _ => files // No sorting for unknown columns
            };
        }
        
        /// <summary>
        /// Sorts files by a dictionary value, with smart numeric/text detection
        /// </summary>
        private List<FileItem> SortByDictionaryValue(List<FileItem> files, 
            Func<FileItem, Dictionary<string, string>> getDictionary, 
            string key, 
            ListSortDirection direction)
        {
            // Get all non-null values to determine if they're numeric
            var values = files
                .Select(f => getDictionary(f).TryGetValue(key, out string? value) ? value : null)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();
                
            bool isNumeric = values.Count > 0 && values.All(v => double.TryParse(v, out _));
            
            if (isNumeric)
            {
                // Numeric sorting
                return direction == ListSortDirection.Ascending
                    ? files.OrderBy(f => 
                        {
                            var dict = getDictionary(f);
                            if (dict.TryGetValue(key, out string? value) && double.TryParse(value, out double num))
                                return num;
                            return double.MaxValue; // Put non-numeric values at the end
                        }).ToList()
                    : files.OrderByDescending(f => 
                        {
                            var dict = getDictionary(f);
                            if (dict.TryGetValue(key, out string? value) && double.TryParse(value, out double num))
                                return num;
                            return double.MinValue; // Put non-numeric values at the end
                        }).ToList();
            }
            else
            {
                // Text sorting
                return direction == ListSortDirection.Ascending
                    ? files.OrderBy(f => 
                        {
                            var dict = getDictionary(f);
                            return dict.TryGetValue(key, out string? value) ? value ?? "" : "";
                        }, StringComparer.OrdinalIgnoreCase).ToList()
                    : files.OrderByDescending(f => 
                        {
                            var dict = getDictionary(f);
                            return dict.TryGetValue(key, out string? value) ? value ?? "" : "";
                        }, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        /// <summary>
        /// Event required by INotifyPropertyChanged interface. This is how WPF knows
        /// when properties change so it can update bound UI elements automatically.
        /// The ? means this event can be null (no subscribers).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Dependency Injection Fields
        // These are the services injected through the constructor. They handle specific responsibilities:
        // This demonstrates the Dependency Injection pattern - instead of creating dependencies directly,
        // they are provided (injected) from outside, making the code more testable and flexible.
        
        private readonly FileManagementService _fileManagementService;           // Handles file system operations
        private readonly KeywordExtractionService _keywordExtractionService;     // Extracts metadata from files
        private readonly AppConfig _appConfig;                                   // Application configuration settings
        private readonly IFolderDialogService _folderDialogService;             // Shows folder selection dialogs
        private readonly IGeneralOptionsDialogService _generalOptionsDialogService; // Shows general options dialog
        private readonly ICustomKeywordsDialogService _customKeywordsDialogService; // Shows custom keywords dialog
        private readonly IFitsKeywordsDialogService _fitsKeywordsDialogService;     // Shows FITS keywords dialog
        private readonly IListViewColumnService _listViewColumnService;         // Manages ListView column visibility
        #endregion
        #region Main Application Commands
        // These commands handle the primary application functionality
        // Commands encapsulate user actions and can be bound to buttons, menu items, etc.
        public RelayCommand LoadFilesCommand { get; }                    // Load files from a directory
        public RelayCommand RefreshCustomKeywordsCommand { get; }        // Refresh custom keyword extraction
        public RelayCommand RefreshFitsKeywordsCommand { get; }          // Refresh FITS keyword extraction
        public RelayCommand OpenFolderDialogCommand { get; }             // Show folder selection dialog
        public RelayCommand ShowGeneralOptionsDialogCommand { get; }     // Show general application options
        public RelayCommand ShowCustomKeywordsDialogCommand { get; }     // Show custom keywords configuration
        public RelayCommand ShowFitsKeywordsDialogCommand { get; }       // Show FITS keywords configuration
        public RelayCommand ShowFileMetadataCommand { get; }             // Show file metadata dialog
        public RelayCommand MoveSelectedCommand { get; }                 // Move selected files
        public RelayCommand EnterFullScreenCommand { get; }              // Enter full screen mode
        #endregion
        #region Constructor
        /// <summary>
        /// Constructor that receives all dependencies through Dependency Injection.
        /// This is a core MVVM pattern - the ViewModel doesn't create its own dependencies,
        /// they are provided by a DI container (configured in App.xaml.cs).
        /// 
        /// Benefits of this approach:
        /// - Testability: Dependencies can be mocked for unit testing
        /// - Flexibility: Different implementations can be injected
        /// - Loose Coupling: ViewModel doesn't depend on concrete classes
        /// </summary>
        /// <param name="fileManagementService">Service for file system operations</param>
        /// <param name="keywordExtractionService">Service for extracting metadata from files</param>
        /// <param name="appConfig">Application configuration object</param>
        /// <param name="folderDialogService">Service for showing folder selection dialogs</param>
        /// <param name="generalOptionsDialogService">Service for general options dialog (nullable)</param>
        /// <param name="customKeywordsDialogService">Service for custom keywords dialog (nullable)</param>
        /// <param name="fitsKeywordsDialogService">Service for FITS keywords dialog (nullable)</param>
        /// <param name="listViewColumnService">Service for managing ListView columns</param>
        public MainWindowViewModel(
            FileManagementService fileManagementService,
            KeywordExtractionService keywordExtractionService,
            AppConfig appConfig,
            IFolderDialogService folderDialogService,
            IGeneralOptionsDialogService? generalOptionsDialogService,
            ICustomKeywordsDialogService? customKeywordsDialogService,
            IFitsKeywordsDialogService? fitsKeywordsDialogService,
            IListViewColumnService listViewColumnService)
        {
            // Store injected dependencies for later use
            _fileManagementService = fileManagementService;
            _keywordExtractionService = keywordExtractionService;
            _appConfig = appConfig;
            _folderDialogService = folderDialogService;
            
            // Initialize AutoStretch from configuration
            _autoStretch = _appConfig.AutoStretch;
            
            // Use null-coalescing operator (??) to provide default implementations if none injected
            // This provides fallback behavior while still supporting dependency injection
            _generalOptionsDialogService = generalOptionsDialogService ?? new GeneralOptionsDialogService();
            _customKeywordsDialogService = customKeywordsDialogService ?? new CustomKeywordsDialogService();
            _fitsKeywordsDialogService = fitsKeywordsDialogService ?? new FitsKeywordsDialogService();
            _listViewColumnService = listViewColumnService;

            // Initialize commands using RelayCommand
            // Commands use lambda expressions (=>) for concise action definitions
            // The underscore (_) parameter means "we don't use this parameter"
            
            // This command accepts a string parameter (directory path) and loads files from it
            LoadFilesCommand = new RelayCommand(param =>
            {
                // Pattern matching: check if param is a string and not empty
                if (param is string dir && !string.IsNullOrEmpty(dir))
                {
                    LoadFiles(dir);
                }
            });

            // Simple commands that call methods without parameters
            RefreshCustomKeywordsCommand = new RelayCommand(_ => RefreshFileListKeywords());
            RefreshFitsKeywordsCommand = new RelayCommand(_ => RefreshFileListFitsKeywords());
            OpenFolderDialogCommand = new RelayCommand(_ => OpenFolderAndLoadFiles());
            ShowGeneralOptionsDialogCommand = new RelayCommand(_ => ShowGeneralOptionsDialog());
            ShowCustomKeywordsDialogCommand = new RelayCommand(_ => ShowCustomKeywordsDialog());
            ShowFitsKeywordsDialogCommand = new RelayCommand(_ => ShowFitsKeywordsDialog());
            ShowFileMetadataCommand = new RelayCommand(param => ShowFileMetadata(param));
            MoveSelectedCommand = new RelayCommand(_ => MoveSelectedFiles(), _ => HasSelectedFiles);
            EnterFullScreenCommand = new RelayCommand(_ => EnterFullScreen(), _ => SelectedIndex >= 0 && SelectedIndex < Files.Count);

            // Navigation commands with CanExecute predicates
            // The second parameter to RelayCommand is a "CanExecute" function that determines
            // when the command should be enabled. These are disabled when no files are loaded.
            GotoFirstCommand = new RelayCommand(_ => GotoFirst(), _ => FileCount > 0);
            GotoPreviousCommand = new RelayCommand(_ => GotoPrevious(), _ => FileCount > 0);
            PlayPauseCommand = new RelayCommand(_ => TogglePlayPause(), _ => FileCount > 0);
            GotoNextCommand = new RelayCommand(_ => GotoNext(), _ => FileCount > 0);
            GotoLastCommand = new RelayCommand(_ => GotoLast(), _ => FileCount > 0);
            
            // Zoom commands with inline logic using Math.Min/Max to constrain values
            ZoomInCommand = new RelayCommand(_ => ZoomLevel = Math.Min(ZoomLevel * 1.25, 5.0));  // Max 500%
            ZoomOutCommand = new RelayCommand(_ => ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1)); // Min 10%
            FitToWindowCommand = new RelayCommand(_ => FitToWindow());
            ResetZoomCommand = new RelayCommand(_ => ZoomLevel = 1.0);  // Reset to 100%
            ZoomActualCommand = new RelayCommand(_ => SetZoomActual());
        }
        #endregion

        #region Zoom Control Methods
        /// <summary>
        /// Switches to fit-to-window mode and requests the UI to recalculate the fit.
        /// This demonstrates the ViewModel-View communication pattern using events.
        /// The ViewModel sets the state, then raises an event for the View to handle.
        /// </summary>
        private void FitToWindow()
        {
            FitMode = true;
            OnPropertyChanged(nameof(ZoomDisplayText));  // Update display text
            // Request the View (code-behind) to perform the actual fitting calculation
            // This is necessary because fitting requires knowledge of the actual UI dimensions
            FitRequested?.Invoke();
        }

        /// <summary>
        /// Event that notifies the View when a fit-to-window operation is requested.
        /// The View subscribes to this event and handles the actual dimension calculations.
        /// This follows the MVVM pattern of keeping UI-specific logic in the View.
        /// </summary>
        public event Action? FitRequested;

        /// <summary>
        /// Sets zoom to actual size (1:1 pixel ratio) and requests the UI to center the image.
        /// This switches from fit mode to manual zoom mode.
        /// </summary>
        private void SetZoomActual()
        {
            FitMode = false;
            ZoomLevel = 1.0;  // 100% zoom
            OnPropertyChanged(nameof(ZoomDisplayText));
            // Request the View to center the image at actual size
            ZoomActualRequested?.Invoke();
        }

        /// <summary>
        /// Event that notifies the View when actual size zoom is requested.
        /// The View handles centering the image in the viewport.
        /// </summary>
        public event Action? ZoomActualRequested;

        /// <summary>
        /// Event that notifies the View when files have been loaded from a directory.
        /// The View can use this to perform UI updates like auto-resizing columns.
        /// </summary>
        public event Action? FilesLoaded;

        /// <summary>
        /// Called by the View when an image has finished rendering.
        /// Used for play mode to start the delay after image rendering is complete.
        /// </summary>
        public void OnImageRenderingCompleted()
        {
            if (IsPlaying && _awaitingImageRender)
            {
                _awaitingImageRender = false;
                StartPlayTimer();
            }
        }
        #endregion

        private void GotoFirst()
        {
            if (FileCount > 0)
                SelectedIndex = 0;
        }

        private void GotoPrevious()
        {
            if (FileCount == 0) return;
            if (SelectedIndex <= 0)
                SelectedIndex = FileCount - 1;
            else
                SelectedIndex--;
        }

        private void GotoNext()
        {
            if (FileCount == 0) return;
            if (SelectedIndex >= FileCount - 1)
                SelectedIndex = 0;
            else
                SelectedIndex++;
                
            // If we're in play mode, mark that we're waiting for image rendering to complete
            if (IsPlaying)
            {
                _awaitingImageRender = true;
            }
        }

        private void GotoLast()
        {
            if (FileCount > 0)
                SelectedIndex = FileCount - 1;
        }

        private void TogglePlayPause()
        {
            if (IsPlaying)
            {
                StopPlay();
            }
            else
            {
                StartPlay();
            }
        }

        private void StartPlay()
        {
            IsPlaying = true;
            // Start by immediately going to the next image
            // The timer will be started after the image finishes rendering
            GotoNext();
        }

        private void StartPlayTimer()
        {
            if (_playTimer == null)
            {
                _playTimer = new System.Timers.Timer(1000);
                _playTimer.Elapsed += (s, e) =>
                {
                    _playTimer.Stop(); // Stop the timer - it will be restarted after next image renders
                    App.Current.Dispatcher.Invoke(() => GotoNext());
                };
            }
            _playTimer.Start();
        }

        private void StopPlay()
        {
            _playTimer?.Stop();
            IsPlaying = false;
            _awaitingImageRender = false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenFolderAndLoadFiles()
        {
            string initialDirectory = !string.IsNullOrEmpty(_appConfig.LastOpenDirectory) && System.IO.Directory.Exists(_appConfig.LastOpenDirectory)
                ? _appConfig.LastOpenDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var selectedFolder = _folderDialogService.ShowFolderDialog(initialDirectory);
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                _appConfig.LastOpenDirectory = selectedFolder;
                _appConfig.Save();
                LoadFiles(selectedFolder);
            }
        }
        private void ShowGeneralOptionsDialog()
        {
            var result = _generalOptionsDialogService.ShowGeneralOptionsDialog(
                _appConfig.ShowSizeColumn, 
                _appConfig.Theme, 
                _appConfig.ShowFullScreenHelp,
                _appConfig.PlayPauseInterval);
                
            if (result.showSizeColumn.HasValue)
            {
                _appConfig.ShowSizeColumn = result.showSizeColumn.Value;
                
                if (result.theme.HasValue)
                {
                    _appConfig.Theme = result.theme.Value;
                    // Apply the new theme
                    ThemeService.SetThemeMode(result.theme.Value);
                }
                
                if (result.showFullScreenHelp.HasValue)
                {
                    _appConfig.ShowFullScreenHelp = result.showFullScreenHelp.Value;
                }
                
                if (result.playPauseInterval.HasValue)
                {
                    _appConfig.PlayPauseInterval = result.playPauseInterval.Value;
                }
                
                _appConfig.Save();
                _listViewColumnService.UpdateListViewColumns();
                // Auto-resize columns after configuration change
                _listViewColumnService.AutoResizeColumns();
            }
        }

        private void EnterFullScreen()
        {
            if (SelectedIndex < 0 || SelectedIndex >= Files.Count)
                return;

            var fullScreenWindow = new FullScreenWindow(Files, SelectedIndex, _appConfig);
            fullScreenWindow.ShowDialog();
            
            // Update the selected index to the last viewed image in full screen
            SelectedIndex = fullScreenWindow.CurrentIndex;
        }

        private void ShowCustomKeywordsDialog()
        {
            var result = _customKeywordsDialogService.ShowCustomKeywordsDialog(_appConfig.CustomKeywords);
            if (result != null)
            {
                _appConfig.CustomKeywords = result;
                _appConfig.Save();
                RefreshFileListKeywords();
                _listViewColumnService.UpdateListViewColumns();
                // Auto-resize columns after configuration change
                _listViewColumnService.AutoResizeColumns();
            }
        }

        private void ShowFitsKeywordsDialog()
        {
            var result = _fitsKeywordsDialogService.ShowFitsKeywordsDialog(_appConfig.FitsKeywords);
            if (result != null)
            {
                _appConfig.FitsKeywords = result;
                _appConfig.Save();
                RefreshFileListFitsKeywords();
                _listViewColumnService.UpdateListViewColumns();
                // Auto-resize columns after configuration change
                _listViewColumnService.AutoResizeColumns();
            }
        }

        private void ShowFileMetadata(object? parameter)
        {
            if (parameter is FileItem fileItem)
            {
                // Show hourglass cursor while loading metadata dialog
                var originalCursor = System.Windows.Input.Mouse.OverrideCursor;
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                
                try
                {
                    var dialog = new FileMetadataDialog(fileItem);
                    
                    // Find the main window to set as owner
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        dialog.Owner = mainWindow;
                    }
                    
                    // Restore cursor before showing dialog
                    System.Windows.Input.Mouse.OverrideCursor = originalCursor;
                    
                    dialog.ShowDialog();
                }
                catch
                {
                    // Ensure cursor is restored even if an error occurs
                    System.Windows.Input.Mouse.OverrideCursor = originalCursor;
                    throw;
                }
            }
        }

        /// <summary>
        /// Handles the Move Selected Files command by showing the move dialog and moving the selected files.
        /// </summary>
        private void MoveSelectedFiles()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
                return;

            // Show the move files dialog
            var dialog = new MoveFilesDialog(selectedFiles.Select(f => f.Name).ToList());
            
            // Set default directory to the same directory as the first selected file
            if (selectedFiles.Count > 0)
            {
                var firstFileDir = System.IO.Path.GetDirectoryName(selectedFiles[0].Path);
                if (!string.IsNullOrEmpty(firstFileDir))
                {
                    dialog.SetDefaultDirectory(firstFileDir);
                }
            }

            // Find the main window to set as owner
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                dialog.Owner = mainWindow;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _fileManagementService.MoveSelectedFiles(selectedFiles, dialog.TargetDirectory, dialog.MoveToTrash);
                    
                    // Remove moved files from the list
                    for (int i = Files.Count - 1; i >= 0; i--)
                    {
                        if (Files[i].IsSelected)
                        {
                            Files.RemoveAt(i);
                        }
                    }
                    
                    // Refresh the HasSelectedFiles property to update button state
                    OnPropertyChanged(nameof(HasSelectedFiles));
                    
                    System.Windows.MessageBox.Show($"Successfully moved {selectedFiles.Count} file(s).", 
                        "Move Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error moving files: {ex.Message}", 
                        "Move Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handles PropertyChanged events from FileItem objects to update HasSelectedFiles when IsSelected changes.
        /// </summary>
        private void FileItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileItem.IsSelected))
            {
                OnPropertyChanged(nameof(HasSelectedFiles));
            }
        }
    }
    
    /// <summary>
    /// Custom comparer for sorting dictionary-based columns (Custom Keywords and FITS Keywords)
    /// </summary>
    public class DictionarySortComparer : IComparer
    {
        private readonly string _columnName;
        private readonly ListSortDirection _direction;
        private readonly AppConfig _appConfig;
        
        public DictionarySortComparer(string columnName, ListSortDirection direction, AppConfig appConfig)
        {
            _columnName = columnName;
            _direction = direction;
            _appConfig = appConfig;
        }
        
        public int Compare(object? x, object? y)
        {
            if (x is not FileItem fileX || y is not FileItem fileY)
                return 0;
                
            string? valueX = GetColumnValue(fileX);
            string? valueY = GetColumnValue(fileY);
            
            // Handle nulls - put them at the end
            if (string.IsNullOrEmpty(valueX) && string.IsNullOrEmpty(valueY)) return 0;
            if (string.IsNullOrEmpty(valueX)) return _direction == ListSortDirection.Ascending ? 1 : -1;
            if (string.IsNullOrEmpty(valueY)) return _direction == ListSortDirection.Ascending ? -1 : 1;
            
            // Try numeric comparison first
            if (double.TryParse(valueX, out double numX) && double.TryParse(valueY, out double numY))
            {
                int result = numX.CompareTo(numY);
                return _direction == ListSortDirection.Ascending ? result : -result;
            }
            
            // Fall back to string comparison
            int stringResult = string.Compare(valueX, valueY, StringComparison.OrdinalIgnoreCase);
            return _direction == ListSortDirection.Ascending ? stringResult : -stringResult;
        }
        
        private string? GetColumnValue(FileItem fileItem)
        {
            if (_appConfig.CustomKeywords.Contains(_columnName))
            {
                return fileItem.CustomKeywords.TryGetValue(_columnName, out string? customValue) ? customValue : null;
            }
            
            if (_appConfig.FitsKeywords.Contains(_columnName))
            {
                return fileItem.FitsKeywords.TryGetValue(_columnName, out string? fitsValue) ? fitsValue : null;
            }
            
            return null;
        }
    }
}
