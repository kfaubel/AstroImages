/**
 * AstroImages - Renderer Process
 * Handles all UI interactions and user interface logic
 * 
 * This file manages the main application interface, including:
 * - File list management and display
 * - Image viewing and zoom controls
 * - FITS header display and parsing
 * - Directory watching and file operations
 */

// ===== GLOBAL APPLICATION STATE =====
let currentDirectory = null;           // Currently selected directory path
let currentFiles = [];                 // Array of image files in current directory
let keywords = [];                     // Custom keywords for filename parsing
let fitsHeaders = [];                  // FITS headers to display in table
let sortColumn = null;                 // Current sort column identifier
let sortDirection = 'asc';             // Sort direction: 'asc' or 'desc'
let selectedFiles = new Set();         // Set of selected file indices
let isWatchingDirectory = false;       // Whether directory watching is active

// ===== PLAYBACK CONTROL STATE =====
let currentImageIndex = -1;           // Index of currently displayed image
let isPlaying = false;                // Whether slideshow is active
let playbackTimer = null;             // Timer handle for slideshow
let playbackInterval = 2000;          // Milliseconds between images (2 seconds)

// ===== ZOOM AND PAN CONTROL STATE =====
let currentZoom = 1.0;                // Current zoom level (1.0 = 100%)
let fitToWindowScale = 1.0;           // Scale factor for fit-to-window mode
let isFitToWindow = true;             // Whether image is in fit-to-window mode
let isDragging = false;               // Whether user is currently dragging image
let lastMouseX = 0;                   // Last recorded mouse X position
let lastMouseY = 0;                   // Last recorded mouse Y position
let imageNaturalWidth = 0;            // Original width of current image
let imageNaturalHeight = 0;           // Original height of current image

// ===== FITS HEADER DEFINITIONS =====
// Common astronomical image metadata keywords with human-readable descriptions
const COMMON_FITS_HEADERS = [
    { keyword: 'AIRMASS', description: 'Airmass at observation' },
    { keyword: 'APERTURE', description: 'Aperture diameter' },
    { keyword: 'BINNING', description: 'Binning factor' },
    { keyword: 'BITPIX', description: 'Bits per pixel' },
    { keyword: 'BZERO', description: 'Zero offset for data values' },
    { keyword: 'CALSTAT', description: 'Calibration status' },
    { keyword: 'CAMERA', description: 'Camera model' },
    { keyword: 'CAMERAID', description: 'Camera identifier' },
    { keyword: 'CCD-TEMP', description: 'CCD temperature' },
    { keyword: 'CCDTEMP', description: 'CCD temperature (alternative)' },
    { keyword: 'CENTALT', description: 'Central altitude' },
    { keyword: 'CENTAZ', description: 'Central azimuth' },
    { keyword: 'DATAMAX', description: 'Maximum data value' },
    { keyword: 'DATAMIN', description: 'Minimum data value' },
    { keyword: 'DATE-AVG', description: 'Average date of observation' },
    { keyword: 'DATE-LOC', description: 'Local date of observation' },
    { keyword: 'DATE-OBS', description: 'Date of observation' },
    { keyword: 'DEC', description: 'Declination' },
    { keyword: 'EGAIN', description: 'Effective gain' },
    { keyword: 'EXPTIME', description: 'Exposure time in seconds' },
    { keyword: 'EXPOSURE', description: 'Exposure time in seconds (alternative)' },
    { keyword: 'EXTEND', description: 'FITS extension indicator' },
    { keyword: 'FILTER', description: 'Filter used (common extension)' },
    { keyword: 'FILTNAM', description: 'Filter name (alternative)' },
    { keyword: 'FILTPOS', description: 'Filter position/number' },
    { keyword: 'FOCALLEN', description: 'Focal length' },
    { keyword: 'FOCPOS', description: 'Focuser position' },
    { keyword: 'FOCRATIO', description: 'Focal ratio (f-stop)' },
    { keyword: 'FOCUSER', description: 'Focuser position (alternative)' },
    { keyword: 'GAIN', description: 'Detector gain' },
    { keyword: 'IMAGETYP', description: 'Type of image (Light, Dark, Flat, etc.)' },
    { keyword: 'INSTRUME', description: 'Instrument name' },
    { keyword: 'NAXIS', description: 'Number of axes' },
    { keyword: 'NAXIS1', description: 'Length of first axis' },
    { keyword: 'NAXIS2', description: 'Length of second axis' },
    { keyword: 'OBJECT', description: 'Object/target name' },
    { keyword: 'OBJCTDEC', description: 'Object center DEC' },
    { keyword: 'OBJCTRA', description: 'Object center RA' },
    { keyword: 'OBSERVER', description: 'Observer name' },
    { keyword: 'OFFSET', description: 'Offset/bias level' },
    { keyword: 'PEDESTAL', description: 'Pedestal/bias level' },
    { keyword: 'PIERSIDE', description: 'Pier side (East/West)' },
    { keyword: 'PROGRAM', description: 'Acquisition program' },
    { keyword: 'RA', description: 'Right Ascension' },
    { keyword: 'READNOIS', description: 'Read noise' },
    { keyword: 'SET-TEMP', description: 'Set temperature' },
    { keyword: 'SETTEMP', description: 'Set temperature (alternative)' },
    { keyword: 'SIMPLE', description: 'Simple FITS format indicator' },
    { keyword: 'SITEELEV', description: 'Site elevation' },
    { keyword: 'SITELAT', description: 'Site latitude' },
    { keyword: 'SWCREATE', description: 'Software used to create file' },
    { keyword: 'TELESCOP', description: 'Telescope name' },
    { keyword: 'TEMPERAT', description: 'Temperature' },
    { keyword: 'TIME-OBS', description: 'Time of observation' },
    { keyword: 'USBLIMIT', description: 'USB bandwidth limit' },
    { keyword: 'XBINNING', description: 'X-axis binning' },
    { keyword: 'XPIXSZ', description: 'X pixel size in microns' },
    { keyword: 'YBINNING', description: 'Y-axis binning' },
    { keyword: 'YPIXSZ', description: 'Y pixel size in microns' }
];

// DOM elements
const fileListDiv = document.getElementById('file-list');
const imageContainer = document.getElementById('image-container');
const imageDisplay = document.getElementById('image-display');
const noImageDiv = document.querySelector('.no-image');

// Playback control elements
const playbackControls = document.getElementById('playback-controls');
const moveSelectedBtn = document.getElementById('move-selected-btn');
const gotoFirstBtn = document.getElementById('goto-first');
const gotoPreviousBtn = document.getElementById('goto-previous');
const playPauseBtn = document.getElementById('play-pause');
const gotoNextBtn = document.getElementById('goto-next');
const gotoLastBtn = document.getElementById('goto-last');
const playIcon = playPauseBtn.querySelector('.play-icon');
const pauseIcon = playPauseBtn.querySelector('.pause-icon');

