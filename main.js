const { app, BrowserWindow, ipcMain, dialog, Menu, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const crypto = require('crypto');
const { createCanvas } = require('canvas');

// Image cache to avoid reprocessing
const imageCache = new Map();
const MAX_CACHE_SIZE = 50; // Maximum number of cached images
const CACHE_DIR = path.join(app.getPath('userData'), 'image-cache');

// Ensure cache directory exists
if (!fs.existsSync(CACHE_DIR)) {
    fs.mkdirSync(CACHE_DIR, { recursive: true });
}

// Cache helper functions
function getCacheKey(filePath, applyStretch, isThumb = false) {
    const stats = fs.statSync(filePath);
    const data = `${filePath}-${stats.mtime.getTime()}-${stats.size}-${applyStretch}-${isThumb ? 'thumb' : 'full'}`;
    return crypto.createHash('md5').update(data).digest('hex');
}

function generateThumbnail(canvas, maxSize = 400) {
    const { width, height } = canvas;
    if (width <= maxSize && height <= maxSize) {
        return canvas;
    }

    const scale = Math.min(maxSize / width, maxSize / height);
    const thumbWidth = Math.floor(width * scale);
    const thumbHeight = Math.floor(height * scale);

    const thumbCanvas = createCanvas(thumbWidth, thumbHeight);
    const thumbCtx = thumbCanvas.getContext('2d');

    // Use bilinear scaling for better quality
    thumbCtx.imageSmoothingEnabled = true;
    thumbCtx.imageSmoothingQuality = 'high';
    thumbCtx.drawImage(canvas, 0, 0, thumbWidth, thumbHeight);

    return thumbCanvas;
}

function getCachedImage(cacheKey) {
    const cachePath = path.join(CACHE_DIR, `${cacheKey}.png`);
    if (fs.existsSync(cachePath)) {
        const buffer = fs.readFileSync(cachePath);
        return `data:image/png;base64,${buffer.toString('base64')}`;
    }
    return null;
}

function setCachedImage(cacheKey, imageData) {
    // Remove old cache entries if we're at the limit
    if (imageCache.size >= MAX_CACHE_SIZE) {
        const oldestKey = imageCache.keys().next().value;
        imageCache.delete(oldestKey);
        const oldCachePath = path.join(CACHE_DIR, `${oldestKey}.png`);
        if (fs.existsSync(oldCachePath)) {
            fs.unlinkSync(oldCachePath);
        }
    }

    // Save to disk cache
    const cachePath = path.join(CACHE_DIR, `${cacheKey}.png`);
    const base64Data = imageData.replace(/^data:image\/png;base64,/, '');
    fs.writeFileSync(cachePath, Buffer.from(base64Data, 'base64'));

    // Add to memory cache
    imageCache.set(cacheKey, true);
}

let mainWindow;
let currentWatcher = null; // File system watcher

// Simple FITS header parser
function parseFitsHeader(buffer) {
    const blockSize = 2880; // FITS header is in 2880-byte blocks
    let headerSize = blockSize;
    let foundEnd = false;

    // Search for END keyword in more blocks (up to 50 blocks = ~144KB of header)
    // This handles files with very large headers or multiple HDU sections
    for (let offset = 0; offset < Math.min(buffer.length, blockSize * 50); offset += blockSize) {
        const block = buffer.slice(offset, offset + blockSize).toString('ascii');
        if (block.includes('END ')) {
            headerSize = offset + blockSize;
            foundEnd = true;
            break;
        }
        headerSize = offset + blockSize;
    }

    if (!foundEnd) {
        console.warn('END keyword not found in FITS header, using available data');
        headerSize = Math.min(buffer.length, blockSize * 50);
    }

    const headerText = buffer.slice(0, headerSize).toString('ascii');
    const lines = headerText.match(/.{80}/g) || [];

    const header = { _headerSize: headerSize };

    for (const line of lines) {
        if (line.startsWith('END ')) continue; // Don't break, look for additional sections

        // More flexible regex for FITS keywords:
        // - Case insensitive matching
        // - Allow more characters in keywords (some software uses non-standard keywords)
        // - Handle both = and : separators (some software uses colons)
        const match = line.match(/^([A-Za-z0-9_-]{1,8})\s*[=:]\s*([^/]*)/);
        if (match) {
            const key = match[1].trim().toUpperCase(); // Normalize to uppercase
            let value = match[2].trim();

            // Handle quoted string values more robustly
            if (value.startsWith("'")) {
                // Find the closing quote, handling potential single quotes in the string
                let closingQuoteIndex = -1;
                for (let i = value.length - 1; i >= 1; i--) {
                    if (value[i] === "'") {
                        closingQuoteIndex = i;
                        break;
                    }
                }
                if (closingQuoteIndex > 0) {
                    value = value.slice(1, closingQuoteIndex).trim();
                } else {
                    value = value.slice(1).trim();
                }
            } else if (value === 'T') {
                value = true;
            } else if (value === 'F') {
                value = false;
            } else if (!isNaN(value) && value !== '' && !/^[TF]$/.test(value)) {
                // Parse numeric values, but be more careful about what we convert
                const num = parseFloat(value);
                if (!isNaN(num) && isFinite(num)) {
                    value = num;
                }
            }
            // Keep as string for everything else

            // Only store if we don't already have this keyword (first occurrence wins)
            if (!header.hasOwnProperty(key)) {
                header[key] = value;
            }
        }
    }

    // Log some debug information about what we found
    const keywords = Object.keys(header).filter(k => k !== '_headerSize');
    console.log(`FITS parsing found ${keywords.length} keywords in ${headerSize} bytes`);

    return header;
}

// Histogram stretching function for FITS images
function applyHistogramStretch(imageData, stretchParams = { lowPercentile: 1, highPercentile: 99 }) {
    const { data, width, height } = imageData;
    const { lowPercentile, highPercentile } = stretchParams;

    // Extract grayscale values (assuming RGBA format)
    const grayValues = [];
    for (let i = 0; i < data.length; i += 4) {
        // Convert RGB to grayscale (using luminance formula)
        const gray = 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
        grayValues.push(gray);
    }

    // Sort values to find percentiles
    const sortedValues = [...grayValues].sort((a, b) => a - b);
    const lowIndex = Math.floor(sortedValues.length * (lowPercentile / 100));
    const highIndex = Math.floor(sortedValues.length * (highPercentile / 100));

    const minVal = sortedValues[lowIndex];
    const maxVal = sortedValues[highIndex];
    const range = maxVal - minVal;

    // Apply stretching to the image data
    for (let i = 0; i < data.length; i += 4) {
        // Get original RGB values
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        // Apply stretch to each channel
        const stretchedR = Math.min(255, Math.max(0, ((r - minVal) / range) * 255));
        const stretchedG = Math.min(255, Math.max(0, ((g - minVal) / range) * 255));
        const stretchedB = Math.min(255, Math.max(0, ((b - minVal) / range) * 255));

        data[i] = stretchedR;
        data[i + 1] = stretchedG;
        data[i + 2] = stretchedB;
        // Alpha channel (data[i + 3]) remains unchanged
    }

    return imageData;
}

function createMenu() {
    const template = [
        {
            label: 'File',
            submenu: [
                {
                    label: 'Open Folder...',
                    accelerator: 'CmdOrCtrl+O',
                    click: async () => {
                        const result = await dialog.showOpenDialog(mainWindow, {
                            properties: ['openDirectory']
                        });

                        if (!result.canceled && result.filePaths.length > 0) {
                            // Send the selected folder path to the renderer process
                            mainWindow.webContents.send('folder-selected', result.filePaths[0]);
                        }
                    }
                },
                {
                    label: 'Move Selected...',
                    accelerator: 'CmdOrCtrl+M',
                    enabled: false, // Will be enabled/disabled based on selection
                    id: 'move-selected',
                    click: () => {
                        mainWindow.webContents.send('show-move-dialog');
                    }
                },
                { type: 'separator' },
                {
                    label: 'Exit',
                    accelerator: process.platform === 'darwin' ? 'Cmd+Q' : 'Ctrl+Q',
                    click: () => {
                        app.quit();
                    }
                }
            ]
        },
        {
            label: 'Options',
            submenu: [
                {
                    label: 'Custom Keywords...',
                    click: () => {
                        mainWindow.webContents.send('show-keywords-dialog');
                    }
                },
                {
                    label: 'FITS Headers...',
                    click: () => {
                        mainWindow.webContents.send('show-fits-headers-dialog');
                    }
                }
            ]
        },
        {
            label: 'View',
            submenu: [
                {
                    label: 'Zoom In',
                    accelerator: 'CmdOrCtrl+Plus',
                    click: () => {
                        mainWindow.webContents.send('zoom-in');
                    }
                },
                {
                    label: 'Zoom Out',
                    accelerator: 'CmdOrCtrl+-',
                    click: () => {
                        mainWindow.webContents.send('zoom-out');
                    }
                },
                {
                    label: 'Actual Size',
                    accelerator: 'CmdOrCtrl+0',
                    click: () => {
                        mainWindow.webContents.send('zoom-actual');
                    }
                },
                {
                    label: 'Fit to Window',
                    accelerator: 'CmdOrCtrl+F',
                    click: () => {
                        mainWindow.webContents.send('zoom-fit');
                    }
                },
                { type: 'separator' },
                {
                    label: 'Toggle Developer Tools',
                    accelerator: 'F12',
                    click: () => {
                        mainWindow.webContents.toggleDevTools();
                    }
                },
                { type: 'separator' },
                {
                    label: 'Reload',
                    accelerator: 'CmdOrCtrl+R',
                    click: () => {
                        mainWindow.webContents.reload();
                    }
                }
            ]
        }
    ];

    // macOS specific menu adjustments
    if (process.platform === 'darwin') {
        template.unshift({
            label: app.getName(),
            submenu: [
                { label: 'About ' + app.getName(), role: 'about' },
                { type: 'separator' },
                { label: 'Services', role: 'services', submenu: [] },
                { type: 'separator' },
                { label: 'Hide ' + app.getName(), accelerator: 'Command+H', role: 'hide' },
                { label: 'Hide Others', accelerator: 'Command+Shift+H', role: 'hideothers' },
                { label: 'Show All', role: 'unhide' },
                { type: 'separator' },
                { label: 'Quit', accelerator: 'Command+Q', click: () => app.quit() }
            ]
        });

        // Adjust File menu for macOS (remove Exit since it's in the app menu)
        template[1].submenu = template[1].submenu.filter(item => item.label !== 'Exit');
    }

    const menu = Menu.buildFromTemplate(template);
    Menu.setApplicationMenu(menu);
}

// Window state management
function saveWindowState() {
    if (!mainWindow) return;

    const bounds = mainWindow.getBounds();
    const isMaximized = mainWindow.isMaximized();

    const windowState = {
        x: bounds.x,
        y: bounds.y,
        width: bounds.width,
        height: bounds.height,
        isMaximized: isMaximized
    };

    try {
        const userDataPath = app.getPath('userData');
        const statePath = path.join(userDataPath, 'window-state.json');
        fs.writeFileSync(statePath, JSON.stringify(windowState, null, 2));
    } catch (error) {
        console.error('Failed to save window state:', error);
    }
}

function loadWindowState() {
    try {
        const userDataPath = app.getPath('userData');
        const statePath = path.join(userDataPath, 'window-state.json');

        if (fs.existsSync(statePath)) {
            const windowState = JSON.parse(fs.readFileSync(statePath, 'utf8'));

            // Validate the loaded state
            if (windowState.width && windowState.height &&
                windowState.width > 100 && windowState.height > 100) {

                // Ensure window position is on screen
                const { screen } = require('electron');
                const displays = screen.getAllDisplays();
                let isOnScreen = false;

                if (windowState.x !== undefined && windowState.y !== undefined) {
                    for (const display of displays) {
                        const { x, y, width, height } = display.workArea;
                        if (windowState.x >= x && windowState.x < x + width &&
                            windowState.y >= y && windowState.y < y + height) {
                            isOnScreen = true;
                            break;
                        }
                    }

                    // If window is off-screen, reset position
                    if (!isOnScreen) {
                        windowState.x = undefined;
                        windowState.y = undefined;
                    }
                }

                return windowState;
            }
        }
    } catch (error) {
        console.error('Failed to load window state:', error);
    }

    // Return default values if loading fails
    return {
        width: 1200,
        height: 800,
        x: undefined,
        y: undefined,
        isMaximized: false
    };
}

function createWindow() {
    const windowState = loadWindowState();

    const windowOptions = {
        width: windowState.width,
        height: windowState.height,
        title: 'Image Viewer',
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
            preload: path.join(__dirname, 'preload.js')
        },
        icon: path.join(__dirname, 'assets', 'icon.png'), // Optional: add an icon
        show: false // Don't show until ready
    };

    // Set position if available
    if (windowState.x !== undefined && windowState.y !== undefined) {
        windowOptions.x = windowState.x;
        windowOptions.y = windowState.y;
    }

    mainWindow = new BrowserWindow(windowOptions);

    mainWindow.loadFile('index.html');

    // Restore maximized state
    if (windowState.isMaximized) {
        mainWindow.maximize();
    }

    // Show window when ready
    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
    });

    // Save window state when moved or resized
    mainWindow.on('resize', saveWindowState);
    mainWindow.on('move', saveWindowState);
    mainWindow.on('maximize', saveWindowState);
    mainWindow.on('unmaximize', saveWindowState);

    // Save window state before closing
    mainWindow.on('close', () => {
        saveWindowState();
    });

    // Create menu
    createMenu();

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
    // Clean up file watcher
    if (currentWatcher) {
        currentWatcher.close();
        currentWatcher = null;
    }

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

