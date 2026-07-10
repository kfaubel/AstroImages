# Getting Started with ApexAstro

Welcome to ApexAstro — an image viewer and analysis tool for astronomical imaging.

This application allows a user to quickly review the metadata (RMS, HWF, Stars, Eccentricity, etc.) for a directory of images and to preview the images themselves. 

Much of this data comes from N.I.N.A. either directly from the FITS headers or encoded in the filename.  

# Super Quick Getting Started

You can get going very quickly by opening a folder of fits files.  Then go to Settings → Fits Keywords and pick the values you want to see.  Common values in the fits metadata include: Dec, RA, Exposure, Gain, and Filter.  Once set, you can sort or any of the columns and easily blink through the images.

Unfortunately some important metadata is not available directly from the FITS headers and must be encoded in the filename.  This includes RMS, HFR, Stars, and Eccentricity.  See the next steps to configure N.I.N.A. to write the fits files with descriptive filenames.

# Configure the File Pattern in N.I.N.A.

In N.I.N.A. go to Options → Imaging and paste the following pattern into the Image file pattern field.

```
$$SEQUENCETITLE$$\NIGHT_$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_RMS_$$RMSARCSEC$$_HFR_$$HFR$$_ECC_$$ECCENTRICITY$$_FWHM_$$FWHM$$_Stars_$$STARCOUNT$$_$$GAIN$$_$$EXPOSURETIME$$s_$$SENSORTEMP$$C_$$FRAMENR$$
```
The filenames will then look like this:
```
...\M33\NIGHT_2015-12-31\LIGHT\2016-01-01_12-00-00_L_RMS_0.65_HFR_3.25_ECC_0.66_FWHM_4.23_Stars_3294_1600_10.21s_-15C_0001.fits
```

The directory part of the pattern is optional and can be adjusted for your needs.

The leading "$$DATETIME$$", the "RMS", "HFR", "ECC", "Stars" labels and values ($$RMSARCSEC", $$HFR$$, ...) are parsed and displayed in sortable columns in the file list.  The columns headers can also be dragged to reorder the metadata fields.

# Settings in ApexAstro

Launch the application and click on the settings gear and open Custom Keywords... .  This is how ApexAstro know where the metadata values are in the filename.

Add these new keywords: RMS, HFR, Stars and ECC .

In the File List Options you can unselect "Show full filename" and then select the "Show time" and "Show frame" options. The full filename is long and redundent with the new columns that we added.

Once set, open a folder with fits files and it will look something like this:
<img alt="ApexAstro" src="ApexAstro view.png" />

You can also go to settings and add columns for any value that is stored in the fits header or, if you use the Session Metadata plugin, you can get additional metadata that is not encoded in the filename or in the fits header.

# Browse the Images

- Click any file in the list to view it
- Click the icon in the Info column to see all of the fits header data
- Click in the checkbox for each row to mark the image
- Click Move Selected... to move the selected files to another directory or send to the recycle bin
- Pan and zoom the image to see details
- You can switch to full screen or pop out the image pane and move to another monitor.
- Press play to blink the images for a quick visual inspection.