// Zoom control elements
const zoomControls = document.getElementById('zoom-controls');
const zoomInBtn = document.getElementById('zoom-in');
const zoomOutBtn = document.getElementById('zoom-out');
const zoomActualBtn = document.getElementById('zoom-actual');
const zoomFitBtn = document.getElementById('zoom-fit');
const zoomLevel = document.getElementById('zoom-level');
const autoStretchCheckbox = document.getElementById('auto-stretch');

// Resize elements
const sidebar = document.getElementById('sidebar');
const resizeHandle = document.getElementById('resize-handle');
const mainContent = document.getElementById('main-content');

// Dialog elements
const keywordDialog = document.getElementById('keyword-dialog');
const closeDialogBtn = document.getElementById('close-dialog');
const keywordInput = document.getElementById('keyword-input');
const addKeywordBtn = document.getElementById('add-keyword');
const keywordsContainer = document.getElementById('keywords-container');
const testFilename = document.getElementById('test-filename');
const testResults = document.getElementById('test-results');
const saveKeywordsBtn = document.getElementById('save-keywords');
const cancelKeywordsBtn = document.getElementById('cancel-keywords');

// Move dialog elements
const moveDialog = document.getElementById('move-dialog');
const closeMoveDialogBtn = document.getElementById('close-move-dialog');
const moveFileCount = document.getElementById('move-file-count');
const destinationPath = document.getElementById('destination-path');
const browseDestinationBtn = document.getElementById('browse-destination');
const executeMoveBtn = document.getElementById('execute-move');
const cancelMoveBtn = document.getElementById('cancel-move');

// FITS headers dialog elements
const fitsHeadersDialog = document.getElementById('fits-headers-dialog');
const closeFitsHeadersDialogBtn = document.getElementById('close-fits-headers-dialog');
const commonFitsHeaders = document.getElementById('common-fits-headers');
const customHeaderInput = document.getElementById('custom-header-input');
const addCustomHeaderBtn = document.getElementById('add-custom-header');
const selectedFitsHeaders = document.getElementById('selected-fits-headers');
const saveFitsHeadersBtn = document.getElementById('save-fits-headers');
const cancelFitsHeadersBtn = document.getElementById('cancel-fits-headers');

// Loading overlay elements
const loadingOverlay = document.getElementById('loading-overlay');
const loadingText = document.getElementById('loading-text');

// Loading overlay functions
let loadingTimeout = null;

function showLoading(message = 'Loading...') {
    // Clear any pending hide timeout
    if (loadingTimeout) {
        clearTimeout(loadingTimeout);
        loadingTimeout = null;
    }

    loadingText.textContent = message;
    loadingOverlay.style.display = 'flex';
}

function hideLoading() {
    // Add a small delay to prevent flashing for very quick operations
    loadingTimeout = setTimeout(() => {
        loadingOverlay.style.display = 'none';
        loadingTimeout = null;
    }, 100);
}

// Event listeners
closeDialogBtn.addEventListener('click', closeKeywordDialog);
addKeywordBtn.addEventListener('click', addKeyword);
saveKeywordsBtn.addEventListener('click', saveKeywords);
cancelKeywordsBtn.addEventListener('click', closeKeywordDialog);
testFilename.addEventListener('input', updateTestResults);
keywordInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') addKeyword();
});

// Move dialog event listeners
closeMoveDialogBtn.addEventListener('click', closeMoveDialog);
browseDestinationBtn.addEventListener('click', browseDestination);
executeMoveBtn.addEventListener('click', executeMoveFiles);
cancelMoveBtn.addEventListener('click', closeMoveDialog);

// FITS headers dialog event listeners
closeFitsHeadersDialogBtn.addEventListener('click', closeFitsHeadersDialog);
addCustomHeaderBtn.addEventListener('click', addCustomFitsHeader);
saveFitsHeadersBtn.addEventListener('click', saveFitsHeaders);
cancelFitsHeadersBtn.addEventListener('click', closeFitsHeadersDialog);
customHeaderInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') addCustomFitsHeader();
});

// Move selected button event listener
moveSelectedBtn.addEventListener('click', showMoveDialog);

// Playback control event listeners
gotoFirstBtn.addEventListener('click', gotoFirstImage);
gotoPreviousBtn.addEventListener('click', gotoPreviousImage);
playPauseBtn.addEventListener('click', togglePlayback);
gotoNextBtn.addEventListener('click', gotoNextImage);
gotoLastBtn.addEventListener('click', gotoLastImage);

// Keyboard shortcuts for playback
document.addEventListener('keydown', (e) => {
    // Only handle keyboard shortcuts when no dialog is open and not typing in an input
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
    if (keywordDialog.style.display === 'flex' || moveDialog.style.display === 'flex' || fitsHeadersDialog.style.display === 'flex') return;

    switch (e.key) {
        case 'Home':
            e.preventDefault();
            gotoFirstImage();
            break;
        case 'ArrowLeft':
            e.preventDefault();
            gotoPreviousImage();
            break;
        case ' ':
        case 'Spacebar':
            e.preventDefault();
            togglePlayback();
            break;
        case 'ArrowRight':
            e.preventDefault();
            gotoNextImage();
            break;
        case 'End':
            e.preventDefault();
            gotoLastImage();
            break;
    }
});

// Zoom control event listeners
zoomInBtn.addEventListener('click', zoomIn);
zoomOutBtn.addEventListener('click', zoomOut);
zoomActualBtn.addEventListener('click', zoomActual);
zoomFitBtn.addEventListener('click', zoomFit);

// Auto-stretch event listener
autoStretchCheckbox.addEventListener('change', handleAutoStretchChange);

// Restore auto-stretch setting from localStorage
const savedAutoStretch = localStorage.getItem('autoStretchEnabled');
if (savedAutoStretch !== null) {
    autoStretchCheckbox.checked = savedAutoStretch === 'true';
}

// Image panning event listeners
imageContainer.addEventListener('mousedown', startImageDrag);
imageContainer.addEventListener('mousemove', handleImageDrag);
imageContainer.addEventListener('mouseup', stopImageDrag);
imageContainer.addEventListener('mouseleave', stopImageDrag);

// Mouse wheel zoom
imageContainer.addEventListener('wheel', handleMouseWheel);

// Handle move type radio button changes
document.querySelectorAll('input[name="move-type"]').forEach(radio => {
    radio.addEventListener('change', updateMoveButtonState);
});

// Click outside dialog to close
keywordDialog.addEventListener('click', (e) => {
    if (e.target === keywordDialog) closeKeywordDialog();
});

moveDialog.addEventListener('click', (e) => {
    if (e.target === moveDialog) closeMoveDialog();
});

fitsHeadersDialog.addEventListener('click', (e) => {
    if (e.target === fitsHeadersDialog) closeFitsHeadersDialog();
});

// Resize functionality
let isResizing = false;

resizeHandle.addEventListener('mousedown', (e) => {
    isResizing = true;
    resizeHandle.classList.add('dragging');
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
    e.preventDefault();
});

