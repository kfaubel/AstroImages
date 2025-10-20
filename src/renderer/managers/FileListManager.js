/**
 * File List Manager
 * 
 * Handles all file list operations including display, sorting, filtering,
 * and selection management. Provides a clean interface for file list
 * manipulation and maintains consistency across the application.
 */

import appState from '../core/AppState.js';
import domElements from '../core/DOMElements.js';

class FileListManager {
    constructor() {
        this.initialized = false;
    }
    
    /**
     * Initialize the file list manager
     */
    init() {
        if (this.initialized) return;
        
        // Subscribe to state changes
        appState.subscribe('currentFiles', () => this.renderFileList());
        appState.subscribe('selectedFiles', () => this.updateSelectionInfo());
        appState.subscribe('keywords', () => this.renderFileList());
        appState.subscribe('fitsHeaders', () => this.renderFileList());
        appState.subscribe('sortColumn', () => this.renderFileList());
        appState.subscribe('sortDirection', () => this.renderFileList());
        appState.subscribe('currentImageIndex', () => this.renderFileList()); // Re-render when active image changes
        
        // Listen for window resize - but no automatic sidebar adjustment
        window.addEventListener('resize', () => {
            // Resize timeout cleared but no automatic adjustment
            clearTimeout(this.resizeTimeout);
        });
        
        this.initialized = true;
        console.log('FileListManager initialized');
    }
    
    /**
     * Load directory and populate file list
     */
    async loadDirectory(directoryPath) {
        console.log('FileListManager: Loading directory:', directoryPath);
        
        try {
            if (!window.electronAPI) {
                throw new Error('ElectronAPI not available');
            }
            
            // Load files from the directory using the correct IPC call
            const files = await window.electronAPI.readDirectory(directoryPath);
            console.log('FileListManager: Loaded', files.length, 'files');
            
            // Update app state with the new files
            appState.set('currentFiles', files);
            appState.set('currentDirectory', directoryPath);
            
            // Clear current selection
            appState.set('selectedFiles', new Set());
            appState.set('currentImageIndex', -1);
            
            // Start watching the directory for changes
            if (window.electronAPI.startWatchingDirectory) {
                await window.electronAPI.startWatchingDirectory(directoryPath);
                appState.set('isWatchingDirectory', true);
            }
            
            console.log('FileListManager: Directory loaded successfully');
            
        } catch (error) {
            console.error('FileListManager: Error loading directory:', error);
            
            // Clear files on error
            appState.set('currentFiles', []);
            appState.set('selectedFiles', new Set());
            appState.set('currentImageIndex', -1);
            
            throw error;
        }
    }
    
    /**
     * Render the complete file list table
     */
    renderFileList() {
        const fileListDiv = domElements.get('fileListDiv');
        if (!fileListDiv) return;
        
        const files = appState.get('currentFiles');
        const keywords = appState.get('keywords');
        const fitsHeaders = appState.get('fitsHeaders');
        
        // Show/hide playback controls based on file availability
        const playbackControls = domElements.get('playbackControls');
        
        if (!files || files.length === 0) {
            fileListDiv.innerHTML = '<div class="no-files">No image files found in this directory</div>';
            // Hide playback controls when no files
            if (playbackControls) {
                playbackControls.style.display = 'none';
            }
            // Reset sidebar to minimum width when no files
            this.resetSidebarWidth();
            return;
        }
        
        // Show playback controls when files are available
        if (playbackControls) {
            playbackControls.style.display = 'flex';
        }
        
        try {
            // Create table structure
            const table = this.createTableStructure(keywords, fitsHeaders);
            
            // Add file rows
            files.forEach((file, index) => {
                const row = this.createFileRow(file, index, keywords, fitsHeaders);
                table.appendChild(row);
            });
            
            // Replace content
            fileListDiv.innerHTML = '';
            fileListDiv.appendChild(table);
            
            // Update selection info
            this.updateSelectionInfo();
            
        } catch (error) {
            console.error('Error rendering file list:', error);
            fileListDiv.innerHTML = '<div class="error">Error displaying file list</div>';
        }
    }
    