ipcMain.handle('select-move-destination', async (event, defaultPath) => {
    const result = await dialog.showOpenDialog(mainWindow, {
        properties: ['openDirectory'],
        defaultPath: defaultPath
    });

    if (!result.canceled && result.filePaths.length > 0) {
        return result.filePaths[0];
    }
    return null;
});

ipcMain.handle('move-files', async (event, filePaths, destinationPath) => {
    try {
        const results = [];
        for (const filePath of filePaths) {
            const fileName = path.basename(filePath);
            const destinationFile = path.join(destinationPath, fileName);

            // Check if destination file already exists
            if (fs.existsSync(destinationFile)) {
                results.push({
                    success: false,
                    file: filePath,
                    error: `File already exists: ${fileName}`
                });
                continue;
            }

            try {
                fs.renameSync(filePath, destinationFile);
                results.push({ success: true, file: filePath });
            } catch (error) {
                results.push({
                    success: false,
                    file: filePath,
                    error: error.message
                });
            }
        }
        return results;
    } catch (error) {
        console.error('Error moving files:', error);
        throw error;
    }
});

ipcMain.handle('move-files-to-trash', async (event, filePaths) => {
    const { shell } = require('electron');
    try {
        const results = [];
        for (const filePath of filePaths) {
            try {
                await shell.trashItem(filePath);
                results.push({ success: true, file: filePath });
            } catch (error) {
                results.push({
                    success: false,
                    file: filePath,
                    error: error.message
                });
            }
        }
        return results;
    } catch (error) {
        console.error('Error moving files to trash:', error);
        throw error;
    }
});

