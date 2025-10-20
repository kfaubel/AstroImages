/**
 * AstroImages - Main Renderer Entry Point
 * 
 * This is the main entry point for the renderer process of the AstroImages application.
 * It initializes all managers and provides a clean, modular architecture for
 * astronomical image viewing and management.
 * 
 * Architecture Overview:
 * - AppState: Centralized state management with reactive updates
 * - DOMElements: Safe DOM element access and event handling
 * - FileListManager: File list display, sorting, and selection
 * - ImageDisplayManager: Image loading, display, and zoom controls
 * - PlaybackManager: Slideshow and navigation controls
 * - DialogManager: Modal dialogs for configuration
 * 
 * @author AstroImages Development Team
 * @version 2.0.0
 */

console.log('AstroImages modular application starting...');

// Core modules
import appState from './core/AppState.js';
import domElements from './core/DOMElements.js';

// Manager modules
import fileListManager from './managers/FileListManager.js';
import imageDisplayManager from './managers/ImageDisplayManager.js';

/**
 * Main Application Class
 * 
 * Coordinates all application managers and handles high-level
 * application lifecycle events.
 */
class AstroImagesApp {
    constructor() {
        this.initialized = false;
        this.managers = new Map();
    }
    
    /**
     * Initialize the application
     */
    async init() {
        if (this.initialized) {
            console.warn('Application already initialized');
            return;
        }
        
        try {
            console.log('Initializing AstroImages application...');
            
            // Wait for DOM to be ready
            await this.waitForDOM();
            
            // Initialize core systems
            await this.initializeCore();
            
            // Initialize managers
            await this.initializeManagers();
            
            // Setup global event handlers
            this.setupGlobalEventHandlers();
            
            // Setup IPC handlers
            this.setupIPCHandlers();
            
            // Load saved state
            await this.loadSavedState();
            
            // Make directory selection function globally available for menu
            window.selectFolder = () => this.selectFolder();
            
            this.initialized = true;
            console.log('AstroImages application initialized successfully');
            
        } catch (error) {
            console.error('Failed to initialize application:', error);
            this.handleInitializationError(error);
        }
    }
    
