using System.Collections.ObjectModel;  // For ObservableCollection<T>
using System.ComponentModel;           // For INotifyPropertyChanged interface
using System.Windows;                  // For Window base class and WPF framework

namespace AstroImages.Wpf
{
    /// <summary>
    /// Dialog window for selecting which FITS header keywords to display as columns in the file list.
    /// 
    /// FITS (Flexible Image Transport System) files contain metadata in their headers.
    /// This dialog presents a list of common astronomical keywords with descriptions,
    /// allowing users to choose which ones they want to see as columns.
    /// 
    /// Examples of useful FITS keywords:
    /// - OBJECT: Name of the target being imaged
    /// - EXPTIME: Exposure duration in seconds
    /// - FILTER: Filter used (R, G, B, Ha, OIII, etc.)
    /// - CCD-TEMP: Camera temperature during capture
    /// - GAIN: Camera gain setting
    /// </summary>
    public partial class FitsKeywordsDialog : Window
    {
        /// <summary>
        /// Collection of FITS keyword items that can be selected/deselected by the user.
        /// Each item contains the keyword name, description, and selection state.
        /// 
        /// ObservableCollection automatically notifies the UI when items change,
        /// enabling real-time updates to the checkbox list.
        /// </summary>
        public ObservableCollection<FitsKeywordItem> KeywordItems { get; } = new ObservableCollection<FitsKeywordItem>();

        /// <summary>
        /// Static list of common FITS header keywords with their descriptions.
        /// 
        /// This list includes standard FITS keywords, common astronomical imaging keywords,
        /// and standard photography EXIF metadata keywords that users might want to display.
        /// Each entry is a tuple containing:
        /// - Keyword: The exact FITS header keyword name or EXIF tag name
        /// - Description: Human-readable explanation of what the keyword represents
        /// 
        /// Static means this list is shared by all instances of the dialog and doesn't
        /// change during the application's lifetime. Readonly means it can't be reassigned
        /// after initialization.
        /// 
        /// The "new()" syntax is target-typed instantiation - the compiler infers the
        /// full type List<(string Keyword, string Description)> from the field declaration.
        /// </summary>
        private static readonly List<(string Keyword, string Description)> CommonFitsKeywords = new()
        {
            // Standard FITS keywords
            ("SIMPLE", "Conforms to FITS standard"),
            ("BITPIX", "Bits per data value"),
            ("NAXIS", "Number of data axes"),
            ("NAXIS1", "Length of data axis 1"),
            ("NAXIS2", "Length of data axis 2"),
            ("EXTEND", "FITS dataset may contain extensions"),
            ("BZERO", "Zero point in scaling equation"),
            ("BSCALE", "Linear factor in scaling equation"),
            
            // Astronomical imaging keywords
            ("OBJECT", "Target object name"),
            ("TELESCOP", "Telescope used to acquire data"),
            ("INSTRUME", "Instrument used to acquire data"),
            ("OBSERVER", "Observer who acquired the data"),
            ("DATE-OBS", "Date of the observation"),
            ("TIME-OBS", "Time of the observation"),
            ("EXPTIME", "Exposure time in seconds"),
            ("FILTER", "Filter name"),
            ("IMAGETYP", "Type of image"),
            ("GAIN", "CCD gain (electrons per ADU)"),
            ("RDNOISE", "CCD readout noise (electrons)"),
            ("XBINNING", "Binning factor in width"),
            ("YBINNING", "Binning factor in height"),
            ("CCD-TEMP", "CCD temperature in degrees C"),
            ("SET-TEMP", "CCD temperature setpoint in degrees C"),
            ("XPIXSZ", "Pixel Width in microns"),
            ("YPIXSZ", "Pixel Height in microns"),
            ("FOCALLEN", "Focal length of telescope in mm"),
            ("APTDIA", "Aperture diameter of telescope in mm"),
            ("APTAREA", "Aperture area of telescope in mm^2"),
            ("SBSTDVER", "SBIG File Format Version"),
            ("SWCREATE", "Software that created this file"),
            ("SWMODIFY", "Software that modified this file"),
            ("HISTORY", "Processing history"),
            ("COMMENT", "Descriptive comment"),
            ("BUNIT", "Physical unit of array values"),
            ("BLANK", "Value used for undefined array elements"),
            ("DATAMAX", "Maximum valid data value"),
            ("DATAMIN", "Minimum valid data value"),
            ("CRVAL1", "Coordinate value at reference point"),
            ("CRVAL2", "Coordinate value at reference point"),
            ("CRPIX1", "Array location of reference point in pixels"),
            ("CRPIX2", "Array location of reference point in pixels"),
            ("CDELT1", "Coordinate increment at reference point"),
            ("CDELT2", "Coordinate increment at reference point"),
            ("CTYPE1", "Coordinate type code"),
            ("CTYPE2", "Coordinate type code"),
            ("CUNIT1", "Units of coordinate increment and value"),
            ("CUNIT2", "Units of coordinate increment and value"),
            
            // Common photography/EXIF metadata keywords
            ("MAKE", "Camera manufacturer"),
            ("MODEL", "Camera model"),
            ("DATETIME", "Date and time of image creation"),
            ("EXPOSURE", "Exposure time in seconds"),
            ("ISO", "ISO speed rating"),
            ("APERTURE", "Aperture f-number"),
            ("FOCAL", "Focal length in mm"),
            ("WIDTH", "Image width in pixels"),
            ("HEIGHT", "Image height in pixels"),
            ("XRESOLUTION", "Horizontal resolution (DPI)"),
            ("YRESOLUTION", "Vertical resolution (DPI)"),
            ("SOFTWARE", "Software used to create image"),
            ("ARTIST", "Photographer/artist name"),
            ("COPYRIGHT", "Copyright information"),
            ("DESCRIPTION", "Image description"),
            ("ORIENTATION", "Image orientation"),
            ("WHITEBALANCE", "White balance setting"),
            ("FLASH", "Flash setting and mode"),
            ("GPS_LATITUDE", "GPS latitude coordinate"),
            ("GPS_LONGITUDE", "GPS longitude coordinate"),
            ("GPS_ALTITUDE", "GPS altitude in meters")
        };

