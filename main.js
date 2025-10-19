const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const { createCanvas } = require('canvas');

let mainWindow;

// Simple FITS header parser
function parseFitsHeader(buffer) {
  const blockSize = 2880; // FITS header is in 2880-byte blocks
  let headerSize = blockSize;
  let foundEnd = false;
  
  // Find the actual header size by looking for END keyword
  for (let offset = 0; offset < Math.min(buffer.length, blockSize * 10); offset += blockSize) {
    const block = buffer.slice(offset, offset + blockSize).toString('ascii');
    if (block.includes('END ')) {
      headerSize = offset + blockSize;
      foundEnd = true;
      break;
    }
    headerSize = offset + blockSize;
  }
  
  if (!foundEnd) {
    console.warn('END keyword not found in FITS header, using default size');
  }
  
  const headerText = buffer.slice(0, headerSize).toString('ascii');
  const lines = headerText.match(/.{80}/g) || [];
  
  const header = { _headerSize: headerSize };
  for (const line of lines) {
    if (line.startsWith('END ')) break;
    
    const match = line.match(/^(\w+)\s*=\s*([^/]*)/);
    if (match) {
      const key = match[1].trim();
      let value = match[2].trim();
      
      // Remove quotes for string values
      if (value.startsWith("'") && value.endsWith("'")) {
        value = value.slice(1, -1).trim();
      } else if (!isNaN(value) && value !== '') {
        value = parseFloat(value);
      }
      
      header[key] = value;
    }
  }
  
  return header;
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    },
    icon: path.join(__dirname, 'assets', 'icon.png') // Optional: add an icon
  });

  mainWindow.loadFile('index.html');

  // Open developer tools in development
  if (process.argv.includes('--dev')) {
    mainWindow.webContents.openDevTools();
  }

  // Add keyboard shortcut to toggle developer tools
  mainWindow.webContents.on('before-input-event', (event, input) => {
    if (input.key === 'F12') {
      mainWindow.webContents.toggleDevTools();
    }
  });
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// IPC handlers
ipcMain.handle('select-directory', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openDirectory']
  });

  if (!result.canceled && result.filePaths.length > 0) {
    return result.filePaths[0];
  }
  return null;
});

ipcMain.handle('read-directory', async (event, directoryPath) => {
  try {
    const files = fs.readdirSync(directoryPath);
    const imageFiles = files.filter(file => {
      const ext = path.extname(file).toLowerCase();
      return ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg', '.fits', '.fit', '.fts'].includes(ext);
    }).map(file => ({
      name: file,
      path: path.join(directoryPath, file),
      isFits: ['.fits', '.fit', '.fts'].includes(path.extname(file).toLowerCase())
    }));
    
    return imageFiles;
  } catch (error) {
    console.error('Error reading directory:', error);
    return [];
  }
});

ipcMain.handle('get-file-path', async (event, filePath) => {
  return `file://${filePath}`;
});

ipcMain.handle('check-directory-exists', async (event, directoryPath) => {
  try {
    const stats = fs.statSync(directoryPath);
    return stats.isDirectory();
  } catch (error) {
    return false;
  }
});

ipcMain.handle('process-fits-file', async (event, filePath) => {
  try {
    const buffer = fs.readFileSync(filePath);
    
    // Parse FITS header
    const header = parseFitsHeader(buffer);
    console.log('FITS Header:', header); // Debug log
    
    const width = header.NAXIS1;
    const height = header.NAXIS2;
    const bitpix = header.BITPIX || -32;
    
    if (!width || !height) {
      throw new Error(`Unable to determine image dimensions from FITS header. NAXIS1: ${width}, NAXIS2: ${height}`);
    }

    // Use header size from parser
    const headerSize = header._headerSize;

    // Extract data portion
    const dataBuffer = buffer.slice(headerSize);
    const numPixels = width * height;
    
    console.log(`Processing FITS: ${width}x${height}, BITPIX: ${bitpix}, Header size: ${headerSize}, Data buffer size: ${dataBuffer.length}`);
    
    // Calculate expected data size
    let bytesPerPixel;
    switch (bitpix) {
      case 8: bytesPerPixel = 1; break;
      case 16: bytesPerPixel = 2; break;
      case 32: bytesPerPixel = 4; break;
      case -32: bytesPerPixel = 4; break;
      case -64: bytesPerPixel = 8; break;
      default: bytesPerPixel = 4; break;
    }
    
    const expectedDataSize = numPixels * bytesPerPixel;
    if (dataBuffer.length < expectedDataSize) {
      throw new Error(`Insufficient data in FITS file. Expected: ${expectedDataSize}, Got: ${dataBuffer.length}`);
    }
    
    // Parse data based on BITPIX using DataView for proper byte order handling
    const dataView = new DataView(dataBuffer.buffer, dataBuffer.byteOffset);
    const data = new Array(numPixels);
    
    for (let i = 0; i < numPixels; i++) {
      const offset = i * bytesPerPixel;
      try {
        switch (bitpix) {
          case 8:
            data[i] = dataView.getUint8(offset);
            break;
          case 16:
            data[i] = dataView.getInt16(offset, false); // false = big-endian
            break;
          case 32:
            data[i] = dataView.getInt32(offset, false);
            break;
          case -32:
            data[i] = dataView.getFloat32(offset, false);
            break;
          case -64:
            data[i] = dataView.getFloat64(offset, false);
            break;
          default:
            data[i] = dataView.getFloat32(offset, false);
        }
      } catch (err) {
        console.error(`Error reading pixel ${i} at offset ${offset}:`, err);
        data[i] = 0; // Default value for corrupted pixels
      }
    }

    // Create canvas for image conversion
    const canvas = createCanvas(width, height);
    const ctx = canvas.getContext('2d');
    const imageData = ctx.createImageData(width, height);
    
    // Find min and max values for scaling (efficient method for large arrays)
    let min = data[0];
    let max = data[0];
    for (let i = 1; i < data.length; i++) {
      if (data[i] < min) min = data[i];
      if (data[i] > max) max = data[i];
    }
    
    // Handle edge case where min equals max
    if (min === max) {
      max = min + 1;
    }
    
    // Apply scaling to convert to 0-255 range
    for (let i = 0; i < data.length; i++) {
      const normalized = Math.max(0, Math.min(1, (data[i] - min) / (max - min)));
      const value = Math.floor(normalized * 255);
      
      const pixelIndex = i * 4;
      imageData.data[pixelIndex] = value;     // Red
      imageData.data[pixelIndex + 1] = value; // Green
      imageData.data[pixelIndex + 2] = value; // Blue
      imageData.data[pixelIndex + 3] = 255;   // Alpha
    }
    
    ctx.putImageData(imageData, 0, 0);
    
    // Return base64 data URL
    return canvas.toDataURL('image/png');
  } catch (error) {
    console.error('Error processing FITS file:', error);
    // Return a placeholder image for FITS files that can't be processed
    const canvas = createCanvas(400, 300);
    const ctx = canvas.getContext('2d');
    
    // Draw a simple placeholder
    ctx.fillStyle = '#f0f0f0';
    ctx.fillRect(0, 0, 400, 300);
    ctx.fillStyle = '#666';
    ctx.font = '16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('FITS File', 200, 140);
    ctx.fillText('(Processing Error)', 200, 160);
    ctx.fillText('File may be corrupted or unsupported', 200, 180);
    
    return canvas.toDataURL('image/png');
  }
});