document.addEventListener('mousemove', (e) => {
    if (!isResizing) return;

    const containerRect = document.querySelector('.container').getBoundingClientRect();
    const newWidth = e.clientX - containerRect.left;
    const minWidth = 250;
    const maxWidth = window.innerWidth * 0.7;

    if (newWidth >= minWidth && newWidth <= maxWidth) {
        sidebar.style.width = newWidth + 'px';
    }
});

document.addEventListener('mouseup', () => {
    if (isResizing) {
        isResizing = false;
        resizeHandle.classList.remove('dragging');
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // Save the sidebar width to localStorage
        localStorage.setItem('sidebarWidth', sidebar.style.width);
    }
});

// Load saved sidebar width
function loadSidebarWidth() {
    const savedWidth = localStorage.getItem('sidebarWidth');
    if (savedWidth) {
        sidebar.style.width = savedWidth;
    }
}

// Checkbox selection functionality
function toggleFileSelection(index, event) {
    event.stopPropagation(); // Prevent file selection when clicking checkbox

    if (selectedFiles.has(index)) {
        selectedFiles.delete(index);
    } else {
        selectedFiles.add(index);
    }

    console.log(`File selection toggled for index ${index}. Selected files: [${Array.from(selectedFiles).join(', ')}]`);

    updateSelectAllCheckbox();
    updateFileCheckbox(index);
}

function toggleSelectAll() {
    const selectAllCheckbox = document.getElementById('select-all-checkbox');

    if (selectAllCheckbox.checked) {
        // Select all files
        selectedFiles.clear();
        for (let i = 0; i < currentFiles.length; i++) {
            selectedFiles.add(i);
        }
        console.log('All files selected');
    } else {
        // Deselect all files
        selectedFiles.clear();
        console.log('All files deselected');
    }

    updateAllFileCheckboxes();
}

function updateSelectAllCheckbox() {
    const selectAllCheckbox = document.getElementById('select-all-checkbox');
    if (!selectAllCheckbox) return;

    if (selectedFiles.size === 0) {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = false;
    } else if (selectedFiles.size === currentFiles.length) {
        selectAllCheckbox.checked = true;
        selectAllCheckbox.indeterminate = false;
    } else {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = true;
    }

    updateSelectionInfo();
}

function updateFileCheckbox(index) {
    const checkbox = document.querySelector(`[data-file-index="${index}"] .file-checkbox`);
    if (checkbox) {
        checkbox.checked = selectedFiles.has(index);
    }
}

function updateAllFileCheckboxes() {
    currentFiles.forEach((_, index) => {
        updateFileCheckbox(index);
    });
}

function getSelectedFiles() {
    return Array.from(selectedFiles).map(index => currentFiles[index]);
}

function updateSelectionInfo() {
    const count = selectedFiles.size;

    // Update move selected button state
    moveSelectedBtn.disabled = count === 0;

    // Update menu state (selection info UI was removed)
    window.electronAPI.updateMenuState(count > 0);
}

function clearSelection() {
    selectedFiles.clear();
    updateSelectAllCheckbox();
    updateAllFileCheckboxes();
}

// Directory watching functions
async function startWatchingDirectory(directoryPath) {
    if (!directoryPath) return;

    try {
        // Stop any existing watcher first
        await stopWatchingDirectory();

        const result = await window.electronAPI.startWatchingDirectory(directoryPath);
        if (result.success) {
            isWatchingDirectory = true;
            console.log('Started watching directory:', directoryPath);
        } else {
            console.error('Failed to start watching directory:', result.error);
        }
    } catch (error) {
        console.error('Error starting directory watcher:', error);
    }
}

async function stopWatchingDirectory() {
    if (!isWatchingDirectory) return;

    try {
        const result = await window.electronAPI.stopWatchingDirectory();
        if (result.success) {
            isWatchingDirectory = false;
            console.log('Stopped watching directory');
        } else {
            console.error('Failed to stop watching directory:', result.error);
        }
    } catch (error) {
        console.error('Error stopping directory watcher:', error);
    }
}

function handleDirectoryChange(changeInfo) {
    console.log('Directory change detected:', changeInfo);

    // Add a small delay to allow file operations to complete
    setTimeout(async () => {
        try {
            // Remember the current image being displayed
            const currentImagePath = currentImageIndex >= 0 && currentImageIndex < currentFiles.length
                ? currentFiles[currentImageIndex].path
                : null;

            // Reload the file list
            const previousFiles = [...currentFiles];
            currentFiles = await window.electronAPI.readDirectory(currentDirectory);

            // Check if the currently displayed image still exists
            if (currentImagePath) {
                const currentImageStillExists = currentFiles.some(file => file.path === currentImagePath);

                if (!currentImageStillExists) {
                    // Current image has been deleted, hide it
                    hideImage();
                    currentImageIndex = -1;
                    console.log('Current image was deleted, hiding image display');
                } else {
                    // Update the current image index to match the new file list
                    const newIndex = currentFiles.findIndex(file => file.path === currentImagePath);
                    if (newIndex >= 0) {
                        currentImageIndex = newIndex;
                    }
                }
            }

            // Re-render the file list without auto-selecting an image
            clearSelection(); // Clear previous selections when loading new files
            renderFileList(false);

            console.log('File list refreshed due to directory change');
        } catch (error) {
            console.error('Error refreshing file list after directory change:', error);
        }
    }, 500); // 500ms delay to allow file operations to settle
}

function handleWatcherError(error) {
    console.error('Directory watcher error:', error);
    isWatchingDirectory = false;
    // Optionally show a user notification
    // You could add a toast notification here if desired
}

// Sorting functionality
function sortFiles(column) {
    if (sortColumn === column) {
        // Toggle sort direction if same column
        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        // New column, default to ascending
        sortColumn = column;
        sortDirection = 'asc';
    }

    // Sort the files array
    currentFiles.sort((a, b) => {
        let valueA, valueB;

        if (column === 'filename') {
            valueA = a.name.toLowerCase();
            valueB = b.name.toLowerCase();
        } else {
            // Keyword column
            const parsedA = parseFilename(a.name);
            const parsedB = parseFilename(b.name);
            valueA = (parsedA[column] || '').toLowerCase();
            valueB = (parsedB[column] || '').toLowerCase();
        }

        // Primary sort
        let comparison = 0;
        if (valueA < valueB) comparison = -1;
        else if (valueA > valueB) comparison = 1;

        // Secondary sort by filename if primary values are equal
        if (comparison === 0 && column !== 'filename') {
            const nameA = a.name.toLowerCase();
            const nameB = b.name.toLowerCase();
            if (nameA < nameB) comparison = -1;
            else if (nameA > nameB) comparison = 1;
        }

        return sortDirection === 'asc' ? comparison : -comparison;
    });

    // Re-render the file list
    renderFileList(false);
}

function updateSortIndicators() {
    // Remove all sort indicators
    document.querySelectorAll('.file-header-cell').forEach(cell => {
        cell.classList.remove('sort-asc', 'sort-desc');
        cell.classList.add('sortable');
    });

    // Add current sort indicator
    if (sortColumn) {
        const headerCell = document.querySelector(`[data-sort-column="${sortColumn}"]`);
        if (headerCell) {
            headerCell.classList.add(sortDirection === 'asc' ? 'sort-asc' : 'sort-desc');
        }
    }
}

