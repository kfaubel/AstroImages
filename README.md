# Astro Images 

A simple image viewer built with Electron that allows you to browse folders, view images review metadata and then move images to be processed or ignored.

There are other tools for reviewing FITS files including ASIFitsView and Pixinsight tools like Blink and SubFramSelector.  These are good but if you want to quickly review a folder of images and FITS files with a simple interface, this app aims to fill that niche. 

Particularly useful for astrophotographers who use NINA.  If NINA is configured to save metadata in the filename, this app can parse that information and display it in columns for easy review.

Consider a NINA image file path format like: 
```
$$SEQUENCETITLE$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS_$$RMS$$_HFR_$$HFR$$_Stars_$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
An example file path might be:
```
...\Tadpole Nebula\NIGHT_2023-10-19\Light\2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits

```
A review of the tracking RMS, star HFR and the star count can provide a quick assessment of image quality.  This app can parse those values from the filename and display them in columns for easy sorting and identification of images to keep or discard.

This app also provides a way to view FITS headers so that they can also be sorted and reviewed along with the filename parsed keywords.

It would be nice if this data were available to Windows Explorer so that columns could added and sorted but unfortunately that is not possible.

## Notes
- This application is currently in **Beta**
- This application is currently only available for Windows
- There is no warranty and there are surely bugs
- This applicaiton is not (yet) digitally signed
 
## Development Installation or if you want to build from source

1. Make sure you have Node.js installed
2. Clone or download this project
3. Open terminal in the project directory
4. Run: `npm install`
5. Run: `npm start` to launch the app
## Usage

### Development Mode
```bash
npm run dev
```
This runs the app with developer tools enabled.

# Development
npm start                 # Start the app in development mode

# Packaging  
npm run package          # Create executable package
npm run make             # Create distributable installers
npm run make:win         # Create Windows installer (current platform)

# Publishing (when ready)
npm run publish          # Publish to GitHub releases

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



## Filename Parsing Feature

The application can parse structured filenames to extract meaningful information:

### Setting Up Filename Keywords
1. Go to Options → Custom Keywords...
2. Add keywords that appear in your filenames (e.g., "RMS", "HFR", "Stars")
3. Test with example filenames to see how parsing works
4. Save your configuration


## FITS Header Display
For astronomical imaging workflows, the application can display FITS header values as columns:

### Setting Up FITS Headers
1. Go to Options → FITS Headers...
2. Select from common astronomical headers (OBJECT, EXPTIME, FILTER, etc.)
3. Add custom headers by typing the keyword name
4. Save your configuration to see the headers as columns


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