    /**
     * Wait for DOM to be ready
     */
    waitForDOM() {
        return new Promise((resolve) => {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', resolve);
            } else {
                resolve();
            }
        });
    }
    
    /**
     * Initialize core systems
     */
    async initializeCore() {
        console.log('Initializing core systems...');
        
        // Initialize DOM elements first
        domElements.init();
        
        // Verify critical elements exist
        const criticalElements = ['fileListDiv', 'imageContainer', 'mainContent'];
        for (const elementName of criticalElements) {
            if (!domElements.has(elementName)) {
                throw new Error(`Critical DOM element missing: ${elementName}`);
            }
        }
        
        // Initialize UI state - hide playback controls initially
        const playbackControls = domElements.get('playbackControls');
        if (playbackControls) {
            playbackControls.style.display = 'none';
        }
        
        console.log('Core systems initialized');
    }
    
    /**
     * Initialize all managers
     */
    async initializeManagers() {
        console.log('Initializing managers...');
        
        // Initialize managers in order of dependency
        const managersToInit = [
            ['fileList', fileListManager],
            ['imageDisplay', imageDisplayManager]
        ];
        
        for (const [name, manager] of managersToInit) {
            try {
                if (typeof manager.init === 'function') {
                    await manager.init();
                    this.managers.set(name, manager);
                    console.log(`${name} manager initialized`);
                } else {
                    console.warn(`Manager ${name} does not have init method`);
                }
            } catch (error) {
                console.error(`Failed to initialize ${name} manager:`, error);
                throw error;
            }
        }
        
        console.log('All managers initialized');
    }
    
    /**
     * Setup global event handlers
     */
    setupGlobalEventHandlers() {
        console.log('Setting up global event handlers...');
        
        // Setup resize handle functionality
        this.setupResizeHandle();
        
        // Setup dialog event listeners
        this.setupDialogEventListeners();
        
        // Custom application events
        window.addEventListener('displayImage', this.handleDisplayImage.bind(this));
        window.addEventListener('playbackNext', this.handlePlaybackNext.bind(this));
        window.addEventListener('playbackPrevious', this.handlePlaybackPrevious.bind(this));
        
        // Keyboard shortcuts
        document.addEventListener('keydown', this.handleKeyboardShortcuts.bind(this));
        
        // Window events
        window.addEventListener('beforeunload', this.handleBeforeUnload.bind(this));
        
        // Error handling
        window.addEventListener('error', this.handleGlobalError.bind(this));
        window.addEventListener('unhandledrejection', this.handleUnhandledRejection.bind(this));
        
        console.log('Global event handlers set up');
    }
    
    /**
     * Setup resize handle functionality for sidebar
     */
    setupResizeHandle() {
        console.log('Setting up resize handle...');
        
        const sidebar = document.getElementById('sidebar');
        const resizeHandle = document.getElementById('resize-handle');
        
        if (!sidebar || !resizeHandle) {
            console.warn('Sidebar or resize handle not found');
            return;
        }

        let isResizing = false;

        // Clear any existing sidebar width preference and set to 50% of window width
        localStorage.removeItem('sidebarWidth'); // Clear previous setting
        const initialWidth = window.innerWidth * 0.5;
        sidebar.style.width = initialWidth + 'px';
        console.log(`Setting initial sidebar width to 50%: ${initialWidth}px`);

        // Resize handle mouse down
        resizeHandle.addEventListener('mousedown', (e) => {
            isResizing = true;
            resizeHandle.classList.add('dragging');
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        });

        // Mouse move for resizing
        document.addEventListener('mousemove', (e) => {
            if (!isResizing) return;

            const containerRect = document.querySelector('.container').getBoundingClientRect();
            const newWidth = e.clientX - containerRect.left;
            const minWidth = 250;
            const maxWidth = window.innerWidth * 0.8; // Keep the 80% max limit

            if (newWidth >= minWidth && newWidth <= maxWidth) {
                sidebar.style.width = newWidth + 'px';
            }
        });

        // Mouse up to end resizing
        document.addEventListener('mouseup', () => {
            if (isResizing) {
                isResizing = false;
                resizeHandle.classList.remove('dragging');
                document.body.style.cursor = '';
                document.body.style.userSelect = '';

                // Save the sidebar width to localStorage
                localStorage.setItem('sidebarWidth', sidebar.style.width);

                // Trigger image resize if needed
                const imageDisplay = document.getElementById('image-display');
                if (imageDisplay && imageDisplay.src && imageDisplay.style.display !== 'none') {
                    // Check if we're in fit-to-window mode
                    const zoomFitBtn = document.getElementById('zoom-fit');
                    if (zoomFitBtn && zoomFitBtn.classList.contains('active')) {
                        setTimeout(() => {
                            imageDisplayManager.zoomFit();
                        }, 50); // Small delay to allow DOM to update
                    }
                }
            }
        });

        console.log('Resize handle set up');
    }
    
    /**
     * Setup dialog event listeners
     */
    setupDialogEventListeners() {
        console.log('Setting up dialog event listeners...');
        
        // Keywords dialog
        const addKeywordBtn = document.getElementById('add-keyword');
        const keywordInput = document.getElementById('keyword-input');
        const saveKeywordsBtn = document.getElementById('save-keywords');
        const cancelKeywordsBtn = document.getElementById('cancel-keywords');
        const testFilenameInput = document.getElementById('test-filename');
        
        if (addKeywordBtn && keywordInput) {
            addKeywordBtn.addEventListener('click', () => {
                const keyword = keywordInput.value.trim();
                if (keyword) {
                    this.addKeyword(keyword);
                    keywordInput.value = '';
                }
            });
            
            keywordInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    const keyword = keywordInput.value.trim();
                    if (keyword) {
                        this.addKeyword(keyword);
                        keywordInput.value = '';
                    }
                }
            });
        }
        
        if (saveKeywordsBtn) {
            saveKeywordsBtn.addEventListener('click', () => {
                this.saveKeywordsDialog();
            });
        }
        
        if (cancelKeywordsBtn) {
            cancelKeywordsBtn.addEventListener('click', () => {
                this.cancelKeywordsDialog();
            });
        }
        
        if (testFilenameInput) {
            testFilenameInput.addEventListener('input', () => {
                this.updateTestResults();
            });
        }
        
        // FITS Headers dialog
        const addHeaderBtn = document.getElementById('add-custom-header');
        const headerInput = document.getElementById('custom-header-input');
        const saveFitsHeadersBtn = document.getElementById('save-fits-headers');
        const cancelFitsHeadersBtn = document.getElementById('cancel-fits-headers');
        
        if (addHeaderBtn && headerInput) {
            addHeaderBtn.addEventListener('click', () => {
                const header = headerInput.value.trim().toUpperCase();
                if (header) {
                    this.addFitsHeader(header);
                    headerInput.value = '';
                }
            });
            
            headerInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    const header = headerInput.value.trim().toUpperCase();
                    if (header) {
                        this.addFitsHeader(header);
                        headerInput.value = '';
                    }
                }
            });
        }
        
        if (saveFitsHeadersBtn) {
            saveFitsHeadersBtn.addEventListener('click', () => {
                this.saveFitsHeadersDialog();
            });
        }
        
        if (cancelFitsHeadersBtn) {
            cancelFitsHeadersBtn.addEventListener('click', () => {
                this.cancelFitsHeadersDialog();
            });
        }
        
        // Common FITS headers checkboxes
        document.addEventListener('change', (e) => {
            if (e.target.classList.contains('common-header-checkbox')) {
                const header = e.target.value;
                if (e.target.checked) {
                    this.addFitsHeader(header);
                } else {
                    this.removeFitsHeaderFromSelected(header);
                }
            }
        });
        
        console.log('Dialog event listeners set up');
    }
    
    /**
     * Setup IPC handlers for communication with main process
     */
    setupIPCHandlers() {
        console.log('Setting up IPC handlers...');
        
        if (window.electronAPI) {
            // Menu-triggered folder selection
            window.electronAPI.onFolderSelected?.((folderPath) => {
                console.log('Folder selected via menu:', folderPath);
                this.handleFolderSelection(folderPath);
            });
            
            // Menu-triggered dialog events
            window.electronAPI.onShowKeywordsDialog?.(() => {
                console.log('Opening keywords dialog');
                this.openKeywordDialog();
            });
            
            window.electronAPI.onShowFitsHeadersDialog?.(() => {
                console.log('Opening FITS headers dialog');
                this.openFitsHeadersDialog();
            });
            
            window.electronAPI.onShowMoveDialog?.(() => {
                console.log('Opening move dialog');
                this.openMoveDialog();
            });
            
            // Directory watcher events
            window.electronAPI.onDirectoryChanged?.((files) => {
                this.handleDirectoryChanged(files);
            });
            
            window.electronAPI.onWatcherError?.((error) => {
                this.handleWatcherError(error);
            });
            
            console.log('IPC handlers set up');
        } else {
            console.warn('ElectronAPI not available - running in browser mode');
        }
    }
    
    /**
     * Load saved application state
     */
    async loadSavedState() {
        console.log('Loading saved state...');
        
        try {
            // Load keywords from storage
            const savedKeywords = localStorage.getItem('astro-keywords');
            if (savedKeywords) {
                const keywords = JSON.parse(savedKeywords);
                appState.set('keywords', keywords);
                console.log('Loaded keywords:', keywords);
            }
            
            // Load FITS headers from storage
            const savedFitsHeaders = localStorage.getItem('astro-fits-headers');
            if (savedFitsHeaders) {
                const fitsHeaders = JSON.parse(savedFitsHeaders);
                appState.set('fitsHeaders', fitsHeaders);
                console.log('Loaded FITS headers:', fitsHeaders);
            }
            
            // Load last directory
            const lastDirectory = localStorage.getItem('astro-last-directory');
            if (lastDirectory && window.electronAPI) {
                console.log('Loading last directory:', lastDirectory);
                await this.handleFolderSelection(lastDirectory);
            }
            
            console.log('Saved state loaded');
            
        } catch (error) {
            console.error('Error loading saved state:', error);
            // Continue without saved state
        }
    }
    
    /**
     * Handle folder selection from menu or UI
     */
    async selectFolder() {
        try {
            console.log('Selecting folder...');
            const folderPath = await window.electronAPI.selectDirectory();
            if (folderPath) {
                console.log('Folder selected:', folderPath);
                await this.handleFolderSelection(folderPath);
            }
        } catch (error) {
            console.error('Error selecting folder:', error);
            // Hide loading if it was shown
            const loadingOverlay = document.getElementById('loading-overlay');
            if (loadingOverlay) {
                loadingOverlay.style.display = 'none';
            }
        }
    }
    
    /**
     * Handle folder selection and load files
     */
    async handleFolderSelection(folderPath) {
        try {
            console.log('Handling folder selection:', folderPath);
            
            const loadingOverlay = document.getElementById('loading-overlay');
            const loadingText = document.getElementById('loading-text');
            
            if (loadingOverlay) {
                loadingOverlay.style.display = 'flex';
            }
            if (loadingText) {
                loadingText.textContent = 'Loading directory...';
            }
            
            // Update app state
            appState.set('currentDirectory', folderPath);
            
            // Load directory files through fileListManager
            await fileListManager.loadDirectory(folderPath);
            
            // Save last directory
            localStorage.setItem('astro-last-directory', folderPath);
            
            // Hide loading
            if (loadingOverlay) {
                loadingOverlay.style.display = 'none';
            }
            
            console.log('Folder selection handled successfully');
            
        } catch (error) {
            console.error('Error handling folder selection:', error);
            
            // Hide loading on error
            const loadingOverlay = document.getElementById('loading-overlay');
            if (loadingOverlay) {
                loadingOverlay.style.display = 'none';
            }
        }
    }
    
    /**
     * Open keyword configuration dialog
     */
    openKeywordDialog() {
        const keywordDialog = document.getElementById('keyword-dialog');
        if (keywordDialog) {
            keywordDialog.style.display = 'flex';
            this.updateKeywordsDisplay();
            this.updateTestResults();
        }
    }
    
    /**
     * Open FITS headers configuration dialog
     */
    openFitsHeadersDialog() {
        const fitsHeadersDialog = document.getElementById('fits-headers-dialog');
        if (fitsHeadersDialog) {
            this.populateCommonFitsHeaders();
            this.updateSelectedFitsHeadersDisplay();
            fitsHeadersDialog.style.display = 'flex';
        }
    }
    
    /**
     * Open move files dialog
     */
    openMoveDialog() {
        const moveDialog = document.getElementById('move-dialog');
        if (moveDialog) {
            // Update move file count
            const selectedFiles = appState.get('selectedFiles');
            const moveFileCount = document.getElementById('move-file-count');
            if (moveFileCount) {
                moveFileCount.textContent = `${selectedFiles.size} files selected for moving`;
            }
            moveDialog.style.display = 'flex';
        }
    }
    
    /**
     * Update keywords display in dialog
     */
    updateKeywordsDisplay() {
        const keywordsContainer = document.getElementById('keywords-container');
        const keywords = appState.get('keywords') || [];
        
        if (!keywordsContainer) return;
        
        if (keywords.length === 0) {
            keywordsContainer.innerHTML = '<div class="no-keywords">No keywords defined</div>';
            return;
        }
        
        keywordsContainer.innerHTML = keywords.map(keyword => `
            <div class="keyword-item">
                <span class="keyword-name">${keyword}</span>
                <button class="remove-keyword" onclick="removeKeyword('${keyword}')">×</button>
            </div>
        `).join('');
    }
    
    /**
     * Update test results in keyword dialog
     */
    updateTestResults() {
        const testFilename = document.getElementById('test-filename');
        const testResults = document.getElementById('test-results');
        const keywords = appState.get('keywords') || [];
        
        if (!testFilename || !testResults) return;
        
        const filename = testFilename.value.trim();
        if (!filename) {
            testResults.innerHTML = '';
            return;
        }
        
        const parsed = this.parseFilenameForKeywords(filename, keywords);
        testResults.innerHTML = `
            <h5>Parsed Values:</h5>
            ${Object.entries(parsed).map(([key, value]) => 
                `<div class="test-result-item">${key}: <strong>${value}</strong></div>`
            ).join('')}
        `;
    }
    
    /**
     * Parse filename for keywords (simplified version)
     */
    parseFilenameForKeywords(filename, keywords) {
        const parsed = {};
        const parts = filename.split('_');
        
        keywords.forEach(keyword => {
            const index = parts.findIndex(part => part === keyword);
            if (index !== -1 && index + 1 < parts.length) {
                parsed[keyword] = parts[index + 1];
            }
        });
        
        return parsed;
    }
    
    /**
     * Populate common FITS headers in dialog
     */
    populateCommonFitsHeaders() {
        const commonFitsHeaders = document.getElementById('common-fits-headers');
        const fitsHeaders = appState.get('fitsHeaders') || [];
        
        if (!commonFitsHeaders) return;
        
        const COMMON_FITS_HEADERS = [
            { keyword: 'EXPOSURE', description: 'Exposure time' },
            { keyword: 'FILTER', description: 'Filter used' },
            { keyword: 'CCD-TEMP', description: 'CCD temperature' },
            { keyword: 'GAIN', description: 'Camera gain' },
            { keyword: 'OFFSET', description: 'Camera offset' },
            { keyword: 'XBINNING', description: 'X binning factor' },
            { keyword: 'YBINNING', description: 'Y binning factor' }
        ];
        
        commonFitsHeaders.innerHTML = COMMON_FITS_HEADERS.map(header => `
            <div class="fits-header-option">
                <input type="checkbox" class="common-header-checkbox" id="fits-${header.keyword}" value="${header.keyword}" 
                       ${fitsHeaders.includes(header.keyword) ? 'checked' : ''}>
                <label for="fits-${header.keyword}">${header.keyword} - ${header.description}</label>
            </div>
        `).join('');
    }
    
    /**
     * Update selected FITS headers display
     */
    updateSelectedFitsHeadersDisplay() {
        const selectedFitsHeaders = document.getElementById('selected-fits-headers');
        const fitsHeaders = appState.get('fitsHeaders') || [];
        
        if (!selectedFitsHeaders) return;
        
        if (fitsHeaders.length === 0) {
            selectedFitsHeaders.innerHTML = '<div class="no-headers">No headers selected</div>';
            return;
        }
        
        selectedFitsHeaders.innerHTML = fitsHeaders.map(header => `
            <div class="selected-header-item">
                <span class="header-name">${header}</span>
                <button class="remove-header" onclick="removeFitsHeader('${header}')">×</button>
            </div>
        `).join('');
    }
    
    /**
     * Add keyword to the list
     * @param {string} keyword - Keyword to add
     */
    addKeyword(keyword) {
        if (!keyword || keyword.trim() === '') return;
        
        const keywords = appState.get('keywords') || [];
        const trimmedKeyword = keyword.trim();
        
        if (!keywords.includes(trimmedKeyword)) {
            keywords.push(trimmedKeyword);
            appState.set('keywords', keywords);
            localStorage.setItem('keywords', JSON.stringify(keywords));
            this.updateKeywordsDisplay();
            this.updateTestResults();
        }
    }
    
    /**
     * Add FITS header to the list
     * @param {string} header - FITS header to add
     */
    addFitsHeader(header) {
        if (!header || header.trim() === '') return;
        
        const fitsHeaders = appState.get('fitsHeaders') || [];
        const trimmedHeader = header.trim().toUpperCase();
        
        if (!fitsHeaders.includes(trimmedHeader)) {
            fitsHeaders.push(trimmedHeader);
            appState.set('fitsHeaders', fitsHeaders);
            localStorage.setItem('fitsHeaders', JSON.stringify(fitsHeaders));
            this.updateSelectedFitsHeadersDisplay();
            
            // Update common headers checkboxes
            const checkbox = document.getElementById(`fits-${trimmedHeader}`);
            if (checkbox) {
                checkbox.checked = true;
            }
        }
    }
    
    /**
     * Remove FITS header from selected list (but keep checkbox state)
     * @param {string} header - FITS header to remove
     */
    removeFitsHeaderFromSelected(header) {
        const fitsHeaders = appState.get('fitsHeaders') || [];
        const index = fitsHeaders.indexOf(header);
        
        if (index !== -1) {
            fitsHeaders.splice(index, 1);
            appState.set('fitsHeaders', fitsHeaders);
            localStorage.setItem('fitsHeaders', JSON.stringify(fitsHeaders));
            this.updateSelectedFitsHeadersDisplay();
        }
    }
    
    /**
     * Save keywords dialog
     */
    saveKeywordsDialog() {
        const keywordDialog = document.getElementById('keyword-dialog');
        if (keywordDialog) {
            keywordDialog.style.display = 'none';
        }
        
        // Refresh file list to show new columns
        if (fileListManager) {
            fileListManager.refreshDisplay();
        }
    }
    
    /**
     * Cancel keywords dialog
     */
    cancelKeywordsDialog() {
        const keywordDialog = document.getElementById('keyword-dialog');
        if (keywordDialog) {
            keywordDialog.style.display = 'none';
        }
        
        // Reload keywords from localStorage to revert changes
        const savedKeywords = localStorage.getItem('keywords');
        if (savedKeywords) {
            try {
                const keywords = JSON.parse(savedKeywords);
                appState.set('keywords', keywords);
            } catch (error) {
                console.error('Error loading saved keywords:', error);
                appState.set('keywords', []);
            }
        } else {
            appState.set('keywords', []);
        }
    }
    
    /**
     * Save FITS headers dialog
     */
    saveFitsHeadersDialog() {
        const fitsHeadersDialog = document.getElementById('fits-headers-dialog');
        if (fitsHeadersDialog) {
            fitsHeadersDialog.style.display = 'none';
        }
        
        // Refresh file list to show new columns
        if (fileListManager) {
            fileListManager.refreshDisplay();
        }
    }
    
    /**
     * Cancel FITS headers dialog
     */
    cancelFitsHeadersDialog() {
        const fitsHeadersDialog = document.getElementById('fits-headers-dialog');
        if (fitsHeadersDialog) {
            fitsHeadersDialog.style.display = 'none';
        }
        
        // Reload FITS headers from localStorage to revert changes
        const savedFitsHeaders = localStorage.getItem('fitsHeaders');
        if (savedFitsHeaders) {
            try {
                const fitsHeaders = JSON.parse(savedFitsHeaders);
                appState.set('fitsHeaders', fitsHeaders);
            } catch (error) {
                console.error('Error loading saved FITS headers:', error);
                appState.set('fitsHeaders', []);
            }
        } else {
            appState.set('fitsHeaders', []);
        }
    }
    
    /**
     * Handle directory changed event
     * @param {Array} files - Updated file list
     */
    handleDirectoryChanged(files) {
        console.log('Directory changed, updating file list');
        appState.set('currentFiles', files);
    }
    
    /**
     * Handle watcher error
     * @param {Error} error - Watcher error
     */
    handleWatcherError(error) {
        console.error('Directory watcher error:', error);
        appState.set('isWatchingDirectory', false);
        
        // Show user notification
        this.showNotification('Directory watching stopped due to error', 'error');
    }
    
    /**
     * Handle display image event
     * @param {CustomEvent} event - Display image event
     */
    handleDisplayImage(event) {
        const { index } = event.detail;
        appState.set('currentImageIndex', index);
    }
    
    /**
     * Handle playback next event
     */
    handlePlaybackNext() {
        const currentIndex = appState.get('currentImageIndex');
        const files = appState.get('currentFiles');
        
        if (files && files.length > 0) {
            const nextIndex = (currentIndex + 1) % files.length;
            appState.set('currentImageIndex', nextIndex);
        }
    }
    
    /**
     * Handle playback previous event
     */
    handlePlaybackPrevious() {
        const currentIndex = appState.get('currentImageIndex');
        const files = appState.get('currentFiles');
        
        if (files && files.length > 0) {
            const prevIndex = currentIndex <= 0 ? files.length - 1 : currentIndex - 1;
            appState.set('currentImageIndex', prevIndex);
        }
    }
    
    /**
     * Handle keyboard shortcuts
     * @param {KeyboardEvent} event - Keyboard event
     */
    handleKeyboardShortcuts(event) {
        // Only handle shortcuts when not in input fields
        if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') {
            return;
        }
        
        switch (event.key) {
            case 'ArrowLeft':
                event.preventDefault();
                this.handlePlaybackPrevious();
                break;
            case 'ArrowRight':
                event.preventDefault();
                this.handlePlaybackNext();
                break;
            case ' ':
                event.preventDefault();
                this.togglePlayback();
                break;
            case 'f':
                event.preventDefault();
                imageDisplayManager.zoomFit();
                break;
            case '1':
                event.preventDefault();
                imageDisplayManager.zoomActual();
                break;
            case '+':
            case '=':
                event.preventDefault();
                imageDisplayManager.zoomIn();
                break;
            case '-':
                event.preventDefault();
                imageDisplayManager.zoomOut();
                break;
        }
    }
    
    /**
     * Toggle playback
     */
    togglePlayback() {
        const isPlaying = appState.get('isPlaying');
        appState.set('isPlaying', !isPlaying);
        
        if (!isPlaying) {
            // Start playback
            imageDisplayManager.scheduleNextSlide();
        } else {
            // Stop playback
            const timer = appState.getTimer('playbackTimer');
            if (timer) {
                clearTimeout(timer);
                appState.setTimer('playbackTimer', null);
            }
        }
    }
    
    /**
     * Load directory
     * @param {string} directoryPath - Directory path to load
     */
    async loadDirectory(directoryPath) {
        try {
            console.log('Loading directory:', directoryPath);
            
            const files = await window.electronAPI.loadDirectory(directoryPath);
            
            appState.update({
                currentDirectory: directoryPath,
                currentFiles: files,
                currentImageIndex: -1,
                selectedFiles: new Set()
            });
            
            // Start watching directory
            if (window.electronAPI.watchDirectory) {
                await window.electronAPI.watchDirectory(directoryPath);
                appState.set('isWatchingDirectory', true);
            }
            
            // Save as last directory
            localStorage.setItem('astro-last-directory', directoryPath);
            
            console.log(`Loaded ${files.length} files from directory`);
            
        } catch (error) {
            console.error('Error loading directory:', error);
            this.showNotification('Failed to load directory', 'error');
        }
    }
    
    /**
     * Show notification to user
     * @param {string} message - Notification message
     * @param {string} type - Notification type ('info', 'warning', 'error')
     */
    showNotification(message, type = 'info') {
        // For now, just use console. Could be enhanced with toast notifications
        console.log(`[${type.toUpperCase()}] ${message}`);
    }
    
    /**
     * Handle global errors
     * @param {ErrorEvent} event - Error event
     */
    handleGlobalError(event) {
        console.error('Global error:', event.error);
        this.showNotification('An unexpected error occurred', 'error');
    }
    
    /**
     * Handle unhandled promise rejections
     * @param {PromiseRejectionEvent} event - Promise rejection event
     */
    handleUnhandledRejection(event) {
        console.error('Unhandled promise rejection:', event.reason);
        this.showNotification('An unexpected error occurred', 'error');
        event.preventDefault(); // Prevent the default browser behavior
    }
    
    /**
     * Handle before unload
     * @param {BeforeUnloadEvent} event - Before unload event
     */
    handleBeforeUnload(event) {
        // Clean up resources
        appState.clearAllTimers();
        
        // Save current state
        try {
            const keywords = appState.get('keywords');
            const fitsHeaders = appState.get('fitsHeaders');
            const currentDirectory = appState.get('currentDirectory');
            
            if (keywords) localStorage.setItem('astro-keywords', JSON.stringify(keywords));
            if (fitsHeaders) localStorage.setItem('astro-fits-headers', JSON.stringify(fitsHeaders));
            if (currentDirectory) localStorage.setItem('astro-last-directory', currentDirectory);
            
        } catch (error) {
            console.error('Error saving state on unload:', error);
        }
    }
    
    /**
     * Handle initialization error
     * @param {Error} error - Initialization error
     */
    handleInitializationError(error) {
        console.error('Application initialization failed:', error);
        
        // Show error message to user
        const errorDiv = document.createElement('div');
        errorDiv.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: #ff4444;
            color: white;
            padding: 20px;
            border-radius: 8px;
            z-index: 10000;
            text-align: center;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        `;
        errorDiv.innerHTML = `
            <h3>Application Failed to Initialize</h3>
            <p>Please refresh the page or restart the application.</p>
            <p><small>Error: ${error.message}</small></p>
        `;
        document.body.appendChild(errorDiv);
    }
    
    /**
     * Get manager instance
     * @param {string} name - Manager name
     * @returns {Object|null} Manager instance
     */
    getManager(name) {
        return this.managers.get(name) || null;
    }
    
    /**
     * Shutdown the application
     */
    shutdown() {
        console.log('Shutting down application...');
        
        // Clear all timers
        appState.clearAllTimers();
        
        // Reset state
        appState.reset();
        
        // Clear managers
        this.managers.clear();
        
        this.initialized = false;
        console.log('Application shutdown complete');
    }
}

// Initialize application when DOM is ready
const app = new AstroImagesApp();

// Start the application
app.init().catch(error => {
    console.error('Failed to start application:', error);
});

// Export for global access if needed
window.astroApp = app;

// Make functions available globally for HTML onclick handlers
// TODO: Refactor these to use proper event handlers
window.toggleFileSelection = (index, event) => {
    fileListManager.toggleFileSelection(index, event);
};

window.toggleAllFileSelection = (event) => {
    fileListManager.toggleAllFileSelection(event);
};

// Dialog-related global functions
window.removeKeyword = (keyword) => {
    const keywords = appState.get('keywords') || [];
    const updatedKeywords = keywords.filter(k => k !== keyword);
    appState.set('keywords', updatedKeywords);
    localStorage.setItem('astro-keywords', JSON.stringify(updatedKeywords));
    app.updateKeywordsDisplay();
};

window.removeFitsHeader = (header) => {
    const fitsHeaders = appState.get('fitsHeaders') || [];
    const updatedHeaders = fitsHeaders.filter(h => h !== header);
    appState.set('fitsHeaders', updatedHeaders);
    localStorage.setItem('astro-fits-headers', JSON.stringify(updatedHeaders));
    app.updateSelectedFitsHeadersDisplay();
};

// Export modules for other scripts if needed
export { app, appState, domElements, fileListManager, imageDisplayManager };