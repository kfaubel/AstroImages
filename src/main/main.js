/**
 * @fileoverview Refactored main process for AstroImages
 * 
 * This is the new main entry point that uses the modular architecture
 * with proper separation of concerns and dependency injection.
 */

const { app, BrowserWindow, ipcMain, dialog, Menu } = require('electron');
const path = require('path');

// Import our services and utilities
const configManager = require('../services/ConfigManager');
const logger = require('../services/Logger');
const FitsProcessor = require('../services/FitsProcessor');
const CacheManager = require('../services/CacheManager');
const { FileUtils } = require('../utils');
const { errorHandler, FileError, FitsError } = require('../utils/ErrorHandler');

/**
 * Main application class that orchestrates all services
 */
class AstroImagesApp {
    constructor() {
        this.mainWindow = null;
        this.config = null;
        this.fitsProcessor = null;
        this.cacheManager = null;
        this.fileWatcher = null;
        this.isInitialized = false;
    }

    /**
     * Initialize the application and all services
     */
    async initialize() {
        try {
            logger.info('Initializing AstroImages application');

            // Initialize configuration
            this.config = await configManager.initialize();
            
            // Initialize logger with config
            await logger.initialize(this.config.logging);
            
            // Initialize services with configuration
            this.fitsProcessor = new FitsProcessor(this.config.fits);
            this.cacheManager = new CacheManager(this.config.cache);
            await this.cacheManager.initialize();

            // Set up IPC handlers
            this.setupIpcHandlers();

            this.isInitialized = true;
            logger.info('Application initialization completed');

        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'app_initialization' });
            console.error('Failed to initialize application:', handled.error);
            
