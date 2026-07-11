# ApexAstro Documentation

Welcome to ApexAstro - an image viewer and analysis tool for astronomical imaging.

## Features

- **View FITS and XISF astronomical images** - Includes display of metadata for these file formats
- **View other common image formats** - JPG, PNG, BMP, GIF, WEBP and TIFF
- **Zoom and pan controls** - Navigate through your images with mouse wheel zoom and drag-to-pan functionality
- **File metadata display** - View filename metadata as well as info in FITS/XISF headers
- **Graph display of metadata** - Visualize metadata values for all images in the file list using graphs
- **Select and move images** - Select images and then move them to a processing folder or the recycle bin
- **Image statistics and analysis** - Examine pixel statistics and image properties including median values
- **Histogram display** - Optional histogram panel with linear and logarithmic scales (can be hidden via Options)
- **Floating windows** - Detach image and histogram viewers for multi-monitor setups
- **Auto-stretch control** - Adjustable stretch factor for better image visibility
- **Dark mode** - Switch between light and dark themes for comfortable viewing

## File List Columns
Getting Started gives a quick overview of how to configure N.I.N.A. to generate filenames with embedded metadata and how to set up ApexAstro to parse these keywords so that the metadata can be displayed as columns in the file list.

Any metadata value that can be encoded in the image filename can be extracted and displayed as a custom keyword.

Any metadata in the FITS header can be extracted and displayed in the file list as well.

If you use the Session Metadata plugin in N.I.N.A., additional metadata that is not stored in the FITS header or filename can also be extracted and displayed as custom keywords in ApexAstro. This allows for a more comprehensive view of your imaging session's data directly within the application.

Columns can be dragged to change their order in the file list.  You can also click on a column header to sort the images based on that column's values.

### RMS
A good RMS value is typically below 1.0 for well-guided and well-focused images.  Or, more precicely, less than the Image Scale (arcseconds/pixel) of the imaging setup.  The Image scale is calculated as: (206.3 * pixel size in microns) / (focal length in mm).  For example, with a pixel size of 3.76 microns and focal length of 800mm, the image scale is about 0.97 arcseconds/pixel.  So an RMS below 0.97 would be considered good.

### HFR
HFR (Half Flux Radius) indicates the radius (in pixels) that contains half of a star's total flux.  Lower HFR values indicate sharper stars.  Typical good values are between 1.5 and 2.0 pixels, depending on seeing conditions and focal length.

### Stars
The star count indicates the number of stars detected in the image.  Higher star counts generally indicate better focus and sky conditions, but this can vary based on the field of view and exposure settings.  If the star count is very low (e.g., below 50), it may indicate poor focus, clouds or light pollution.  Sorting on the Stars column can help cull out poorly focused or cloudy images.

### Eccentricity

Eccentricity measures how elongated the stars appear in the image.  An eccentricity of 0 indicates perfectly round stars, while values closer to 1 indicate more elongated stars.  Values above 0.6 typically indicate poor tracking or focus.  Lower eccentricity values are generally better, indicating well-tracked and well-focused images.

### FITS/XISF Keywords
Specific FITS/XISF header keywords can be added as columns in the file list for sorting. Use the Options menu to add desired keywords.

Examples of useful keywords include: GAIN, EXPTIME, FILTER, DATE-OBS, OBJECT, TELESCOP, INSTRUME, etc.

### Median and Mean Display
If enabled, the median pixel value is automatically calculated when a folder is loaded. You can display it in two formats via the Options menu:
- **Normalized (0.0-1.0)**: Shows median as a decimal value with 5 decimal places (e.g., 0.03604)
- **16-bit range (0-65535)**: Shows median as an integer value (e.g., 2362)

Note: enabling this option slows down the loading of folders, especially with large numbers of images, as the median and mean values need to be calculated for each image.  They are cached to make opening the same folder faster in the future.

## Graph Display
Clicking the graph icon in the toolbar will display a plot of the metadata values for all of the images in the current folder. This allows for quick visual analysis of trends and outliers in the dataset.

## Histogram Panel
A histogram panel can be shown below the image viewer via the Options menu. The histogram can be displayed in linear or logarithmic scale and automatically updates when you select different images. The histogram panel can be hidden to maximize the image viewing area.

## Floating Windows
For users with multiple monitors, click the float button at the top-right of the image viewer to detach the image and histogram into separate windows. These can be dragged to another monitor and shown full screen, allowing you to see the full file list and images simultaneously. Click the re-dock button in the toolbar to return windows to the main interface.

## Support
For technical support or feature requests, please refer to the Github repository: [ApexAstro on Github](https://github.com/kfaubel/ApexAstro/issues).

Software updates are published here: [ApexAstro Releases](https://github.com/kfaubel/ApexAstro/releases)