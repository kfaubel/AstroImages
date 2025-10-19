# Image Viewer - Electron App

A simple two-pane image viewer built with Electron that allows you to browse folders and view images.

## Features

- **Two-pane layout**: File browser on the left, image viewer on the right
- **Current directory**: Directory path displayed in the application title bar
- **File selection**: Checkbox column allows selecting one or multiple files for batch operations
- **File management**: Move selected files to other folders or trash with File → Move Selected...
- **Auto-refresh**: Automatically detects and updates when files are added, removed, or modified in the current folder
- **Menu system**: File menu with Open Folder option, Options menu for configuration, and keyboard shortcuts
- **Title bar display**: Current directory full path shown in application title bar
- **Resizable panes**: Drag the divider to adjust the width of the file list panel
- **Folder selection**: Use File → Open Folder... to choose a directory
- **Image filtering**: Shows image files and FITS files (jpg, jpeg, png, gif, bmp, webp, svg, fits, fit, fts)
- **FITS file support**: Automatically processes and displays FITS files used in astronomy and scientific imaging
- **Filename parsing**: Configure keywords to extract values from structured filenames
- **FITS header display**: Show FITS header values as columns for astronomical image metadata
- **Keyword columns**: Display extracted values in organized columns for easy sorting and identification
- **Column sorting**: Click any column header to sort files by that attribute (with filename as secondary sort)
- **Click to view**: Click any image file in the list to display it
- **Playback controls**: Navigation buttons above file list for automated slideshow functionality
  - Go to first/last image
  - Previous/next image navigation
  - Auto-slideshow with 2-second timing per image (waits for each image to fully load before starting timer)
  - Play/pause toggle with visual feedback
- **Zoom controls**: Image zoom functionality with buttons above image viewer and View menu options
  - Default view: Images automatically fit to available space (no scrollbars)
  - Zoom In/Out (10% to 500% range)
  - Actual Size (1:1 / 100%)
  - Fit to Window (scale to fit container)
  - Mouse wheel zoom (Ctrl + wheel)
  - Image panning when zoomed in (click and drag)
  - Keyboard shortcuts: Ctrl+Plus, Ctrl+Minus, Ctrl+0, Ctrl+F
- **Keyboard navigation**: Use arrow keys to navigate between images
- **Visual indicators**: FITS files are marked with special icons and badges
- **Image information**: Shows file name and type
- **Responsive design**: Clean, modern interface
- **Persistent settings**: Sidebar width, keywords, and last opened directory are saved between sessions
- **Smart tooltips**: Hover over filenames or long keyword values to see the full text
- **Optimized layout**: Keyword columns are kept narrow to maximize filename visibility

## Installation

1. Make sure you have Node.js installed
2. Clone or download this project
3. Open terminal in the project directory
4. Run: `npm install`

## Usage

### Development Mode
```bash
npm run dev
```
This runs the app with developer tools enabled.

### Production Mode
```bash
npm start
```

### Building for Distribution
```bash
npm run build
```

## How to Use

1. Launch the application
2. The app will automatically restore your last opened folder (if it still exists)
3. If no previous folder, use File → Open Folder... to choose a directory (Ctrl+O / Cmd+O)
4. (Optional) Use Options → Custom Keywords... to set up filename parsing
5. (Optional) Drag the vertical divider to resize the file list pane
6. Use the checkbox column to select one or multiple files for batch operations
7. Click column headers to sort files by different attributes
8. Hover over filenames or keyword values to see full text in tooltips
9. The left pane will show all image files with extracted keyword values
10. Click on any image file to view it in the right pane
11. Use the up/down arrow keys to navigate between images

### File Selection

The application includes a checkbox column for file selection:

- **Individual selection**: Check/uncheck individual files using the checkboxes in each row
- **Select all toggle**: Use the checkbox in the header to select or deselect all files at once
- **Selection indicator**: The header checkbox shows an indeterminate state when some (but not all) files are selected
- **Selection info**: When files are selected, a blue info bar shows the count and provides a "Clear Selection" button
- **Batch operations**: Selected files can be used for future batch operations (export, copy, etc.)
- **Persistent selection**: Selected files remain selected when sorting or changing column order

### File Management

The application provides file management capabilities for selected files:

- **Move to folder**: Select files and use File → Move Selected... to move them to another directory
- **Move to trash**: Use the move dialog to send files to the system trash/recycle bin
- **Smart validation**: Prevents overwriting files that already exist in the destination
- **Progress feedback**: Shows detailed results of move operations including any errors
- **Menu integration**: Move Selected menu item is only enabled when files are selected

### Auto-Refresh Monitoring

The application automatically monitors the current folder for changes:

- **Real-time updates**: File list refreshes automatically when files are added, removed, or renamed
- **Smart filtering**: Only responds to changes affecting image files (jpg, png, fits, etc.)
- **Background monitoring**: Uses efficient file system watchers with minimal performance impact
- **Error handling**: Gracefully handles watcher errors and network drive disconnections
- **Automatic cleanup**: Properly stops monitoring when switching folders or closing the application

### Menu System

The application features a comprehensive menu system:

**File Menu:**
- Open Folder... (Ctrl+O / Cmd+O): Select a directory to browse
- Move Selected... (Ctrl+M / Cmd+M): Move selected files to another location or trash
- Exit (Ctrl+Q / Cmd+Q): Close the application