        /// <summary>
        /// Default constructor - creates the dialog with all keywords available but none selected.
        /// Sets up the UI and populates the keyword list from the predefined common keywords.
        /// </summary>
        public FitsKeywordsDialog()
        {
            // Initialize UI components from XAML
            InitializeComponent();
            
            // Populate the KeywordItems collection with available FITS keywords
            InitializeKeywords();
            
            // Connect the checkbox list to our data collection
            KeywordCheckBoxes.ItemsSource = KeywordItems;
        }

        /// <summary>
        /// Constructor that initializes the dialog with pre-selected keywords.
        /// Used when editing existing FITS keyword configuration to show current selections.
        /// 
        /// Constructor chaining (: this()) ensures the default constructor runs first,
        /// setting up all UI components and populating the keyword list before we
        /// mark the selected items.
        /// </summary>
        /// <param name="selectedKeywords">Collection of keyword names that should be initially selected</param>
        public FitsKeywordsDialog(IEnumerable<string> selectedKeywords) : this()
        {
            // Loop through all available keyword items and mark them as selected
            // if they appear in the selectedKeywords collection
            foreach (var item in KeywordItems)
            {
                item.IsSelected = selectedKeywords.Contains(item.Keyword);
            }
        }

        /// <summary>
        /// Initializes the KeywordItems collection with all available FITS keywords.
        /// Sorts the keywords alphabetically to make them easier to find in the UI.
        /// 
        /// Uses tuple deconstruction in the foreach loop: the tuple (keyword, description)
        /// is automatically split into separate variables for cleaner code.
        /// </summary>
        private void InitializeKeywords()
        {
            // Sort keywords alphabetically by keyword name for better user experience
            // OrderBy creates a new sorted sequence, ToList() materializes it into a list
            var sortedKeywords = CommonFitsKeywords.OrderBy(kv => kv.Keyword).ToList();
            
            // Create FitsKeywordItem objects for each keyword and add to the collection
            // Tuple deconstruction: (keyword, description) extracts both values from the tuple
            foreach (var (keyword, description) in sortedKeywords)
            {
                KeywordItems.Add(new FitsKeywordItem(keyword, description));
            }
        }

        /// <summary>
        /// Returns a list of keyword names that the user has selected.
        /// This method is called by the parent window after the dialog closes with OK result.
        /// 
        /// Uses LINQ query methods:
        /// - Where: Filters to only items where IsSelected is true
        /// - Select: Projects each FitsKeywordItem to just its Keyword string
        /// - ToList: Converts the query result to a concrete List<string>
        /// 
        /// Method chaining allows complex operations to be written in a readable, left-to-right flow.
        /// </summary>
        /// <returns>List of selected FITS keyword names</returns>
        public List<string> GetSelectedKeywords()
        {
            return KeywordItems.Where(item => item.IsSelected).Select(item => item.Keyword).ToList();
        }