    /**
     * Create the table header structure
     * @param {Array} keywords - Custom keywords
     * @param {Array} fitsHeaders - FITS headers to display
     * @returns {HTMLTableElement} Table element with header
     */
    createTableStructure(keywords, fitsHeaders) {
        const table = document.createElement('table');
        table.className = 'file-table';
        table.style.tableLayout = 'auto';
        
        // Create header
        const thead = document.createElement('thead');
        const headerRow = document.createElement('tr');
        
        // Checkbox column
        const checkboxHeader = document.createElement('th');
        checkboxHeader.className = 'file-header-cell file-header-checkbox';
        checkboxHeader.innerHTML = `
            <input type="checkbox" id="select-all-checkbox" onchange="toggleAllFileSelection(event)">
        `;
        headerRow.appendChild(checkboxHeader);
        
        // Filename column
        const filenameHeader = document.createElement('th');
        filenameHeader.className = 'file-header-cell file-header-filename sortable';
        filenameHeader.setAttribute('data-sort-column', 'filename');
        filenameHeader.textContent = 'Filename';
        filenameHeader.onclick = () => this.handleSort('filename');
        headerRow.appendChild(filenameHeader);
        
        // Keyword columns
        keywords.forEach(keyword => {
            const header = document.createElement('th');
            header.className = 'file-header-cell file-header-keyword sortable';
            header.setAttribute('data-sort-column', `keyword-${keyword}`);
            header.textContent = keyword;
            header.onclick = () => this.handleSort(`keyword-${keyword}`);
            headerRow.appendChild(header);
        });
        
        // FITS header columns
        fitsHeaders.forEach(header => {
            const headerElement = document.createElement('th');
            headerElement.className = 'file-header-cell file-header-fits sortable';
            headerElement.setAttribute('data-sort-column', `fits-${header}`);
            headerElement.setAttribute('title', `FITS Header: ${header}`);
            headerElement.textContent = header;
            headerElement.onclick = () => this.handleSort(`fits-${header}`);
            headerRow.appendChild(headerElement);
        });
        
        thead.appendChild(headerRow);
        table.appendChild(thead);
        
        // Create tbody
        const tbody = document.createElement('tbody');
        table.appendChild(tbody);
        
        return table;
    }
    
    /**
     * Create a file row
     * @param {Object} file - File object
     * @param {number} index - File index
     * @param {Array} keywords - Custom keywords
     * @param {Array} fitsHeaders - FITS headers
     * @returns {HTMLTableRowElement} Table row element
     */
    createFileRow(file, index, keywords, fitsHeaders) {
        const row = document.createElement('tr');
        row.className = 'file-row';
        row.onclick = (e) => this.handleFileClick(index, e);
        
        const selectedFiles = appState.get('selectedFiles');
        const currentImageIndex = appState.get('currentImageIndex');
        
        // Add selection highlighting for checkbox selection
        if (selectedFiles.has(index)) {
            row.classList.add('selected');
        }
        
        // Add active highlighting for currently viewed image
        if (currentImageIndex === index) {
            row.classList.add('active');
        }
        
        // Parse filename for keywords
        const parsed = this.parseFilename(file.name, keywords);
        
        // If this is a FITS file and we haven't loaded headers yet, load them
        if (file.isFits && fitsHeaders.length > 0 && !file.fitsHeaders) {
            this.loadFitsHeaders(file, index);
        }
        
        // Checkbox cell
        const checkboxCell = document.createElement('td');
        checkboxCell.className = 'file-cell file-cell-checkbox';
        checkboxCell.innerHTML = `
            <input type="checkbox" class="file-checkbox" 
                   ${selectedFiles.has(index) ? 'checked' : ''} 
                   onchange="toggleFileSelection(${index}, event)">
        `;
        row.appendChild(checkboxCell);
        
        // Filename cell
        const filenameCell = document.createElement('td');
        filenameCell.className = 'file-cell file-cell-filename';
        filenameCell.innerHTML = `<span class="file-name file-name-truncated">${file.name}</span>`;
        row.appendChild(filenameCell);
        
        // Keyword cells
        keywords.forEach(keyword => {
            const cell = document.createElement('td');
            cell.className = 'file-cell file-cell-keyword';
            const value = parsed[keyword] || '';
            
            if (value && value.length > 8) {
                cell.innerHTML = `
                    <div class="tooltip">
                        <span class="keyword-value-truncated">${value}</span>
                        <div class="tooltip-text">${keyword}: ${value}</div>
                    </div>
                `;
            } else {
                cell.innerHTML = `<span class="keyword-value">${value}</span>`;
            }
            
            row.appendChild(cell);
        });
        
        // FITS header cells
        fitsHeaders.forEach(header => {
            const cell = document.createElement('td');
            cell.className = 'file-cell file-cell-fits';
            cell.setAttribute('data-header', header);
            cell.setAttribute('data-file-index', index);
            
            // Show loading indicator for FITS files, empty for non-FITS files
            if (file.isFits && !file.fitsHeaders) {
                cell.innerHTML = `<span class="fits-loading">Loading...</span>`;
            } else {
                const value = file.fitsHeaders?.[header] || '';
                
                if (value && value.toString().length > 8) {
                    cell.innerHTML = `
                        <div class="tooltip">
                            <span class="fits-value-truncated">${value}</span>
                            <div class="tooltip-text">${header}: ${value}</div>
                        </div>
                    `;
                } else {
                    cell.innerHTML = `<span class="fits-value">${value}</span>`;
                }
            }
            
            row.appendChild(cell);
        });
        
        return row;
    }
    