// Filename parsing functions
function parseFilename(filename) {
    const baseName = filename.replace(/\.[^/.]+$/, ""); // Remove extension
    const tokens = baseName.split('_');
    const parsed = {};

    console.log('DEBUG: Parsing filename:', filename);
    console.log('DEBUG: Base name:', baseName);
    console.log('DEBUG: Tokens:', tokens);
    console.log('DEBUG: Available keywords:', keywords);

    for (let i = 0; i < tokens.length - 1; i++) {
        const token = tokens[i];
        console.log(`DEBUG: Checking token ${i}: "${token}"`);
        if (keywords.includes(token)) {
            parsed[token] = tokens[i + 1];
            console.log(`DEBUG: Match found! ${token} = ${tokens[i + 1]}`);
        } else {
            console.log(`DEBUG: No match for token "${token}"`);
        }
    }

    console.log('DEBUG: Final parsed result:', parsed);
    return parsed;
}

// FITS header parsing functions
async function getFitsHeaders(file) {
    if (!file.isFits) {
        return {};
    }

    try {
        const result = await window.electronAPI.getFitsHeaders(file.path);
        if (result.success) {
            return result.headers;
        } else {
            console.error('Error reading FITS headers:', result.error);
            return {};
        }
    } catch (error) {
        console.error('Error getting FITS headers:', error);
        return {};
    }
}

function updateTestResults() {
    const filename = testFilename.value.trim();
    if (!filename) {
        testResults.textContent = 'Enter a filename to test parsing';
        return;
    }

    const parsed = parseFilename(filename);
    if (Object.keys(parsed).length === 0) {
        testResults.textContent = 'No keywords found in filename';
    } else {
        const results = Object.entries(parsed)
            .map(([key, value]) => `${key}: ${value}`)
            .join('\n');
        testResults.textContent = `Parsed values:\n${results}`;
    }
}

// Dialog functions
function openKeywordDialog() {
    keywordDialog.style.display = 'flex';
    updateKeywordsDisplay();
    updateTestResults();
}

function closeKeywordDialog() {
    keywordDialog.style.display = 'none';
    keywordInput.value = '';
    testFilename.value = '';
    testResults.textContent = '';
}

function addKeyword() {
    const keyword = keywordInput.value.trim();
    if (!keyword) return;

    if (keywords.includes(keyword)) {
        alert('Keyword already exists');
        return;
    }

    keywords.push(keyword);
    keywordInput.value = '';
    updateKeywordsDisplay();
    updateTestResults();
}

function removeKeyword(keyword) {
    keywords = keywords.filter(k => k !== keyword);
    updateKeywordsDisplay();
    updateTestResults();
}

function updateKeywordsDisplay() {
    if (keywords.length === 0) {
        keywordsContainer.innerHTML = '<div class="no-keywords">No keywords defined</div>';
        return;
    }

    keywordsContainer.innerHTML = keywords.map(keyword => `
        <div class="keyword-item">
            <span class="keyword-text">${keyword}</span>
            <button class="remove-keyword" onclick="removeKeyword('${keyword}')">Remove</button>
        </div>
    `).join('');
}

function saveKeywords() {
    // Save keywords to localStorage
    localStorage.setItem('filenameKeywords', JSON.stringify(keywords));
    closeKeywordDialog();

    // Refresh file list to show new columns
    if (currentFiles.length > 0) {
        // Reset sort to filename when keywords change
        sortColumn = 'filename';
        sortDirection = 'asc';
        sortFiles('filename');
    }
}

function loadKeywords() {
    const saved = localStorage.getItem('filenameKeywords');
    console.log('DEBUG: loadKeywords - localStorage value:', saved);
    if (saved) {
        keywords = JSON.parse(saved);
        console.log('DEBUG: loadKeywords - parsed keywords:', keywords);
    } else {
        console.log('DEBUG: loadKeywords - no saved keywords found');
    }
    console.log('DEBUG: loadKeywords - final keywords array:', keywords);
}

// FITS headers dialog functions
function openFitsHeadersDialog() {
    populateCommonFitsHeaders();
    updateSelectedFitsHeadersDisplay();
    fitsHeadersDialog.style.display = 'flex';
}

function closeFitsHeadersDialog() {
    fitsHeadersDialog.style.display = 'none';
    customHeaderInput.value = '';
}

function populateCommonFitsHeaders() {
    commonFitsHeaders.innerHTML = COMMON_FITS_HEADERS.map(header => `
        <div class="fits-header-option">
            <input type="checkbox" id="fits-${header.keyword}" value="${header.keyword}" 
                   ${fitsHeaders.includes(header.keyword) ? 'checked' : ''}>
            <label for="fits-${header.keyword}">${header.keyword}</label>
            <div class="fits-header-description">${header.description}</div>
        </div>
    `).join('');

    // Add event listeners for checkboxes
    commonFitsHeaders.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
        checkbox.addEventListener('change', (e) => {
            if (e.target.checked) {
                if (!fitsHeaders.includes(e.target.value)) {
                    fitsHeaders.push(e.target.value);
                }
            } else {
                fitsHeaders = fitsHeaders.filter(h => h !== e.target.value);
            }
            updateSelectedFitsHeadersDisplay();
        });
    });
}

function addCustomFitsHeader() {
    const header = customHeaderInput.value.trim().toUpperCase();
    if (!header) return;

    if (fitsHeaders.includes(header)) {
        alert('Header already selected');
        return;
    }

    fitsHeaders.push(header);
    customHeaderInput.value = '';
    updateSelectedFitsHeadersDisplay();

    // Update the checkbox if it exists in common headers
    const checkbox = document.getElementById(`fits-${header}`);
    if (checkbox) {
        checkbox.checked = true;
    }
}

function removeFitsHeader(header) {
    fitsHeaders = fitsHeaders.filter(h => h !== header);
    updateSelectedFitsHeadersDisplay();

    // Update the checkbox if it exists in common headers
    const checkbox = document.getElementById(`fits-${header}`);
    if (checkbox) {
        checkbox.checked = false;
    }
}

function updateSelectedFitsHeadersDisplay() {
    if (fitsHeaders.length === 0) {
        selectedFitsHeaders.innerHTML = '<div class="no-headers">No headers selected</div>';
        return;
    }

    selectedFitsHeaders.innerHTML = fitsHeaders.map(header => `
        <div class="fits-header-item">
            <span class="fits-header-name">${header}</span>
            <button class="remove-fits-header" onclick="removeFitsHeader('${header}')">Remove</button>
        </div>
    `).join('');
}

function saveFitsHeaders() {
    // Save FITS headers to localStorage
    localStorage.setItem('fitsHeaders', JSON.stringify(fitsHeaders));
    closeFitsHeadersDialog();

    // Refresh file list to show new columns
    if (currentFiles.length > 0) {
        // Reset sort to filename when headers change
        sortColumn = 'filename';
        sortDirection = 'asc';
        sortFiles('filename');
    }
}

