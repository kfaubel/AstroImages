let currentDirectory = null;
let currentFiles = [];
let keywords = []; // Array of keywords for filename parsing
let sortColumn = null; // Current sort column
let sortDirection = 'asc'; // 'asc' or 'desc'

// DOM elements
const selectFolderBtn = document.getElementById('select-folder');
const configureKeywordsBtn = document.getElementById('configure-keywords');
const currentFolderDiv = document.getElementById('current-folder');
const fileListDiv = document.getElementById('file-list');
const imageContainer = document.getElementById('image-container');
const imageDisplay = document.getElementById('image-display');
const imageInfo = document.getElementById('image-info');
const imageName = document.getElementById('image-name');
const imageSize = document.getElementById('image-size');
const noImageDiv = document.querySelector('.no-image');

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

// Event listeners
selectFolderBtn.addEventListener('click', selectFolder);
configureKeywordsBtn.addEventListener('click', openKeywordDialog);
closeDialogBtn.addEventListener('click', closeKeywordDialog);
addKeywordBtn.addEventListener('click', addKeyword);
saveKeywordsBtn.addEventListener('click', saveKeywords);
cancelKeywordsBtn.addEventListener('click', closeKeywordDialog);
testFilename.addEventListener('input', updateTestResults);
keywordInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') addKeyword();
});