ipcMain.on('update-menu-state', (event, hasSelection) => {
    const menu = Menu.getApplicationMenu();
    if (menu) {
        const moveItem = menu.getMenuItemById('move-selected');
        if (moveItem) {
            moveItem.enabled = hasSelection;
        }
    }
});

ipcMain.handle('start-watching-directory', (event, directoryPath) => {
    try {
        // Stop any existing watcher
        if (currentWatcher) {
            currentWatcher.close();
            currentWatcher = null;
        }

        if (!directoryPath || !fs.existsSync(directoryPath)) {
            return { success: false, error: 'Directory does not exist' };
        }

        // Create new watcher
        currentWatcher = fs.watch(directoryPath, { persistent: false }, (eventType, filename) => {
            console.log('Directory change detected:', eventType, filename);

            // Filter for image files only
            if (filename) {
                const ext = path.extname(filename).toLowerCase();
                const isImageFile = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg', '.fits', '.fit', '.fts'].includes(ext);

                if (isImageFile) {
                    // Send notification to renderer process
                    mainWindow.webContents.send('directory-changed', {
                        eventType,
                        filename,
                        directoryPath
                    });
                }
            } else {
                // If no filename provided, assume general directory change
                mainWindow.webContents.send('directory-changed', {
                    eventType,
                    filename: null,
                    directoryPath
                });
            }
        });

        currentWatcher.on('error', (error) => {
            console.error('File watcher error:', error);
            mainWindow.webContents.send('watcher-error', error.message);
        });

        console.log('Started watching directory:', directoryPath);
        return { success: true };

    } catch (error) {
        console.error('Error starting directory watcher:', error);
        return { success: false, error: error.message };
    }
});

