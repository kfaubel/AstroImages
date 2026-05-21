# AstroImages Documentation

Welcome to AstroImages - an image viewer and analysis tool for astronomical imaging.

## Features

- **View FITS and XISF astronomical images** - Includes display of metadata for these file formats
- **View other common image formats** - JPG, PNG, BMP, GIF, WEBP and TIFF
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View filename metadata as well as info in FITS/XISF headers
- **Select and move images** - Select images and then move them to a processing folder or the recycle bin
- **Image statistics and analysis** - Examine pixel statistics and image properties including median values
- **Histogram display** - Optional histogram panel with linear and logarithmic scales (can be hidden via Options)
- **Floating windows** - Detach image and histogram viewers for multi-monitor setups
- **Auto-stretch control** - Adjustable stretch factor for better image visibility
- **Dark mode** - Switch between light and dark themes for comfortable viewing


## Getting Started

1. **Loading a Folder of Images**: Go to File --> Open Folder to load a new directory of files.
2. **Add Columns to File List**: Add custom filename keywords or FITS keywords using the Options menu.
3. **Viewing Images**: Click on a file to view that file, or use the controls below the file list.
4. **View Image Metadata**: Click the (i) icon to view FITS or XISF metadata.
5. **Navigation**: Use the controls on the right panel to navigate through your images.
6. **Select Images**: Use the checkboxes next to each file to select images for moving or deletion.
7. **Move Selected Images**: Click "Move Selected" button to organize your files.
8. **Full Screen**: Click the Full Screen button to view an image in full screen mode.
9. **Float Windows**: Click the float button (top-right of image viewer) to detach the image and histogram to separate windows for multi-monitor setups. 


## Custom Keywords
If you use NINA (N.I.N.A. - Nighttime Imaging 'N' Astronomy) with specified filenames like:
```
$$SEQUENCETITLE$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS:$$RMSARCSEC$$_HFR:$$HFR$$_ECC_$$ECCENTRICITY$$_FWHM_$$FWHM$$_Stars_$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
That generates filenames such as:
```
2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_ECC_0.50_FWHM_0.00_Stars_2029_100_10.00s_-9.60C_0052.fits
```

This app can parse RMS, HFR, ECCENTRICITY, FWHM, star count, and other quality metrics directly from the filename to speed up sorting subimages. Select Custom Keywords in the Options menu and use the exact keyword labels (e.g., RMS, HFR, ECC, FWHM, Stars) to add them as columns in the file list.

**Color Coding**: Custom keyword values are displayed in green, FITS keyword values in blue, and median values in purple in the file list for easy identification.

### RMS
A good RMS value is typically below 1.0 for well-focused images.  Or, more precicely, less than the Image Scale (arcseconds/pixel) of the imaging setup.  The Image scale is calculated as: (206.3 * pixel size in microns) / (focal length in mm).  For example, with a pixel size of 3.76 microns and focal length of 800mm, the image scale is about 0.97 arcseconds/pixel.  So an RMS below 0.97 would be considered good.

### HFR
HFR (Half Flux Radius) indicates the radius (in pixels) that contains half of a star's total flux.  Lower HFR values indicate sharper stars.  Typical good values are between 1.5 and 3.0 pixels, depending on seeing conditions and focal length.

### Stars
The star count indicates the number of stars detected in the image.  Higher star counts generally indicate better focus and sky conditions, but this can vary based on the field of view and exposure settings.  If the star count is very low (e.g., below 50), it may indicate poor focus, clouds or light pollution.  Sorting on the Stars column can help cull out poorly focused or cloudy images.

## FITS/XISF Keywords
Specific FITS/XISF header keywords can be added as columns in the file list for sorting. Use the Options menu to add desired keywords.

Examples of useful keywords include: GAIN, EXPTIME, FILTER, DATE-OBS, OBJECT, TELESCOP, INSTRUME, etc.

## Median Display
The median pixel value is automatically calculated when a folder is loaded. You can display it in two formats via the Options menu:
- **Normalized (0.0-1.0)**: Shows median as a decimal value with 5 decimal places (e.g., 0.03604)
- **16-bit range (0-65535)**: Shows median as an integer value (e.g., 2362)

The median column is displayed in purple to distinguish it from other data types.

## Histogram Panel
A histogram panel can be shown below the image viewer via the Options menu. The histogram can be displayed in linear or logarithmic scale and automatically updates when you select different images. The histogram panel can be hidden to maximize the image viewing area.

## Floating Windows
For users with multiple monitors, click the float button at the top-right of the image viewer to detach the image and histogram into separate windows. These can be dragged to another monitor and shown full screen, allowing you to see the full file list and images simultaneously. Click the re-dock button in the toolbar to return windows to the main interface.

## Support
For technical support or feature requests, please refer to the Github repository: [AstroImages on Github](https://github.com/kfaubel/AstroImages/issues).


Software updates are published here: (https://github.com/kfaubel/AstroImages/releases)