    /**
     * Parse filename for keyword values
     * @param {string} filename - Filename to parse
     * @param {Array} keywords - Keywords to extract
     * @returns {Object} Parsed keyword values
     */
    parseFilename(filename, keywords) {
        const parsed = {};
        
        keywords.forEach(keyword => {
            const regex = new RegExp(`${keyword}_([^_]+)`, 'i');
            const match = filename.match(regex);
            if (match) {
                parsed[keyword] = match[1];
            }
        });
        
        return parsed;
    }
    
    /**
     * Load FITS headers for a file
     * @param {Object} file - File object to load headers for
     * @param {number} index - File index in the files array
     */
    async loadFitsHeaders(file, index) {
        try {
            if (!window.electronAPI || !window.electronAPI.getFitsHeaders) {
                console.warn('ElectronAPI getFitsHeaders not available');
                return;
            }
            
            console.log('Loading FITS headers for:', file.name);
            const result = await window.electronAPI.getFitsHeaders(file.path);
            
            if (result.success) {
                // Update the file object with the headers
                file.fitsHeaders = result.headers;
                
                // Update the files array in app state
                const files = appState.get('currentFiles');
                if (files && files[index]) {
                    files[index].fitsHeaders = result.headers;
                    appState.set('currentFiles', files);
                }
                
                // Update the display for this file's FITS header cells
                this.updateFitsHeaderCells(index, result.headers);
                
                console.log(`Loaded FITS headers for ${file.name}:`, Object.keys(result.headers));
            } else {
                console.error(`Failed to load FITS headers for ${file.name}:`, result.error);
                this.updateFitsHeaderCells(index, {}, true);
            }
        } catch (error) {
            console.error(`Error loading FITS headers for ${file.name}:`, error);
            this.updateFitsHeaderCells(index, {}, true);
        }
    }
    
    /**
     * Update FITS header cells for a specific file
     * @param {number} fileIndex - File index
     * @param {Object} headers - FITS headers object
     * @param {boolean} isError - Whether this is an error state
     */
    updateFitsHeaderCells(fileIndex, headers, isError = false) {
        const fitsHeaders = appState.get('fitsHeaders') || [];
        
        fitsHeaders.forEach(header => {
            const cell = document.querySelector(`[data-header="${header}"][data-file-index="${fileIndex}"]`);
            if (cell) {
                if (isError) {
                    cell.innerHTML = `<span class="fits-error">Error</span>`;
                } else {
                    const value = headers[header] || '';
                    
                    if (value && value.toString().length > 8) {
                        cell.innerHTML = `
                            <div class="tooltip">
                                <span class="fits-value-truncated">${value}</span>
                                <div class="tooltip-text">${header}: ${value}</div>
                            </div>
                        `;
                    } else {
                        cell.innerHTML = `<span class="fits-value">${value}</span>`;
                    }
                }
            }
        });
        
        // Clear timeout (no longer adjusting width automatically)
        clearTimeout(this.fitsUpdateTimeout);
    }
    