// Click outside dialog to close
keywordDialog.addEventListener('click', (e) => {
    if (e.target === keywordDialog) closeKeywordDialog();
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
    renderFileList();
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
    
    for (let i = 0; i < tokens.length - 1; i++) {
        const token = tokens[i].toLowerCase();
        if (keywords.includes(token)) {
            parsed[token] = tokens[i + 1];
        }
    }
    
    return parsed;
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
    const keyword = keywordInput.value.trim().toLowerCase();
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
    if (saved) {
        keywords = JSON.parse(saved);
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
        try {
            // Show loading state
            currentFolderDiv.textContent = 'Restoring last directory...';
            fileListDiv.innerHTML = '<div class="no-files">Loading files...</div>';
            
            // Check if directory still exists
            const dirExists = await window.electronAPI.checkDirectoryExists(lastDir);
            if (!dirExists) {
                throw new Error('Directory no longer exists');
            }
            
            // Load the directory
            currentDirectory = lastDir;
            await loadFiles();
            currentFolderDiv.textContent = lastDir;
        } catch (error) {
            console.log('Last directory no longer accessible:', error);
            // Clear the saved directory if it's no longer accessible
            localStorage.removeItem('lastDirectory');
            currentDirectory = null;
            currentFolderDiv.textContent = 'No folder selected';
            fileListDiv.innerHTML = '<div class="no-files">Select a folder to view images</div>';
        }
    }
}

async function selectFolder() {
    try {
        const folderPath = await window.electronAPI.selectDirectory();
        if (folderPath) {
            currentDirectory = folderPath;
            currentFolderDiv.textContent = folderPath;
            saveLastDirectory(folderPath); // Save the selected directory
            await loadFiles();
        }
    } catch (error) {
        console.error('Error selecting folder:', error);
    }
}

async function loadFiles() {
    try {
        currentFiles = await window.electronAPI.readDirectory(currentDirectory);
        renderFileList();
    } catch (error) {
        console.error('Error loading files:', error);
        renderEmptyFileList();
    }
}

function renderFileList() {
    if (currentFiles.length === 0) {
        renderEmptyFileList();
        return;
    }

    // Create table structure
    let html = '<div class="file-list-table">';
    
    // Create header
    html += '<div class="file-header">';
    html += '<div class="file-header-cell file-header-filename sortable" data-sort-column="filename">Filename</div>';
    
    // Add keyword columns
    keywords.forEach(keyword => {
        html += `<div class="file-header-cell file-header-keyword sortable" data-sort-column="${keyword}">${keyword.charAt(0).toUpperCase() + keyword.slice(1)}</div>`;
    });
    
    html += '</div>';
    
    // Create file rows
    currentFiles.forEach((file, index) => {
        const parsed = parseFilename(file.name);
        
        html += `<div class="file-row" data-index="${index}">`;
        
        // Filename cell
        const iconPath = file.isFits 
            ? "M12 2C13.1 2 14 2.9 14 4V6C14 7.1 13.1 8 12 8S10 7.1 10 6V4C10 2.9 10.9 2 12 2M21 9V7L15 13L13 11V16H18L16 14L22 8V6L21 9M9 11C7.9 11 7 11.9 7 13S7.9 15 9 15 11 14.1 11 13 10.1 11 9 11M2.5 19L6.5 15L10.5 18L11.5 17L7.5 13L1.5 19L2.5 19Z"
            : "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z";
        
        html += `<div class="file-cell file-cell-filename">`;
        html += `<svg class="file-icon ${file.isFits ? 'fits-icon' : ''}" viewBox="0 0 24 24" fill="currentColor">`;
        html += `<path d="${iconPath}"/>`;
        html += `</svg>`;
        html += `<div class="tooltip">`;
        html += `<span class="file-name file-name-truncated">${file.name}</span>`;
        html += `<div class="tooltip-text">${file.name}</div>`;
        html += `</div>`;
        if (file.isFits) {
            html += `<span class="file-type-badge">FITS</span>`;
        }
        html += `</div>`;
        
        // Keyword value cells
        keywords.forEach(keyword => {
            const value = parsed[keyword] || '';
            html += `<div class="file-cell file-cell-keyword">`;
            if (value) {
                if (value.length > 8) {
                    // Add tooltip for long values
                    html += `<div class="tooltip">`;
                    html += `<span class="keyword-value" style="max-width: 100%; overflow: hidden; text-overflow: ellipsis;">${value}</span>`;
                    html += `<div class="tooltip-text">${keyword}: ${value}</div>`;
                    html += `</div>`;
                } else {
                    html += `<span class="keyword-value">${value}</span>`;
                }
            }
            html += `</div>`;
        });
        
        html += '</div>';
    });
    
    html += '</div>';
    
    fileListDiv.innerHTML = html;
    
    // Add click listeners to file rows
    document.querySelectorAll('.file-row').forEach(row => {
        row.addEventListener('click', () => {
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
    
    // Update sort indicators
    updateSortIndicators();
}

function renderEmptyFileList() {
    fileListDiv.innerHTML = '<div class="no-files">No image files found in this folder</div>';
    hideImage();
}

async function selectFile(index) {
    // Remove previous selection
    document.querySelectorAll('.file-row').forEach(item => {
        item.classList.remove('selected');
    });
    
    // Add selection to clicked item
    const selectedItem = document.querySelector(`[data-index="${index}"]`);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }
    
    const file = currentFiles[index];
    await displayImage(file);
}

async function displayImage(file) {
    try {
        // Hide no-image placeholder
        noImageDiv.style.display = 'none';
        
        // Show loading state
        imageDisplay.style.display = 'block';
        imageDisplay.src = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNTAiIGhlaWdodD0iNTAiIHZpZXdCb3g9IjAgMCA1MCA1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjUiIGN5PSIyNSIgcj0iMjAiIGZpbGw9Im5vbmUiIHN0cm9rZT0iIzk5OSIgc3Ryb2tlLXdpZHRoPSI0IiBzdHJva2UtZGFzaGFycmF5PSI4MCA4MCI+CjxhbmltYXRlVHJhbnNmb3JtIGF0dHJpYnV0ZU5hbWU9InRyYW5zZm9ybSIgdHlwZT0icm90YXRlIiB2YWx1ZXM9IjAgMjUgMjU7MzYwIDI1IDI1IiBkdXI9IjFzIiByZXBlYXRDb3VudD0iaW5kZWZpbml0ZSIvPgo8L2NpcmNsZT4KPC9zdmc+';
        
        let imageSrc;
        
        if (file.isFits) {
            // Process FITS file
            console.log('Processing FITS file:', file.path);
            try {
                imageSrc = await window.electronAPI.processFitsFile(file.path);
                console.log('FITS processing completed successfully');
            } catch (error) {
                console.error('FITS processing failed:', error);
                throw error;
            }
        } else {
            // Regular image file
            imageSrc = await window.electronAPI.getFilePath(file.path);
        }
        
        // Load the processed image
        imageDisplay.src = imageSrc;
        
        // Show image info
        imageName.textContent = file.name;
        
        // Get file size - this won't work in renderer process, so we'll estimate
        imageSize.textContent = file.isFits ? 'FITS Image' : '';
        
        imageInfo.style.display = 'flex';
        
        // Handle image load error
        imageDisplay.onerror = () => {
            hideImage();
            console.error('Failed to load image:', file.path);
        };
        
    } catch (error) {
        console.error('Error displaying image:', error);
        hideImage();
    }
}

function hideImage() {
    imageDisplay.style.display = 'none';
    imageInfo.style.display = 'none';
    noImageDiv.style.display = 'block';
}

// Keyboard navigation
document.addEventListener('keydown', (event) => {
    if (currentFiles.length === 0) return;
    
    const selectedItem = document.querySelector('.file-row.selected');
    let currentIndex = selectedItem ? parseInt(selectedItem.dataset.index) : -1;
    
    switch (event.key) {
        case 'ArrowUp':
            event.preventDefault();
            if (currentIndex > 0) {
                selectFile(currentIndex - 1);
            }
            break;
        case 'ArrowDown':
            event.preventDefault();
            if (currentIndex < currentFiles.length - 1) {
                selectFile(currentIndex + 1);
            }
            break;
    }
});

// Initialize - load saved keywords, sidebar width, and last directory
document.addEventListener('DOMContentLoaded', async () => {
    loadKeywords();
    loadSidebarWidth();
    await loadLastDirectoryOnStartup();
    console.log('Image Viewer loaded with keywords:', keywords);
    if (currentDirectory) {
        console.log('Restored last directory:', currentDirectory);
    }
});

// Make removeKeyword available globally for onclick handlers
window.removeKeyword = removeKeyword;