# Image Viewer - Electron App

A simple two-pane image viewer built with Electron that allows you to browse folders and view images.

## Features

- **Two-pane layout**: File browser on the left, image viewer on the right
- **Resizable panes**: Drag the divider to adjust the width of the file list panel
- **Folder selection**: Click "Select Folder" to choose a directory
- **Image filtering**: Shows image files and FITS files (jpg, jpeg, png, gif, bmp, webp, svg, fits, fit, fts)
- **FITS file support**: Automatically processes and displays FITS files used in astronomy and scientific imaging
- **Filename parsing**: Configure keywords to extract values from structured filenames
- **Keyword columns**: Display extracted values in organized columns for easy sorting and identification
- **Column sorting**: Click any column header to sort files by that attribute (with filename as secondary sort)
- **Click to view**: Click any image file in the list to display it
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
3. If no previous folder, click "Select Folder" to choose a directory
4. (Optional) Click "Configure Keywords" to set up filename parsing
5. (Optional) Drag the vertical divider to resize the file list pane
6. Click column headers to sort files by different attributes
7. Hover over filenames or keyword values to see full text in tooltips
8. The left pane will show all image files with extracted keyword values
9. Click on any image file to view it in the right pane
10. Use the up/down arrow keys to navigate between images

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
1. Click the "Configure Keywords" button
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