            // Try to continue with default configuration
            this.initializeWithDefaults();
        }
    }

    /**
     * Fallback initialization with default configuration
     */
    initializeWithDefaults() {
        logger.warn('Falling back to default configuration');
        
        try {
            this.fitsProcessor = new FitsProcessor();
            this.cacheManager = new CacheManager();
            this.setupIpcHandlers();
            this.isInitialized = true;
        } catch (error) {
            logger.error('Failed to initialize with defaults', error);
            throw error;
        }
    }

    /**
     * Create the main application window
     */
    async createMainWindow() {
        try {
            const windowConfig = this.loadWindowState();

            this.mainWindow = new BrowserWindow({
                width: windowConfig.width,
                height: windowConfig.height,
                x: windowConfig.x,
                y: windowConfig.y,
                title: 'AstroImages',
                webPreferences: {
                    nodeIntegration: false,
                    contextIsolation: true,
                    webSecurity: false,
                    preload: path.join(__dirname, 'preload.js')
                },
                show: false
            });

            // Load the HTML file
            await this.mainWindow.loadFile(path.join(__dirname, '../../index.html'));

            // Set up window event handlers
            this.setupWindowHandlers();

            // Create application menu
            this.createMenu();

            // Restore maximized state
            if (windowConfig.isMaximized) {
                this.mainWindow.maximize();
            }

            // Show window when ready
            this.mainWindow.once('ready-to-show', () => {
                this.mainWindow.show();
            });

            logger.info('Main window created');

        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'create_window' });
            throw handled.error;
        }
    }

    /**
     * Set up window event handlers
     */
    setupWindowHandlers() {
        this.mainWindow.on('resize', () => this.saveWindowState());
        this.mainWindow.on('move', () => this.saveWindowState());
        this.mainWindow.on('maximize', () => this.saveWindowState());
        this.mainWindow.on('unmaximize', () => this.saveWindowState());
        this.mainWindow.on('close', () => this.saveWindowState());

        this.mainWindow.on('closed', () => {
            this.mainWindow = null;
        });
    }

    /**
     * Set up IPC message handlers
     */
    setupIpcHandlers() {
        // File system operations
        ipcMain.handle('select-directory', this.handleSelectDirectory.bind(this));
        ipcMain.handle('read-directory', this.handleReadDirectory.bind(this));
        ipcMain.handle('check-directory-exists', this.handleCheckDirectoryExists.bind(this));

        // FITS processing
        ipcMain.handle('process-fits-file', this.handleProcessFitsFile.bind(this));
        ipcMain.handle('process-fits-file-stretched', this.handleProcessFitsFileStretched.bind(this));
        ipcMain.handle('get-fits-headers', this.handleGetFitsHeaders.bind(this));
        ipcMain.handle('get-fits-thumbnail', this.handleGetFitsThumbnail.bind(this));

        // File management
        ipcMain.handle('select-move-destination', this.handleSelectMoveDestination.bind(this));
        ipcMain.handle('move-files', this.handleMoveFiles.bind(this));
        ipcMain.handle('move-files-to-trash', this.handleMoveFilesToTrash.bind(this));

        // File watching
        ipcMain.handle('start-watching-directory', this.handleStartWatchingDirectory.bind(this));
        ipcMain.handle('stop-watching-directory', this.handleStopWatchingDirectory.bind(this));

        // UI operations
        ipcMain.handle('update-window-title', this.handleUpdateWindowTitle.bind(this));
        ipcMain.on('update-menu-state', this.handleUpdateMenuState.bind(this));

        logger.debug('IPC handlers registered');
    }

    /**
     * IPC Handler: Select directory dialog
     */
    async handleSelectDirectory() {
        try {
            const result = await dialog.showOpenDialog(this.mainWindow, {
                properties: ['openDirectory']
            });

            return result.canceled ? null : result.filePaths[0];
        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'select_directory' });
            throw handled.error;
        }
    }

    /**
     * IPC Handler: Read directory contents
     */
    async handleReadDirectory(event, directoryPath) {
        try {
            logger.debug('Reading directory', { path: directoryPath });
            return FileUtils.getImageFiles(directoryPath);
        } catch (error) {
            const handled = errorHandler.handle(new FileError('Failed to read directory', directoryPath));
            logger.warn('Directory read failed', { path: directoryPath, error: handled.error });
            return [];
        }
    }

    /**
     * IPC Handler: Check if directory exists
     */
    async handleCheckDirectoryExists(event, directoryPath) {
        return FileUtils.isDirectory(directoryPath);
    }

    /**
     * IPC Handler: Process FITS file
     */
    async handleProcessFitsFile(event, filePath) {
        try {
            // Check cache first
            const cacheKey = this.cacheManager.generateKey(filePath, false, false);
            const cachedImage = await this.cacheManager.get(cacheKey);
            
            if (cachedImage) {
                logger.debug('Returning cached FITS image', { filePath });
                return cachedImage;
            }

            // Process the file
            const result = await this.fitsProcessor.processFitsFile(filePath, {
                applyStretch: false
            });

            // Cache the result
            await this.cacheManager.set(cacheKey, result);

            return result;

        } catch (error) {
            const handled = errorHandler.handle(new FitsError('FITS processing failed', filePath), {
                filePath,
                operation: 'process_fits'
            });
            
            // Return placeholder image
            return this.fitsProcessor.createPlaceholderImage(handled.userMessage);
        }
    }

    /**
     * IPC Handler: Process FITS file with stretching
     */
    async handleProcessFitsFileStretched(event, filePath, applyStretch = false) {
        try {
            // Check cache first
            const cacheKey = this.cacheManager.generateKey(filePath, applyStretch, false);
            const cachedImage = await this.cacheManager.get(cacheKey);
            
            if (cachedImage) {
                logger.debug('Returning cached stretched FITS image', { filePath, applyStretch });
                return cachedImage;
            }

            // Process the file
            const result = await this.fitsProcessor.processFitsFile(filePath, {
                applyStretch,
                stretchParams: this.config?.fits?.defaultStretch
            });

            // Cache the result
            await this.cacheManager.set(cacheKey, result);

            // Generate and cache thumbnail
            const thumbKey = this.cacheManager.generateKey(filePath, applyStretch, true);
            const thumbnail = await this.cacheManager.generateThumbnail(result);
            await this.cacheManager.set(thumbKey, thumbnail);

            return result;

        } catch (error) {
            const handled = errorHandler.handle(new FitsError('FITS processing with stretch failed', filePath), {
                filePath,
                applyStretch,
                operation: 'process_fits_stretched'
            });
            
            return this.fitsProcessor.createPlaceholderImage(handled.userMessage);
        }
    }

    /**
     * IPC Handler: Get FITS headers
     */
    async handleGetFitsHeaders(event, filePath) {
        try {
            if (!FileUtils.exists(filePath)) {
                throw new FileError('File does not exist', filePath);
            }

            //logger.info('Reading FITS headers', { filePath });

            const fs = require('fs');
            const buffer = fs.readFileSync(filePath);
            const header = this.fitsProcessor.parseHeader(buffer);

            // Remove internal properties
            delete header._headerSize;

            const keywordCount = Object.keys(header).length;
            logger.info(`FITS headers: ${require('path').basename(filePath)}, ${keywordCount} found.`);

            // Log a summary of key headers for user visibility
            // const importantHeaders = ['OBJECT', 'EXPTIME', 'EXPOSURE', 'FILTER', 'FILTNAM', 'DATE-OBS', 'CAMERA', 'TELESCOP'];
            // const foundImportant = importantHeaders.filter(key => header[key] !== undefined);
            // if (foundImportant.length > 0) {
            //     const importantHeadersList = foundImportant.map(key => `  ${key}: ${header[key]}`).join('\n');
            //     logger.info(`Key FITS headers for ${require('path').basename(filePath)}:\n${importantHeadersList}`);
            // }

            return { success: true, headers: header };

        } catch (error) {
            const handled = errorHandler.handle(new FitsError('Failed to read FITS headers', filePath), {
                filePath,
                operation: 'get_fits_headers'
            });

            logger.error('FITS header extraction failed', {
                filePath,
                fileName: require('path').basename(filePath),
                error: handled.error.message
            });

            return { success: false, error: handled.userMessage };
        }
    }

    /**
     * IPC Handler: Get FITS thumbnail
     */
    async handleGetFitsThumbnail(event, filePath, applyStretch = false) {
        try {
            const thumbKey = this.cacheManager.generateKey(filePath, applyStretch, true);
            return await this.cacheManager.get(thumbKey);
        } catch (error) {
            logger.warn('Failed to get thumbnail', { filePath, error: error.message });
            return null;
        }
    }

    /**
     * IPC Handler: Select move destination
     */
    async handleSelectMoveDestination(event, defaultPath) {
        try {
            const result = await dialog.showOpenDialog(this.mainWindow, {
                properties: ['openDirectory'],
                defaultPath: defaultPath
            });

            return result.canceled ? null : result.filePaths[0];
        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'select_move_destination' });
            throw handled.error;
        }
    }

    /**
     * IPC Handler: Move files
     */
    async handleMoveFiles(event, filePaths, destinationPath) {
        try {
            const fs = require('fs');
            const results = [];

            for (const filePath of filePaths) {
                const fileName = path.basename(filePath);
                const destinationFile = path.join(destinationPath, fileName);

                try {
                    if (fs.existsSync(destinationFile)) {
                        results.push({
                            success: false,
                            file: filePath,
                            error: `File already exists: ${fileName}`
                        });
                        continue;
                    }

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

            logger.info('Files moved', { 
                count: filePaths.length, 
                successful: results.filter(r => r.success).length 
            });

            return results;

        } catch (error) {
            const handled = errorHandler.handle(new FileError('Failed to move files'), { 
                filePaths, 
                destinationPath 
            });
            throw handled.error;
        }
    }

    /**
     * IPC Handler: Move files to trash
     */
    async handleMoveFilesToTrash(event, filePaths) {
        try {
            const { shell } = require('electron');
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

            logger.info('Files moved to trash', { 
                count: filePaths.length, 
                successful: results.filter(r => r.success).length 
            });

            return results;

        } catch (error) {
            const handled = errorHandler.handle(new FileError('Failed to move files to trash'), { filePaths });
            throw handled.error;
        }
    }

    /**
     * IPC Handler: Start watching directory
     */
    async handleStartWatchingDirectory(event, directoryPath) {
        try {
            const fs = require('fs');

            // Stop existing watcher
            if (this.fileWatcher) {
                this.fileWatcher.close();
                this.fileWatcher = null;
            }

            if (!FileUtils.isDirectory(directoryPath)) {
                throw new FileError('Directory does not exist', directoryPath);
            }

            // Create new watcher
            this.fileWatcher = fs.watch(directoryPath, { persistent: false }, (eventType, filename) => {
                this.handleFileSystemChange(eventType, filename, directoryPath);
            });

            this.fileWatcher.on('error', (error) => {
                const handled = errorHandler.handle(error, { context: 'file_watcher' });
                this.mainWindow?.webContents.send('watcher-error', handled.userMessage);
            });

            logger.info('Started watching directory', { path: directoryPath });
            return { success: true };

        } catch (error) {
            const handled = errorHandler.handle(new FileError('Failed to start directory watching', directoryPath));
            return { success: false, error: handled.userMessage };
        }
    }

    /**
     * IPC Handler: Stop watching directory
     */
    async handleStopWatchingDirectory() {
        try {
            if (this.fileWatcher) {
                this.fileWatcher.close();
                this.fileWatcher = null;
                logger.info('Stopped watching directory');
            }
            return { success: true };
        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'stop_file_watcher' });
            return { success: false, error: handled.userMessage };
        }
    }

    /**
     * IPC Handler: Update window title
     */
    async handleUpdateWindowTitle(event, directoryPath) {
        if (this.mainWindow) {
            const title = directoryPath ? `AstroImages - ${directoryPath}` : 'AstroImages';
            this.mainWindow.setTitle(title);
        }
    }

    /**
     * IPC Handler: Update menu state
     */
    handleUpdateMenuState(event, hasSelection) {
        const menu = Menu.getApplicationMenu();
        if (menu) {
            const moveItem = menu.getMenuItemById('move-selected');
            if (moveItem) {
                moveItem.enabled = hasSelection;
            }
        }
    }

    /**
     * Handle file system changes
     */
    handleFileSystemChange(eventType, filename, directoryPath) {
        try {
            logger.debug('Directory change detected', { eventType, filename, directoryPath });

            // Filter for image files only
            if (filename && FileUtils.isImageFile(filename)) {
                this.mainWindow?.webContents.send('directory-changed', {
                    eventType,
                    filename,
                    directoryPath
                });
            }
        } catch (error) {
            logger.warn('Error handling file system change', error);
        }
    }

    /**
     * Load window state from storage
     */
    loadWindowState() {
        try {
            const fs = require('fs');
            const userDataPath = app.getPath('userData');
            const statePath = path.join(userDataPath, 'window-state.json');

            if (fs.existsSync(statePath)) {
                const windowState = JSON.parse(fs.readFileSync(statePath, 'utf8'));

                // Validate window state
                if (this.isValidWindowState(windowState)) {
                    return windowState;
                }
            }
        } catch (error) {
            logger.warn('Failed to load window state', error);
        }

        // Return default state
        return {
            width: this.config?.ui?.defaultWindowWidth || 1200,
            height: this.config?.ui?.defaultWindowHeight || 800,
            x: undefined,
            y: undefined,
            isMaximized: false
        };
    }

    /**
     * Validate window state data
     */
    isValidWindowState(state) {
        return state && 
               typeof state.width === 'number' && state.width > 100 &&
               typeof state.height === 'number' && state.height > 100;
    }

    /**
     * Save current window state
     */
    saveWindowState() {
        if (!this.mainWindow) return;

        try {
            const bounds = this.mainWindow.getBounds();
            const isMaximized = this.mainWindow.isMaximized();

            const windowState = {
                x: bounds.x,
                y: bounds.y,
                width: bounds.width,
                height: bounds.height,
                isMaximized: isMaximized
            };

            const fs = require('fs');
            const userDataPath = app.getPath('userData');
            const statePath = path.join(userDataPath, 'window-state.json');

            fs.writeFileSync(statePath, JSON.stringify(windowState, null, 2));

        } catch (error) {
            logger.warn('Failed to save window state', error);
        }
    }

    /**
     * Create application menu
     */
    createMenu() {
        const template = [
            {
                label: 'File',
                submenu: [
                    {
                        label: 'Open Folder...',
                        accelerator: 'CmdOrCtrl+O',
                        click: () => this.handleMenuOpenFolder()
                    },
                    { type: 'separator' },
                    {
                        label: 'Exit',
                        accelerator: process.platform === 'darwin' ? 'Cmd+Q' : 'Ctrl+Q',
                        click: () => app.quit()
                    }
                ]
            },
            {
                label: 'Action',
                submenu: [
                    {
                        label: 'Move Selected...',
                        accelerator: 'CmdOrCtrl+M',
                        enabled: false,
                        id: 'move-selected',
                        click: () => this.mainWindow?.webContents.send('show-move-dialog')
                    }
                ]
            },
            {
                label: 'Options',
                submenu: [
                    {
                        label: 'Custom Keywords...',
                        click: () => this.mainWindow?.webContents.send('show-keywords-dialog')
                    },
                    {
                        label: 'FITS Headers...',
                        click: () => this.mainWindow?.webContents.send('show-fits-headers-dialog')
                    }
                ]
            },
            {
                label: 'View',
                submenu: [
                    {
                        label: 'Zoom In',
                        accelerator: 'CmdOrCtrl+Plus',
                        click: () => this.mainWindow?.webContents.send('zoom-in')
                    },
                    {
                        label: 'Zoom Out',
                        accelerator: 'CmdOrCtrl+-',
                        click: () => this.mainWindow?.webContents.send('zoom-out')
                    },
                    {
                        label: 'Actual Size',
                        accelerator: 'CmdOrCtrl+0',
                        click: () => this.mainWindow?.webContents.send('zoom-actual')
                    },
                    {
                        label: 'Fit to Window',
                        accelerator: 'CmdOrCtrl+F',
                        click: () => this.mainWindow?.webContents.send('zoom-fit')
                    },
                    { type: 'separator' },
                    {
                        label: 'Toggle Developer Tools',
                        accelerator: 'F12',
                        click: () => this.mainWindow?.webContents.toggleDevTools()
                    }
                ]
            }
        ];

        const menu = Menu.buildFromTemplate(template);
        Menu.setApplicationMenu(menu);

        logger.debug('Application menu created');
    }

    /**
     * Handle menu: Open Folder
     */
    async handleMenuOpenFolder() {
        try {
            const result = await dialog.showOpenDialog(this.mainWindow, {
                properties: ['openDirectory']
            });

            if (!result.canceled && result.filePaths.length > 0) {
                this.mainWindow?.webContents.send('folder-selected', result.filePaths[0]);
            }
        } catch (error) {
            const handled = errorHandler.handle(error, { context: 'menu_open_folder' });
            logger.warn('Menu open folder failed', handled.error);
        }
    }

    /**
     * Clean up resources before shutdown
     */
    async cleanup() {
        try {
            logger.info('Cleaning up application resources');

            // Stop file watcher
            if (this.fileWatcher) {
                this.fileWatcher.close();
                this.fileWatcher = null;
            }

            // Clean up cache
            if (this.cacheManager) {
                await this.cacheManager.cleanup();
            }

            // Close logger
            logger.close();

        } catch (error) {
            console.error('Error during cleanup:', error);
        }
    }
}

// Create application instance
const astroApp = new AstroImagesApp();

// Disable hardware acceleration to prevent GPU process errors
app.disableHardwareAcceleration();

// Application event handlers
app.whenReady().then(async () => {
    try {
        await astroApp.initialize();
        await astroApp.createMainWindow();
        logger.info('AstroImages application started successfully');
    } catch (error) {
        console.error('Failed to start application:', error);
        app.quit();
    }
});

app.on('window-all-closed', async () => {
    await astroApp.cleanup();
    
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        await astroApp.createMainWindow();
    }
});

// Handle unhandled errors
process.on('unhandledRejection', (reason, promise) => {
    const handled = errorHandler.handle(reason, { 
        context: 'unhandled_rejection',
        promise: promise.toString()
    });
    logger.error('Unhandled promise rejection', handled.error);
});

process.on('uncaughtException', (error) => {
    const handled = errorHandler.handle(error, { context: 'uncaught_exception' });
    logger.error('Uncaught exception', handled.error);
    
    // Exit gracefully
    astroApp.cleanup().finally(() => {
        process.exit(1);
    });
});

module.exports = astroApp;