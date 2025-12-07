# AstroImages Documentation

Welcome to AstroImages - an image viewer and analysis tool for astronomical imaging.

## Features

- **View FITS and XISF astronomical images** - Includes display of metadata for these file formats
- **View other common image foramts** - JPG, PNG, BMP, GIF, WEBP and TIFF
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View filename metadata as well as info in FITS/XISF headers
- **Select and move images** - Select images and then move them to a processing folder or the recycle bin
- **Image statistics and analysis** - Examine pixel statistics and image properties
- **Dark mode** - Switch between light and dark themes for comfortable viewing


## Getting Started

1. **Loading a Folder of Images**: Go to File --> Open Folder to load a new directory of files.
2. **Add Columns to File List**: Add custom filename keywords or FITS keywords using the Options menu. 
2. **Vieweing Images**: Click on a file to view that file, or use the controls below the file list.
3. **View Image Metadata**: Click the (i) icon to view FITS or XISF metadata.
2. **Navigation**: Use the controls on the right panel to navigate through your images.
3. **Select Images**: Use the checkboxes next to each file to select images for moving or deletion.
4. **Move Selected Images**: Click "Move Selected" button to organize your files.
4. **Full Screen** - Click the Full Screen button to view an image in full screen mode. 


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

### RMS
A good RMS value is typically below 1.0 for well-focused images.  Or, more precicely, less than the Image Scale (arcseconds/pixel) of the imaging setup.  The Image scale is calculated as: (206.3 * pixel size in microns) / (focal length in mm).  For example, with a pixel size of 3.76 microns and focal length of 800mm, the image scale is about 0.97 arcseconds/pixel.  So an RMS below 0.97 would be considered good.

### HFR
HFR (Half Flux Radius) indicates the radius (in pixels) that contains half of a star's total flux.  Lower HFR values indicate sharper stars.  Typical good values are between 1.5 and 3.0 pixels, depending on seeing conditions and focal length.

### Stars
The star count indicates the number of stars detected in the image.  Higher star counts generally indicate better focus and sky conditions, but this can vary based on the field of view and exposure settings.  If the star count is very low (e.g., below 50), it may indicate poor focus, clouds or light pollution.  Sorting on the Stars column can help cull out poorly focused or cloudy images.

## FITS/XISF Keywords
Specific FITS/XISF header keywords can be added as columns in the file list for sorting. Use the Options menu to add desired keywords. 


Examples of useful keywords include: GAIN, EXPTIME, FILTER, DATE-OBS, OBJECT, TELESCOP, INSTRUME, etc.

## Support
For technical support or feature requests, please refer to the Github repository: [AstroImages on Github](https://github.com/kfaubel/AstroImages/issues).


Software updates are published here: (https://github.com/kfaubel/AstroImages/releases)