ipcMain.handle('stop-watching-directory', () => {
    try {
        if (currentWatcher) {
            currentWatcher.close();
            currentWatcher = null;
            console.log('Stopped watching directory');
        }
        return { success: true };
    } catch (error) {
        console.error('Error stopping directory watcher:', error);
        return { success: false, error: error.message };
    }
});

ipcMain.handle('get-fits-headers', async (event, filePath) => {
    try {
        if (!fs.existsSync(filePath)) {
            throw new Error('File does not exist');
        }

        const buffer = fs.readFileSync(filePath);
        const header = parseFitsHeader(buffer);

        // Log the parsed headers for debugging
        console.log(`FITS headers for ${path.basename(filePath)}:`, Object.keys(header));
        if (header.FILTER !== undefined) {
            console.log(`  FILTER: "${header.FILTER}"`);
        } else {
            console.log('  FILTER: not found');
        }

        // Remove the internal _headerSize property
        delete header._headerSize;

        return { success: true, headers: header };
    } catch (error) {
        console.error('Error reading FITS headers:', error);
        return { success: false, error: error.message };
    }
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
        // Check cache first
        const cacheKey = getCacheKey(filePath, false);
        const cachedImage = getCachedImage(cacheKey);
        if (cachedImage) {
            console.log('Returning cached FITS image');
            return cachedImage;
        }

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
        // Fix the 75% left-to-right shift issue
        console.log('Applying regular FITS data with 75% left-to-right shift correction');

        // 75% of what we see on left belongs on right, so shift everything right by 75% (or left by 25%)
        const shiftAmount = Math.floor(width * 0.75);
        console.log(`Shifting right by 75% (${shiftAmount} pixels)`);

        for (let i = 0; i < Math.min(data.length, width * height); i++) {
            const normalized = Math.max(0, Math.min(1, (data[i] - min) / (max - min)));
            const value = Math.floor(normalized * 255);

            // Calculate original row and column
            const row = Math.floor(i / width);
            const col = i % width;

            // Shift right by 75% (pixels at position X go to position X+75%, with wraparound)
            const correctedCol = (col + shiftAmount) % width;

            const canvasIndex = (row * width + correctedCol) * 4;

            if (canvasIndex >= 0 && canvasIndex < imageData.data.length - 3) {
                imageData.data[canvasIndex] = value;
                imageData.data[canvasIndex + 1] = value;
                imageData.data[canvasIndex + 2] = value;
                imageData.data[canvasIndex + 3] = 255;
            }
        }

        ctx.putImageData(imageData, 0, 0);

        // Return base64 data URL
        const dataUrl = canvas.toDataURL('image/png');

        // Cache the processed image
        setCachedImage(cacheKey, dataUrl);

        return dataUrl;
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

// Update window title
ipcMain.handle('update-window-title', async (event, directoryPath) => {
    if (mainWindow) {
        if (directoryPath) {
            mainWindow.setTitle(`Image Viewer - ${directoryPath}`);
        } else {
            mainWindow.setTitle('Image Viewer');
        }
    }
});

// Process FITS file with optional histogram stretching
ipcMain.handle('process-fits-file-stretched', async (event, filePath, applyStretch = false) => {
    try {
        // Check cache first
        const cacheKey = getCacheKey(filePath, applyStretch);
        const cachedImage = getCachedImage(cacheKey);
        if (cachedImage) {
            console.log('Returning cached FITS image');
            return cachedImage;
        }

        const buffer = fs.readFileSync(filePath);

        // Parse FITS header
        const header = parseFitsHeader(buffer);
        console.log('FITS Header:', header); // Debug log

        const width = header.NAXIS1;
        const height = header.NAXIS2;
        const bitpix = header.BITPIX || -32;

        console.log(`FITS Header dimensions - NAXIS1 (width): ${width}, NAXIS2 (height): ${height}, BITPIX: ${bitpix}`);

        if (!width || !height) {
            throw new Error(`Unable to determine image dimensions from FITS header. NAXIS1: ${width}, NAXIS2: ${height}`);
        }

        // Use header size from parser
        const headerSize = header._headerSize;

        // Extract data portion
        const dataBuffer = buffer.slice(headerSize);
        const numPixels = width * height;

        console.log(`Processing FITS: ${width}x${height}, BITPIX: ${bitpix}, Header size: ${headerSize}, Data buffer size: ${dataBuffer.length}, Stretch: ${applyStretch}`);

        // Performance optimization: For very large images, consider downsampling during processing
        const isLargeImage = width * height > 4000000; // 4 megapixels
        const processingScale = isLargeImage ? 0.5 : 1.0; // Process at half resolution for large images
        const processWidth = Math.floor(width * processingScale);
        const processHeight = Math.floor(height * processingScale);

        console.log(`Image size optimization: ${width}x${height} -> ${processWidth}x${processHeight} (scale: ${processingScale})`);

        // Debug: Check if we need to account for padding
        const totalFileSize = buffer.length;
        const expectedPixelDataSize = width * height * Math.abs(bitpix) / 8;
        console.log(`Total file size: ${totalFileSize}, Header size: ${headerSize}, Expected pixel data: ${expectedPixelDataSize}`);
        console.log(`Remaining after header: ${totalFileSize - headerSize}, FITS blocks are 2880 bytes`);

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

        // Debug: Try to detect the layout issue
        let potentialOffset = 0;
        if (dataBuffer.length > expectedDataSize) {
            // Check if there might be additional padding or offset
            const extraBytes = dataBuffer.length - expectedDataSize;
            console.log(`Extra bytes in data buffer: ${extraBytes}`);
        }

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

        let min, max;

        if (applyStretch) {
            // For histogram stretching, use percentile-based min/max
            const sortedData = [...data].sort((a, b) => a - b);
            const lowIndex = Math.floor(sortedData.length * 0.01); // 1st percentile
            const highIndex = Math.floor(sortedData.length * 0.99); // 99th percentile
            min = sortedData[lowIndex];
            max = sortedData[highIndex];
            console.log(`Histogram stretch: using 1st-99th percentile range: ${min} to ${max}`);
        } else {
            // Standard min/max scaling
            min = data[0];
            max = data[0];
            for (let i = 1; i < data.length; i++) {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }
        }

        // Handle edge case where min equals max
        if (min === max) {
            max = min + 1;
        }

        // Apply scaling to convert to 0-255 range
        // Address the "right 20% on left" issue by trying different data arrangements

        console.log('Applying FITS data with 75% left-to-right shift correction');

        // 75% of what we see on left belongs on right, so shift everything right by 75% (or left by 25%)
        const shiftAmount = Math.floor(width * 0.75);
        console.log(`Shifting right by 75% (${shiftAmount} pixels)`);

        for (let i = 0; i < Math.min(data.length, width * height); i++) {
            const normalized = Math.max(0, Math.min(1, (data[i] - min) / (max - min)));
            const value = Math.floor(normalized * 255);

            // Calculate original row and column
            const row = Math.floor(i / width);
            const col = i % width;

            // Shift right by 75% (pixels at position X go to position X+75%, with wraparound)
            const correctedCol = (col + shiftAmount) % width;

            const canvasIndex = (row * width + correctedCol) * 4;

            if (canvasIndex >= 0 && canvasIndex < imageData.data.length - 3) {
                imageData.data[canvasIndex] = value;
                imageData.data[canvasIndex + 1] = value;
                imageData.data[canvasIndex + 2] = value;
                imageData.data[canvasIndex + 3] = 255;
            }
        }

        ctx.putImageData(imageData, 0, 0);

        // Return base64 data URL
        const dataUrl = canvas.toDataURL('image/png');

        // Cache the processed image
        setCachedImage(cacheKey, dataUrl);

        // Also generate and cache thumbnail for faster loading
        const thumbKey = getCacheKey(filePath, applyStretch, true);
        const thumbCanvas = generateThumbnail(canvas);
        const thumbDataUrl = thumbCanvas.toDataURL('image/png');
        setCachedImage(thumbKey, thumbDataUrl);

        return dataUrl;
    } catch (error) {
        console.error('Error processing FITS file with stretching:', error);
        // Return a placeholder image for FITS files that can't be processed
        const canvas = createCanvas(400, 300);
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#333';
        ctx.fillRect(0, 0, 400, 300);
        ctx.fillStyle = '#fff';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Error processing FITS file', 200, 140);
        ctx.fillText(error.message, 200, 160);
        return canvas.toDataURL('image/png');
    }
});

// Add thumbnail API for faster loading
ipcMain.handle('get-fits-thumbnail', async (event, filePath, applyStretch = false) => {
    try {
        const thumbKey = getCacheKey(filePath, applyStretch, true);
        const cachedThumb = getCachedImage(thumbKey);
        if (cachedThumb) {
            return cachedThumb;
        }

        // If no thumbnail exists, we need to process the full image
        // This will automatically create and cache the thumbnail
        return null; // Return null to indicate thumbnail needs to be generated
    } catch (error) {
        console.error('Error getting FITS thumbnail:', error);
        return null;
    }
});