    /**
     * Handle file click
     * @param {number} index - File index
     * @param {Event} event - Click event
     */
    handleFileClick(index, event) {
        // Prevent checkbox clicks from triggering row click
        if (event.target.type === 'checkbox') return;
        
        appState.set('currentImageIndex', index);
        
        // Trigger image display
        const files = appState.get('currentFiles');
        if (files && files[index]) {
            window.dispatchEvent(new CustomEvent('displayImage', { 
                detail: { file: files[index], index } 
            }));
        }
    }
    
    /**
     * Handle column sorting
     * @param {string} column - Column identifier
     */
    handleSort(column) {
        const currentSortColumn = appState.get('sortColumn');
        const currentSortDirection = appState.get('sortDirection');
        
        let newDirection = 'asc';
        if (currentSortColumn === column && currentSortDirection === 'asc') {
            newDirection = 'desc';
        }
        
        appState.update({
            sortColumn: column,
            sortDirection: newDirection
        });
        
        this.sortFiles(column, newDirection);
    }
    
    /**
     * Sort files by column
     * @param {string} column - Column to sort by
     * @param {string} direction - Sort direction ('asc' or 'desc')
     */
    sortFiles(column, direction) {
        const files = [...appState.get('currentFiles')];
        const keywords = appState.get('keywords');
        
        files.sort((a, b) => {
            let valueA, valueB;
            
            if (column === 'filename') {
                valueA = a.name.toLowerCase();
                valueB = b.name.toLowerCase();
            } else if (column.startsWith('keyword-')) {
                const keyword = column.replace('keyword-', '');
                const parsedA = this.parseFilename(a.name, keywords);
                const parsedB = this.parseFilename(b.name, keywords);
                valueA = parsedA[keyword] || '';
                valueB = parsedB[keyword] || '';
                
                // Try to parse as numbers for proper numeric sorting
                const numA = parseFloat(valueA);
                const numB = parseFloat(valueB);
                if (!isNaN(numA) && !isNaN(numB)) {
                    return direction === 'asc' ? numA - numB : numB - numA;
                }
                valueA = valueA.toLowerCase();
                valueB = valueB.toLowerCase();
            } else if (column.startsWith('fits-')) {
                const header = column.replace('fits-', '');
                valueA = (a.fitsHeaders?.[header] || '').toString().toLowerCase();
                valueB = (b.fitsHeaders?.[header] || '').toString().toLowerCase();
            }
            
            if (valueA < valueB) return direction === 'asc' ? -1 : 1;
            if (valueA > valueB) return direction === 'asc' ? 1 : -1;
            return 0;
        });
        
        appState.set('currentFiles', files);
    }
    
    /**
     * Update selection information display
     */
    updateSelectionInfo() {
        const selectedFiles = appState.get('selectedFiles');
        const totalFiles = appState.get('currentFiles').length;
        
        // Update move button state
        const moveBtn = domElements.get('moveSelectedBtn');
        if (moveBtn) {
            moveBtn.disabled = selectedFiles.size === 0;
        }
        
        // Update select all checkbox
        const selectAllCheckbox = document.getElementById('select-all-checkbox');
        if (selectAllCheckbox) {
            if (selectedFiles.size === 0) {
                selectAllCheckbox.indeterminate = false;
                selectAllCheckbox.checked = false;
            } else if (selectedFiles.size === totalFiles) {
                selectAllCheckbox.indeterminate = false;
                selectAllCheckbox.checked = true;
            } else {
                selectAllCheckbox.indeterminate = true;
            }
        }
        
        console.log(`Selection updated: ${selectedFiles.size}/${totalFiles} files selected`);
    }
    
    /**
     * Toggle file selection
     * @param {number} index - File index
     * @param {Event} event - Checkbox change event
     */
    toggleFileSelection(index, event) {
        event.stopPropagation();
        
        const selectedFiles = new Set(appState.get('selectedFiles'));
        
        if (selectedFiles.has(index)) {
            selectedFiles.delete(index);
        } else {
            selectedFiles.add(index);
        }
        
        appState.set('selectedFiles', selectedFiles);
    }
    