function loadFitsHeaders() {
    const saved = localStorage.getItem('fitsHeaders');
    if (saved) {
        fitsHeaders = JSON.parse(saved);
    }
}

// Move dialog functions
function showMoveDialog() {
    if (selectedFiles.size === 0) {
        alert('No files selected for moving.');
        return;
    }

    const count = selectedFiles.size;
    moveFileCount.textContent = `${count} file${count === 1 ? '' : 's'} selected for moving`;

    // Set default destination to current directory
    destinationPath.value = currentDirectory || '';

    // Reset to move option
    document.querySelector('input[name="move-type"][value="move"]').checked = true;

    updateMoveButtonState();
    moveDialog.style.display = 'flex';
}

function closeMoveDialog() {
    moveDialog.style.display = 'none';
    destinationPath.value = '';
}

async function browseDestination() {
    try {
        const selectedPath = await window.electronAPI.selectMoveDestination(currentDirectory);
        if (selectedPath) {
            destinationPath.value = selectedPath;
            updateMoveButtonState();
        }
    } catch (error) {
        console.error('Error selecting destination:', error);
        alert('Error selecting destination folder.');
    }
}

function updateMoveButtonState() {
    const moveType = document.querySelector('input[name="move-type"]:checked').value;
    const hasDestination = moveType === 'trash' || destinationPath.value.trim() !== '';
    executeMoveBtn.disabled = !hasDestination;

    // Update destination input visibility
    const destinationSection = document.querySelector('.destination-section');
    destinationSection.style.display = moveType === 'trash' ? 'none' : 'block';
}

async function executeMoveFiles() {
    const selectedFileObjects = getSelectedFiles();
    const filePaths = selectedFileObjects.map(file => file.path);
    const moveType = document.querySelector('input[name="move-type"]:checked').value;

    if (filePaths.length === 0) {
        alert('No files selected.');
        return;
    }

    try {
        const actionText = moveType === 'trash' ? 'Moving files to trash...' : 'Moving files...';
        showLoading(actionText);

        executeMoveBtn.disabled = true;
        executeMoveBtn.textContent = 'Moving...';

        let results;
        if (moveType === 'trash') {
            results = await window.electronAPI.moveFilesToTrash(filePaths);
        } else {
            const destination = destinationPath.value.trim();
            if (!destination) {
                hideLoading();
                alert('Please select a destination folder.');
                return;
            }
            results = await window.electronAPI.moveFiles(filePaths, destination);
        }

        // Process results
        const successful = results.filter(r => r.success);
        const failed = results.filter(r => !r.success);

        let message = '';
        if (successful.length > 0) {
            const action = moveType === 'trash' ? 'moved to trash' : 'moved';
            message += `${successful.length} file${successful.length === 1 ? '' : 's'} ${action} successfully.`;
        }

        if (failed.length > 0) {
            message += `\n${failed.length} file${failed.length === 1 ? '' : 's'} failed to move:`;
            failed.forEach(f => {
                const fileName = f.file.split('\\').pop() || f.file.split('/').pop();
                message += `\n- ${fileName}: ${f.error}`;
            });
        }

        alert(message);

        // Close dialog and refresh file list
        closeMoveDialog();
        clearSelection();

        // Reload the current directory to reflect changes
        if (currentDirectory) {
            await loadFiles();
        }

    } catch (error) {
        console.error('Error moving files:', error);
        alert('An error occurred while moving files: ' + error.message);
    } finally {
        hideLoading();
        executeMoveBtn.disabled = false;
        executeMoveBtn.textContent = 'Move Files';
    }
}

function saveLastDirectory(directoryPath) {
    localStorage.setItem('lastDirectory', directoryPath);
}

function loadLastDirectory() {
    return localStorage.getItem('lastDirectory');
}

async function loadLastDirectoryOnStartup() {
    const lastDir = loadLastDirectory();
    if (lastDir) {
        showLoading('Restoring last directory...');
        try {
            // Update title bar to show loading status
            await window.electronAPI.updateWindowTitle('Loading...');
            fileListDiv.innerHTML = '<div class="no-files">Loading files...</div>';

            // Check if directory still exists
            const dirExists = await window.electronAPI.checkDirectoryExists(lastDir);
            if (!dirExists) {
                throw new Error('Directory no longer exists');
            }

            // Load the directory
            currentDirectory = lastDir;
            await loadFiles();
            await window.electronAPI.updateWindowTitle(lastDir);

            // Start watching the restored directory
            await startWatchingDirectory(lastDir);
        } catch (error) {
            console.log('Last directory no longer accessible:', error);
            // Clear the saved directory if it's no longer accessible
            localStorage.removeItem('lastDirectory');
            currentDirectory = null;
            await window.electronAPI.updateWindowTitle(null);
            fileListDiv.innerHTML = '<div class="no-files">Select a folder to view images</div>';
        } finally {
            hideLoading();
        }
    }
}

async function selectFolder() {
    try {
        const folderPath = await window.electronAPI.selectDirectory();
        if (folderPath) {
            handleFolderSelection(folderPath);
        }
    } catch (error) {
        console.error('Error selecting folder:', error);
        hideLoading();
    }
}

async function handleFolderSelection(folderPath) {
    showLoading('Loading directory...');
    try {
        currentDirectory = folderPath;
        await window.electronAPI.updateWindowTitle(folderPath);
        saveLastDirectory(folderPath); // Save the selected directory
        await loadFiles();

        // Start watching the new directory
        await startWatchingDirectory(folderPath);
    } catch (error) {
        console.error('Error handling folder selection:', error);
    } finally {
        hideLoading();
    }
}

async function loadFiles() {
    showLoading('Loading files...');
    try {
        const previousFilesLength = currentFiles.length;
        currentFiles = await window.electronAPI.readDirectory(currentDirectory);
        clearSelection(); // Clear previous selections when loading new files

        // Check if this is a new directory (should auto-select first file)
        const shouldAutoSelectFirst = previousFilesLength === 0 && currentFiles.length > 0;
        await renderFileList(shouldAutoSelectFirst);

    } catch (error) {
        console.error('Error loading files:', error);
        renderEmptyFileList();
    } finally {
        hideLoading();
    }
}