        /// <summary>
        /// Event handler for the "Select None" button click.
        /// Deselects all FITS keywords, providing a quick way to clear all selections
        /// and start over with a clean slate.
        /// 
        /// Since FitsKeywordItem implements INotifyPropertyChanged, setting IsSelected
        /// automatically updates the UI checkboxes through data binding.
        /// </summary>
        /// <param name="sender">The "Select None" button</param>
        /// <param name="e">Event arguments for the click event</param>
        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            // Clear all selections by setting IsSelected to false for every item
            foreach (var item in KeywordItems)
            {
                item.IsSelected = false;  // This automatically updates the UI checkbox
            }
        }

        /// <summary>
        /// Event handler for the OK button click.
        /// Indicates that the user wants to apply their FITS keyword selections.
        /// The calling code should call GetSelectedKeywords() to retrieve the final selection.
        /// </summary>
        /// <param name="sender">The OK button</param>
        /// <param name="e">Event arguments for the click event</param>
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult to true to indicate user accepted the changes
            DialogResult = true;
            
            // Close the dialog - calling code can now call GetSelectedKeywords()
            Close();
        }

        /// <summary>
        /// Event handler for the Cancel button click.
        /// Indicates that the user wants to discard their changes and keep the original selection.
        /// </summary>
        /// <param name="sender">The Cancel button</param>
        /// <param name="e">Event arguments for the click event</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult to false to indicate user cancelled
            DialogResult = false;
            
            // Close the dialog - calling code should ignore any changes
            Close();
        }
    } // End of FitsKeywordsDialog class

    /// <summary>
    /// Data model representing a single FITS keyword that can be selected/deselected.
    /// 
    /// This class implements INotifyPropertyChanged to support WPF data binding.
    /// When the IsSelected property changes, the UI checkboxes automatically update.
    /// 
    /// This is a common MVVM pattern: create simple data objects that notify
    /// the UI of changes, allowing for automatic synchronization between
    /// the data model and the user interface.
    /// </summary>
    public class FitsKeywordItem : INotifyPropertyChanged
    {
        // Private backing field for the IsSelected property
        // Uses underscore prefix following C# naming conventions
        private bool _isSelected;

        /// <summary>
        /// The FITS keyword name (e.g., "OBJECT", "EXPTIME", "FILTER").
        /// This is read-only after construction since keyword names don't change.
        /// </summary>
        public string Keyword { get; }
        
        /// <summary>
        /// Human-readable description of what this FITS keyword represents.
        /// This is read-only after construction and is displayed in the UI to help users
        /// understand what each keyword means.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether this FITS keyword is currently selected by the user.
        /// 
        /// This property uses the full property syntax (not auto-property) because
        /// we need to fire a change notification when the value changes. This
        /// enables WPF data binding to work properly.
        /// 
        /// Property Pattern:
        /// - get: Returns the private backing field value
        /// - set: Updates the backing field AND fires PropertyChanged event
        /// - nameof(IsSelected): Compile-time safe way to get property name as string
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                // Notify WPF that this property changed so UI can update
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        /// <summary>
        /// Constructor that creates a new FITS keyword item with the specified keyword and description.
        /// The item starts unselected (IsSelected = false by default).
        /// </summary>
        /// <param name="keyword">The FITS keyword name (e.g., "OBJECT")</param>
        /// <param name="description">Human-readable description of the keyword</param>
        public FitsKeywordItem(string keyword, string description)
        {
            Keyword = keyword;
            Description = description;
            // IsSelected starts as false (default bool value)
        }

        /// <summary>
        /// Event required by INotifyPropertyChanged interface.
        /// WPF subscribes to this event to know when properties change so it can update the UI.
        /// The ? means this event can be null (no subscribers).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Helper method to fire the PropertyChanged event.
        /// Called whenever a property value changes to notify WPF of the change.
        /// 
        /// The ?. operator is null-conditional: only invoke the event if there are subscribers.
        /// This prevents null reference exceptions if no one is listening to the event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    } // End of FitsKeywordItem class
} // End of AstroImages.Wpf namespace