**Options Menu:**
- Custom Keywords...: Configure filename parsing keywords for structured filenames
- FITS Headers...: Select FITS header keywords to display as columns for astronomical images

**View Menu:**
- Toggle Developer Tools (F12): Show/hide developer console
- Reload (Ctrl+R / Cmd+R): Refresh the application

## Keyboard Shortcuts

- **Ctrl+O** (Cmd+O on macOS): Open Folder
- **Ctrl+M** (Cmd+M on macOS): Move Selected Files (when files are selected)
- **Ctrl+R** (Cmd+R on macOS): Reload application
- **F12**: Toggle Developer Tools
- **Ctrl+Q** (Cmd+Q on macOS): Exit application
- **↑/↓ Arrow Keys**: Navigate between images in the list

## Persistent State

The application automatically saves and restores:
- **Last Directory**: Your most recently opened folder is restored on startup
- **Keyword Configuration**: Your defined keywords persist between sessions
- **Sidebar Width**: Your preferred file list panel width is remembered
- **Smart Restoration**: If the last directory no longer exists, the app gracefully falls back to the folder selection screen

## Interface Features

### Resizable Panes
- **Drag to resize**: Grab the vertical divider between the panes and drag to adjust width
- **Minimum widths**: File list has a minimum width of 250px, image pane minimum of 300px
- **Maximum width**: File list can expand up to 70% of the window width
- **Persistent sizing**: Your preferred width is saved and restored when you restart the app

### Column Sorting
- **Click headers**: Click any column header to sort by that attribute
- **Sort indicators**: Headers show arrows indicating current sort direction (↑ ascending, ↓ descending)
- **Toggle direction**: Click the same header again to reverse the sort order
- **Secondary sorting**: Files with identical primary sort values are sorted by filename
- **Smart sorting**: Text values are sorted alphabetically, empty values appear last

## Filename Parsing Feature

The application can parse structured filenames to extract meaningful information:

### Setting Up Keywords
1. Go to Options → Custom Keywords...
2. Add keywords that appear in your filenames (e.g., "date", "sample", "type")
3. Test with example filenames to see how parsing works
4. Save your configuration

### Filename Structure
The parser expects filenames with tokens separated by underscores (`_`):
- `sample_001_date_2023-10-19_type_raw.fits`
- `experiment_A_temp_25C_time_1200.jpg`
- `subject_P01_session_1_task_rest.png`

### How It Works
- Keywords are case-insensitive when matching
- The token immediately following a keyword becomes its value
- Multiple keywords can be extracted from a single filename
- Values are displayed in organized columns

### Examples
```
Filename: "sample_001_date_2023-10-19_type_raw.fits"
Keywords: ["sample", "date", "type"]
Result:
  sample: 001
  date: 2023-10-19  
  type: raw
```

## FITS Header Display

For astronomical imaging workflows, the application can display FITS header values as columns:

### Setting Up FITS Headers
1. Go to Options → FITS Headers...
2. Select from common astronomical headers (OBJECT, EXPTIME, FILTER, etc.)
3. Add custom headers by typing the keyword name
4. Save your configuration to see the headers as columns

### Common FITS Headers Supported
- **OBJECT**: Target/object name
- **EXPTIME/EXPOSURE**: Exposure time in seconds
- **FILTER**: Filter wheel position or filter name
- **DATE-OBS**: Date of observation
- **TELESCOP**: Telescope name
- **INSTRUME**: Instrument/camera name
- **IMAGETYP**: Image type (Light, Dark, Flat, Bias)
- **GAIN**: Detector gain setting
- **CCD-TEMP**: CCD/sensor temperature
- **BINNING**: Pixel binning factor
- And many more standard astronomical headers...

### How It Works
- Only FITS files will display values in FITS header columns
- Regular image files will show empty cells for FITS headers
- Headers are read directly from the FITS file metadata
- Values are displayed with tooltips for longer entries
- Columns can be sorted like any other column

## Supported Image Formats

- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- BMP (.bmp)
- WebP (.webp)
- SVG (.svg)
- **FITS (.fits, .fit, .fts)** - Flexible Image Transport System files commonly used in astronomy and scientific imaging

## Project Structure

```
AstroImages/
├── main.js          # Main Electron process
├── preload.js       # Preload script for secure IPC
├── index.html       # Main HTML structure
├── styles.css       # Application styles
├── renderer.js      # Renderer process logic
├── package.json     # Project configuration
└── README.md        # This file
```

## FITS File Support

FITS (Flexible Image Transport System) files are widely used in astronomy and scientific imaging. The application automatically:

- Detects FITS files (.fits, .fit, .fts extensions)
- Parses the FITS header to extract image dimensions
- Converts scientific data to viewable RGB format
- Applies automatic scaling for optimal visualization
- Displays FITS files with special visual indicators

**Note**: FITS files are processed on-the-fly and may take a moment to load depending on file size.

## Technologies Used

- **Electron**: Cross-platform desktop app framework
- **HTML5/CSS3**: User interface
- **JavaScript**: Application logic
- **Node.js**: File system operations
- **fits-reader**: FITS file parsing and processing
- **Canvas API**: Image data conversion for FITS files

## Security Features

- Context isolation enabled
- Node integration disabled in renderer
- Secure IPC communication via preload script

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