async function renderFileList(autoSelectFirst = false) {
    console.log('DEBUG: renderFileList called with keywords:', keywords);
    
    if (currentFiles.length === 0) {
        renderEmptyFileList();
        return;
    }

    // Get FITS headers for all FITS files
    const fileData = await Promise.all(currentFiles.map(async (file, index) => {
        const parsed = parseFilename(file.name);
        const fitsHeaderData = await getFitsHeaders(file);
        return {
            file,
            index,
            parsed,
            fitsHeaderData
        };
    }));

    // Create proper HTML table structure for perfect column alignment
    let html = '<div class="file-list-container">';
    html += '<table class="file-list-table">';
    
    // Create table header
    html += '<thead class="file-header-fixed">';
    html += '<tr>';
    
    // Checkbox column header
    html += '<th class="file-header-cell file-header-checkbox">';
    html += '<input type="checkbox" id="select-all-checkbox" class="select-all-checkbox" onchange="toggleSelectAll()">';
    html += '</th>';

    html += '<th class="file-header-cell file-header-filename sortable" data-sort-column="filename">Filename</th>';

    // Add keyword columns
    keywords.forEach(keyword => {
        html += `<th class="file-header-cell file-header-keyword sortable" data-sort-column="${keyword}">${keyword}</th>`;
    });

    // Add FITS header columns
    fitsHeaders.forEach(header => {
        html += `<th class="file-header-cell file-header-fits sortable" data-sort-column="fits-${header}" title="FITS Header: ${header}">${header}</th>`;
    });

    html += '</tr>';
    html += '</thead>';

    // Create table body with scrollable file rows
    html += '<tbody class="file-list-scrollable">';

    // Create file rows
    fileData.forEach(({ file, index, parsed, fitsHeaderData }) => {
        html += `<tr class="file-row" data-index="${index}" data-file-index="${index}">`;

        // Checkbox cell
        html += '<td class="file-cell file-cell-checkbox">';
        html += `<input type="checkbox" class="file-checkbox" ${selectedFiles.has(index) ? 'checked' : ''} onchange="toggleFileSelection(${index}, event)">`;
        html += '</td>';

        // Filename cell
        html += `<td class="file-cell file-cell-filename">`;
        html += `<div class="tooltip">`;
        html += `<span class="file-name file-name-truncated">${file.name}</span>`;
        html += `<div class="tooltip-text">${file.name}</div>`;
        html += `</div>`;
        html += `</td>`;

        // Keyword value cells
        keywords.forEach(keyword => {
            const value = parsed[keyword] || '';
            console.log(`DEBUG: Rendering keyword "${keyword}" with value "${value}" for file ${file.name}`);
            html += `<td class="file-cell file-cell-keyword">`;
            if (value) {
                if (value.length > 8) {
                    html += `<div class="tooltip">`;
                    html += `<span class="keyword-value" style="max-width: 100%; overflow: hidden; text-overflow: ellipsis;">${value}</span>`;
                    html += `<div class="tooltip-text">${keyword}: ${value}</div>`;
                    html += `</div>`;
                } else {
                    html += `<span class="keyword-value">${value}</span>`;
                }
            } else {
                console.log(`DEBUG: No value found for keyword "${keyword}"`);
            }
            html += `</td>`;
        });

        // FITS header value cells
        fitsHeaders.forEach(header => {
            const value = fitsHeaderData[header] || '';
            const displayValue = String(value).slice(0, 15); // Limit display length
            html += `<td class="file-cell file-cell-fits">`;
            if (value) {
                if (String(value).length > 15) {
                    html += `<div class="tooltip">`;
                    html += `<span class="fits-value" style="max-width: 100%; overflow: hidden; text-overflow: ellipsis;">${displayValue}</span>`;
                    html += `<div class="tooltip-text">${header}: ${value}</div>`;
                    html += `</div>`;
                } else {
                    html += `<span class="fits-value">${displayValue}</span>`;
                }
            }
            html += `</td>`;
        });

        html += '</tr>';
    });

    html += '</tbody>';
    html += '</table>';
    html += '</div>'; // Close file-list-container

    fileListDiv.innerHTML = html;

    // Add click listeners to file rows
    document.querySelectorAll('.file-row').forEach(row => {
        row.addEventListener('click', (event) => {
            // Don't select file if clicking on checkbox
            if (event.target.type === 'checkbox') return;

            const index = parseInt(row.dataset.index);
            selectFile(index);
        });
    });

    // Add click listeners to header cells for sorting
    document.querySelectorAll('.file-header-cell[data-sort-column]').forEach(header => {
        header.addEventListener('click', () => {
            const column = header.dataset.sortColumn;
            sortFiles(column);
        });
    });

    // Update sort indicators and checkbox states
    updateSortIndicators();
    updateSelectAllCheckbox();

    // Update playback controls
    updatePlaybackButtons();

    // Auto-select first file if requested
    if (autoSelectFirst && currentFiles.length > 0) {
        // Use setTimeout to ensure DOM is fully rendered before selecting
        setTimeout(async () => {
            await selectFile(0);
        }, 0);
    }
}

function renderEmptyFileList() {
    fileListDiv.innerHTML = '<div class="no-files">No image files found in this folder</div>';
    clearSelection();
    hideImage();

    // Reset playback state and hide controls
    currentImageIndex = -1;
    stopPlayback();
    updatePlaybackButtons();
}

async function selectFile(index) {
    // Update current image index
    currentImageIndex = index;

    // Remove previous selection
    document.querySelectorAll('.file-row').forEach(item => {
        item.classList.remove('selected');
    });

    // Add selection to clicked item
    const selectedItem = document.querySelector(`[data-index="${index}"]`);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }

    // Update playback button states
    updatePlaybackButtons();

    const file = currentFiles[index];
    await displayImage(file);
}

async function displayImage(file) {
    try {
        // Show loading for image processing
        if (file.isFits) {
            showLoading('Processing FITS file...');
        } else {
            showLoading('Loading image...');
        }

        // Hide no-image placeholder
        noImageDiv.style.display = 'none';

        // Show loading state in image container
        imageDisplay.style.display = 'block';
        imageDisplay.src = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNTAiIGhlaWdodD0iNTAiIHZpZXdCb3g9IjAgMCA1MCA1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjUiIGN5PSIyNSIgcj0iMjAiIGZpbGw9Im5vbmUiIHN0cm9rZT0iIzk5OSIgc3Ryb2tlLXdpZHRoPSI0IiBzdHJva2UtZGFzaGFycmF5PSI4MCA4MCI+CjxhbmltYXRlVHJhbnNmb3JtIGF0dHJpYnV0ZU5hbWU9InRyYW5zZm9ybSIgdHlwZT0icm90YXRlIiB2YWx1ZXM9IjAgMjUgMjU7MzYwIDI1IDI1IiBkdXI9IjFzIiByZXBlYXRDb3VudD0iaW5kZWZpbml0ZSIvPgo8L2NpcmNsZT4KPC9zdmc+';

        let imageSrc;

        if (file.isFits) {
            // Process FITS file with optional stretching
            console.log('Processing FITS file:', file.path);
            try {
                const applyStretch = autoStretchCheckbox.checked;

                // Try to load thumbnail first for faster preview
                const thumbnail = await window.electronAPI.getFitsThumbnail(file.path, applyStretch);
                if (thumbnail) {
                    // Show thumbnail immediately
                    imageDisplay.src = thumbnail;
                    showLoading('Loading full resolution...');
                }

                // Then load full resolution
                imageSrc = await window.electronAPI.processFitsFileStretched(file.path, applyStretch);
                console.log('FITS processing completed successfully with stretch:', applyStretch);
            } catch (error) {
                console.error('FITS processing failed:', error);
                throw error;
            }
        } else {
            // Regular image file
            imageSrc = await window.electronAPI.getFilePath(file.path);
        }

        // Load the processed image and wait for it to complete
        await new Promise((resolve, reject) => {
            const tempImage = new Image();

            tempImage.onload = () => {
                // Image loaded successfully
                imageDisplay.src = imageSrc;

                // Store natural dimensions for zoom calculations
                imageNaturalWidth = tempImage.naturalWidth;
                imageNaturalHeight = tempImage.naturalHeight;

                // Show zoom controls first
                zoomControls.style.display = 'flex';

                resolve();
            };

            tempImage.onerror = () => {
                reject(new Error('Failed to load image'));
            };

            tempImage.src = imageSrc;
        });

        // Always fit to window after image is loaded and displayed
        // Use setTimeout to ensure the image is rendered and container dimensions are available
        setTimeout(() => {
            zoomFit();
            // Apply auto-stretch if enabled
            applyAutoStretch(autoStretchCheckbox.checked);
        }, 50);

        // Handle image load error (backup)
        imageDisplay.onerror = () => {
            hideImage();
            console.error('Failed to load image:', file.path);
        };

        // Trigger next slideshow step if playing
        if (isPlaying) {
            scheduleNextSlide();
        }

    } catch (error) {
        console.error('Error displaying image:', error);
        hideImage();

        // Still continue slideshow if playing, even if image failed to load
        if (isPlaying) {
            scheduleNextSlide();
        }
    } finally {
        hideLoading();
    }
}

