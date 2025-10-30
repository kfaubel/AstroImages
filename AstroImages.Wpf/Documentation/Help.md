# AstroImages Documentation

Welcome to AstroImages - an image viewer and analysis tool for astronomical imaging.

## Features

- **View FITS and XISF astronomical images** - Support for both FITS and XISF file formats commonly used in astrophotography
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View detailed FITS/XISF headers and image metadata
- **Image statistics and analysis** - Examine pixel statistics and image properties

## Getting Started

1. **Loading a Folder of Images**: Go to File --> Open Folder to load a new directory of files.
2. **Add Columns to File List**: Add custom filename keywords or FITS keywords using the Options menu.  See keywords documentation for details.
2. **Vieweing Images**: Click on a file to view that file, or use the controls below the file list to step through the images
3. **View Image Metadata**: Right-click on images to access metadata and analysis tools
4. **Move Selected Images**: Select images by using the checkboxes and click the "Move Selected" button to organize your files.
2. **Navigation**: Use the controls on the right panel to navigate through your images
3. **Zooming**: The zoom controls allow you to examine details or fit the entire image to the window
4. **Image Analysis**: Right-click on images to access metadata and analysis tools

## Supported File Formats

- **FITS** (Flexible Image Transport System) - The standard format for astronomical data
- **XISF** (Extensible Image Serialization Format) - Modern format with advanced features for astrophotography

## Custom Keywords
If you use NINA (N.I.N.A. - Nighttime Imaging 'N' Astronomy) with specified filenames like:
```
$$SEQUENCETITLE$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS:$$RMS$$_HFR:$$HFR$$_Stars:$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
That generates filenames such as:
```
2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits
```

This app can parse RMS, HFR, star count, and other quality metrics directly from the filename to speed up sorting subimages.  Select Custom Keywords in the Options menu and use the exact keyword labels (e.g., RMS, HFR, Stars) to add them as columns in the file list.

## FITS Keywords
Specific FITS header keywords can be added as columns in the file list for sorting and filtering. Use the Options menu to add desired keywords. 

Examples of useful keywords include: GAIN, EXPTIME, FILTER, DATE-OBS, OBJECT, TELESCOP, INSTRUME, etc.

## Support
For technical support or feature requests, please refer to the Githuyb repository.