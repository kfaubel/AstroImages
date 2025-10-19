# FITS File Testing

If you're getting "processing error" when selecting FITS files, here are some troubleshooting steps:

## Common Issues:

1. **File Format**: Make sure the file is a valid FITS file with proper header structure
2. **File Size**: Very large FITS files may take time to process
3. **Data Type**: Some exotic FITS formats may not be supported

## Testing FITS Support:

To test if FITS processing is working, you can:

1. **Check Console**: Open Developer Tools (F12) and look for error messages in the Console tab
2. **Try Different Files**: Test with different FITS files if available
3. **Check File Extension**: Ensure files have .fits, .fit, or .fts extensions

## Debug Information:

The application will now log detailed information about FITS processing:
- Header parsing results
- Image dimensions detected
- Data buffer sizes
- Processing steps

## Creating Test FITS Files:

If you have Python with astropy installed, you can create a simple test FITS file:

```python
from astropy.io import fits
import numpy as np

# Create a simple 100x100 test image
data = np.random.random((100, 100)) * 1000
hdu = fits.PrimaryHDU(data)
hdu.writeto('test_image.fits', overwrite=True)
```

## Supported FITS Features:

- ✅ 8-bit unsigned integers (BITPIX = 8)
- ✅ 16-bit signed integers (BITPIX = 16)  
- ✅ 32-bit signed integers (BITPIX = 32)
- ✅ 32-bit floating point (BITPIX = -32)
- ✅ 64-bit floating point (BITPIX = -64)
- ✅ Big-endian byte order conversion
- ✅ Multiple header blocks
- ✅ Automatic intensity scaling

## Not Yet Supported:

- ❌ Compressed FITS files
- ❌ Multi-extension FITS files (only primary HDU)
- ❌ Complex data types
- ❌ World coordinate systems (WCS)