function hideImage() {
    imageDisplay.style.display = 'none';
    noImageDiv.style.display = 'block';

    // Hide zoom controls
    zoomControls.style.display = 'none';

    // Reset scroll bars
    imageContainer.classList.remove('fit-to-window');

    // Reset current image index
    currentImageIndex = -1;
    updatePlaybackButtons();
}

// Initialize - load saved keywords, sidebar width, and last directory
document.addEventListener('DOMContentLoaded', async () => {
    loadKeywords();
    loadFitsHeaders();
    loadSidebarWidth();

    // Initialize playback controls
    updatePlayPauseButton();
    updatePlaybackButtons();

    // Listen for menu-triggered folder selection
    window.electronAPI.onFolderSelected((folderPath) => {
        handleFolderSelection(folderPath);
    });

    // Listen for menu-triggered move dialog
    window.electronAPI.onShowMoveDialog(() => {
        showMoveDialog();
    });

    // Listen for menu-triggered keywords dialog
    window.electronAPI.onShowKeywordsDialog(() => {
        openKeywordDialog();
    });

    // Listen for menu-triggered FITS headers dialog
    window.electronAPI.onShowFitsHeadersDialog(() => {
        openFitsHeadersDialog();
    });

    // Listen for menu-triggered zoom commands
    window.electronAPI.onZoomIn(() => {
        zoomIn();
    });

    window.electronAPI.onZoomOut(() => {
        zoomOut();
    });

    window.electronAPI.onZoomActual(() => {
        zoomActual();
    });

    window.electronAPI.onZoomFit(() => {
        zoomFit();
    });

    // Listen for directory changes
    window.electronAPI.onDirectoryChanged((changeInfo) => {
        handleDirectoryChange(changeInfo);
    });

    // Listen for watcher errors
    window.electronAPI.onWatcherError((error) => {
        handleWatcherError(error);
    });

    await loadLastDirectoryOnStartup();
    console.log('Image Viewer loaded with keywords:', keywords);
    if (currentDirectory) {
        console.log('Restored last directory:', currentDirectory);
    }

    // Add resize listener to maintain fit-to-window when pane size changes
    window.addEventListener('resize', () => {
        if (isFitToWindow && imageDisplay.src && imageDisplay.style.display !== 'none') {
            // Re-calculate zoom to fit the new container size
            const containerRect = imageContainer.getBoundingClientRect();
            const containerWidth = containerRect.width - 20; // Account for minimal padding
            const containerHeight = containerRect.height - 20;

            if (imageNaturalWidth > 0 && imageNaturalHeight > 0) {
                const scaleX = containerWidth / imageNaturalWidth;
                const scaleY = containerHeight / imageNaturalHeight;
                const scale = Math.min(scaleX, scaleY);

                // Update both current zoom and fit-to-window scale
                fitToWindowScale = scale;
                currentZoom = scale;
                imageDisplay.style.transform = `scale(${scale})`;
                updateZoomDisplay();
                updateZoomButtons();
                updateScrollBars();
            }
        }
    });
});

// Zoom control functions
function zoomIn() {
    isFitToWindow = false;
    setZoom(currentZoom * 1.25);
}

function zoomOut() {
    isFitToWindow = false;
    setZoom(currentZoom / 1.25);
}

function zoomActual() {
    isFitToWindow = false;
    // Set zoom to actual pixel size (1.0 scale factor)
    setZoom(1.0);
}

function zoomFit() {
    if (!imageDisplay.src || imageDisplay.style.display === 'none') return;

    const containerRect = imageContainer.getBoundingClientRect();
    const containerWidth = containerRect.width - 20; // Account for minimal padding
    const containerHeight = containerRect.height - 20;

    if (imageNaturalWidth > 0 && imageNaturalHeight > 0) {
        const scaleX = containerWidth / imageNaturalWidth;
        const scaleY = containerHeight / imageNaturalHeight;
        const scale = Math.min(scaleX, scaleY);

        // Set both the current zoom and the fit-to-window reference to the same value
        currentZoom = scale;
        fitToWindowScale = scale;
        isFitToWindow = true;

        // Apply the transform and update display
        imageDisplay.style.transform = `scale(${scale})`;
        updateZoomDisplay();
        updateZoomButtons();
        updateScrollBars();
    }
}

function setZoom(zoom) {
    currentZoom = Math.max(0.1, Math.min(5.0, zoom)); // Limit zoom between 10% and 500%

    if (imageDisplay.style.display !== 'none') {
        imageDisplay.style.transform = `scale(${currentZoom})`;
        updateZoomDisplay();
        updateZoomButtons();
        updateScrollBars();
    }
}

function updateScrollBars() {
    if (isFitToWindow || currentZoom <= 1.0) {
        imageContainer.classList.add('fit-to-window');
    } else {
        imageContainer.classList.remove('fit-to-window');
    }
}

function updateZoomDisplay() {
    if (isFitToWindow) {
        zoomLevel.textContent = 'Fit (100%)';
    } else {
        const percentage = Math.round(currentZoom * 100);
        zoomLevel.textContent = `${percentage}%`;
    }
}

function updateZoomButtons() {
    zoomInBtn.disabled = currentZoom >= 5.0;
    zoomOutBtn.disabled = currentZoom <= 0.1;
}

function resetZoom() {
    currentZoom = 1.0;
    if (imageDisplay.style.display !== 'none') {
        imageDisplay.style.transform = 'scale(1)';
        updateZoomDisplay();
        updateZoomButtons();
    }
}