    /**
     * Toggle all files selection
     * @param {Event} event - Checkbox change event
     */
    toggleAllFileSelection(event) {
        const selectedFiles = new Set();
        const totalFiles = appState.get('currentFiles').length;
        
        if (event.target.checked) {
            // Select all files
            for (let i = 0; i < totalFiles; i++) {
                selectedFiles.add(i);
            }
        }
        // If unchecked, selectedFiles remains empty (deselect all)
        
        appState.set('selectedFiles', selectedFiles);
    }
    
    /**
     * Refresh the file list display
     * This method can be called externally to force a re-render
     */
    refreshDisplay() {
        console.log('FileListManager: Refreshing display...');
        this.renderFileList();
    }
    
    /**
     * Automatically adjust sidebar width to fit table content without horizontal scrollbar
     * Respects the maximum width constraint of 80vw
     * NOTE: This method is disabled - sidebar width is now only manually adjustable
     */
    adjustSidebarWidth() {
        // Method disabled to prevent automatic width changes
        // Sidebar width is now only adjustable via manual resize handle
        console.log('adjustSidebarWidth called but disabled - manual resize only');
        return;
        
        /* DISABLED CODE:
        try {
            const sidebar = domElements.get('sidebar');
            const fileListDiv = domElements.get('fileListDiv');
            const table = fileListDiv?.querySelector('.file-table');
            
            if (!sidebar || !table) {
                console.warn('Cannot adjust sidebar width: missing elements');
                return;
            }
            
            // Force a layout to get accurate measurements
            table.style.width = 'auto';
            
            // Create a temporary container to measure the natural table width
            const tempContainer = document.createElement('div');
            tempContainer.style.position = 'absolute';
            tempContainer.style.visibility = 'hidden';
            tempContainer.style.whiteSpace = 'nowrap';
            tempContainer.style.width = 'auto';
            tempContainer.style.left = '-9999px';
            
            const tempTable = table.cloneNode(true);
            tempTable.style.width = 'auto';
            tempTable.style.tableLayout = 'auto';
            tempContainer.appendChild(tempTable);
            document.body.appendChild(tempContainer);
            
            // Measure the natural width needed
            const naturalTableWidth = tempTable.offsetWidth;
            document.body.removeChild(tempContainer);
            
            // Add padding for sidebar internal spacing (left/right padding, borders, etc.)
            const sidebarPadding = 40; // Approximate padding and border space
            const requiredWidth = naturalTableWidth + sidebarPadding;
            
            // Calculate maximum allowed width (80% of viewport)
            const maxAllowedWidth = window.innerWidth * 0.8;
            const minWidth = 250; // Minimum sidebar width
            
            // Determine the target width
            let targetWidth;
            if (requiredWidth <= minWidth) {
                targetWidth = minWidth;
            } else if (requiredWidth >= maxAllowedWidth) {
                targetWidth = maxAllowedWidth;
            } else {
                targetWidth = requiredWidth;
            }
            
            // Apply the width
            sidebar.style.width = `${targetWidth}px`;
            sidebar.style.minWidth = `${Math.min(targetWidth, minWidth)}px`;
            sidebar.style.maxWidth = '80vw'; // Keep the CSS max-width as fallback
            
            // Reset table width to fill the sidebar
            table.style.width = '100%';
            
            console.log(`Sidebar width adjusted: required=${requiredWidth}px, target=${targetWidth}px, max=${maxAllowedWidth}px`);
            
        } catch (error) {
            console.error('Error adjusting sidebar width:', error);
        }
        */
    }

    /**
     * Reset sidebar to minimum width when no files are present
     * NOTE: This method is disabled - sidebar width is now only manually adjustable
     */
    resetSidebarWidth() {
        // Method disabled to prevent automatic width changes
        console.log('resetSidebarWidth called but disabled - manual resize only');
        return;
        
        /* DISABLED CODE:
        try {
            const sidebar = domElements.get('sidebar');
            if (sidebar) {
                sidebar.style.width = '250px';
                sidebar.style.minWidth = '250px';
                console.log('Sidebar width reset to minimum');
            }
        } catch (error) {
            console.error('Error resetting sidebar width:', error);
        }
        */
    }
}

// Export singleton instance
const fileListManager = new FileListManager();
export default fileListManager;