// Get the currently displayed file
function getCurrentFile() {
    if (currentImageIndex >= 0 && currentImageIndex < currentFiles.length) {
        return currentFiles[currentImageIndex];
    }
    return null;
}

// Auto-stretch functionality
async function handleAutoStretchChange() {
    const isAutoStretchEnabled = autoStretchCheckbox.checked;

    // Save the setting to localStorage
    localStorage.setItem('autoStretchEnabled', isAutoStretchEnabled.toString());

    // If we have a current image displayed, apply the stretch
    if (imageDisplay.src && imageDisplay.style.display !== 'none') {
        const currentFile = getCurrentFile();
        if (currentFile && currentFile.isFits) {
            // For FITS files, we need to reprocess with the new stretch setting
            try {
                showLoading('Applying histogram stretch...');
                const imageSrc = await window.electronAPI.processFitsFileStretched(currentFile.path, isAutoStretchEnabled);
                imageDisplay.src = imageSrc;
                hideLoading();
            } catch (error) {
                console.error('Error applying stretch to FITS file:', error);
                hideLoading();
            }
        } else {
            // For regular images, use CSS filters
            applyAutoStretch(isAutoStretchEnabled);
        }
    }
}

function applyAutoStretch(enabled) {
    if (enabled) {
        // Apply auto-stretch using CSS filters
        // This is a basic implementation - for FITS files, more sophisticated
        // histogram stretching would typically be done in the main process
        imageDisplay.style.filter = 'contrast(1.2) brightness(1.1)';
    } else {
        // Remove auto-stretch
        imageDisplay.style.filter = '';
    }
}

// Image dragging functions
function startImageDrag(e) {
    if (currentZoom <= 1.0) return; // Only allow dragging when zoomed in

    e.preventDefault();
    isDragging = true;
    lastMouseX = e.clientX;
    lastMouseY = e.clientY;
    imageContainer.classList.add('dragging');
}

function handleImageDrag(e) {
    if (!isDragging || currentZoom <= 1.0) return;

    e.preventDefault();
    const deltaX = e.clientX - lastMouseX;
    const deltaY = e.clientY - lastMouseY;

    imageContainer.scrollLeft -= deltaX;
    imageContainer.scrollTop -= deltaY;

    lastMouseX = e.clientX;
    lastMouseY = e.clientY;
}

function stopImageDrag() {
    isDragging = false;
    imageContainer.classList.remove('dragging');
}

function handleMouseWheel(e) {
    if (e.ctrlKey) {
        e.preventDefault();
        const zoomDirection = e.deltaY > 0 ? -1 : 1;
        const zoomFactor = zoomDirection > 0 ? 1.1 : 0.9;
        setZoom(currentZoom * zoomFactor);
    }
}

// Playback control functions
function gotoFirstImage() {
    if (currentFiles.length === 0) return;

    // Clear any pending timer
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }

    stopPlayback();
    currentImageIndex = 0;
    selectFile(currentImageIndex);
    updatePlaybackButtons();
}

function gotoPreviousImage() {
    if (currentFiles.length === 0 || currentImageIndex <= 0) return;

    // Clear any pending timer
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }

    stopPlayback();
    currentImageIndex--;
    selectFile(currentImageIndex);
    updatePlaybackButtons();
}

function gotoNextImage() {
    if (currentFiles.length === 0 || currentImageIndex >= currentFiles.length - 1) return;

    // Clear any pending timer
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }

    stopPlayback();
    currentImageIndex++;
    selectFile(currentImageIndex);
    updatePlaybackButtons();
}

function gotoLastImage() {
    if (currentFiles.length === 0) return;

    // Clear any pending timer
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }

    stopPlayback();
    currentImageIndex = currentFiles.length - 1;
    selectFile(currentImageIndex);
    updatePlaybackButtons();
}

function togglePlayback() {
    if (isPlaying) {
        stopPlayback();
    } else {
        startPlayback();
    }
}

function startPlayback() {
    if (currentFiles.length === 0) return;

    isPlaying = true;
    updatePlayPauseButton();

    // If no image is selected, start from the beginning
    if (currentImageIndex === -1) {
        currentImageIndex = 0;
        selectFile(currentImageIndex);
    } else {
        // If we're already on an image, schedule the next slide after current image is displayed
        scheduleNextSlide();
    }

    updatePlaybackButtons();
}

function scheduleNextSlide() {
    // Clear any existing timer first
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }

    // Only schedule if we're still playing
    if (!isPlaying) return;

    // Schedule the next slide after the specified interval
    playbackTimer = setTimeout(() => {
        if (isPlaying && currentImageIndex < currentFiles.length - 1) {
            currentImageIndex++;
            selectFile(currentImageIndex);
            updatePlaybackButtons();
        } else if (isPlaying) {
            // Reached the end, stop playback
            stopPlayback();
        }
    }, playbackInterval);
}

function stopPlayback() {
    if (playbackTimer) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }
    isPlaying = false;
    updatePlayPauseButton();
    updatePlaybackButtons();
}

function updatePlayPauseButton() {
    if (isPlaying) {
        playPauseBtn.classList.add('playing');
        playPauseBtn.title = 'Pause slideshow';
        playIcon.style.display = 'none';
        pauseIcon.style.display = 'block';
    } else {
        playPauseBtn.classList.remove('playing');
        playPauseBtn.title = 'Play slideshow';
        playIcon.style.display = 'block';
        pauseIcon.style.display = 'none';
    }
}

function updatePlaybackButtons() {
    const hasFiles = currentFiles.length > 0;
    const isFirst = currentImageIndex <= 0;
    const isLast = currentImageIndex >= currentFiles.length - 1;

    // Update button states
    gotoFirstBtn.disabled = !hasFiles || isFirst;
    gotoPreviousBtn.disabled = !hasFiles || isFirst;
    gotoNextBtn.disabled = !hasFiles || isLast;
    gotoLastBtn.disabled = !hasFiles || isLast;
    playPauseBtn.disabled = !hasFiles;

    // Show/hide playback controls based on whether files are loaded
    if (hasFiles) {
        playbackControls.style.display = 'flex';
    } else {
        playbackControls.style.display = 'none';
        // Clear playback state without calling stopPlayback to avoid recursion
        if (playbackTimer) {
            clearTimeout(playbackTimer);
            playbackTimer = null;
        }
        isPlaying = false;
        updatePlayPauseButton();
    }
}

// Clean up watcher when window is closing
window.addEventListener('beforeunload', async () => {
    await stopWatchingDirectory();
});

// Make removeKeyword available globally for onclick handlers
window.removeKeyword = removeKeyword;

// Make checkbox functions available globally for onclick handlers
window.toggleFileSelection = toggleFileSelection;
window.toggleSelectAll = toggleSelectAll;

// Make dialog functions available globally for onclick handlers
window.removeFitsHeader = removeFitsHeader;

// Make selection functions available for potential main process communication
window.getSelectedFiles = getSelectedFiles;
window.clearSelection = clearSelection;