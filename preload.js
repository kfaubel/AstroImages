/**
 * AstroImages - Preload Script
 * 
 * This script runs in the renderer process but has access to Node.js APIs.
 * It provides a secure bridge between the main process and renderer process,
 * exposing only specific IPC functions needed by the UI.
 * 
 * Security: Uses contextBridge to safely expose APIs without giving
 * the renderer process full access to Node.js or Electron APIs.
 */

const { contextBridge, ipcRenderer } = require('electron');

// Expose secure API to the renderer process through window.electronAPI
contextBridge.exposeInMainWorld('electronAPI', {
    // ===== FILE SYSTEM OPERATIONS =====
    selectDirectory: () => ipcRenderer.invoke('select-directory'),
    readDirectory: (path) => ipcRenderer.invoke('read-directory', path),
    getFilePath: (filePath) => ipcRenderer.invoke('get-file-path', filePath),
    checkDirectoryExists: (directoryPath) => ipcRenderer.invoke('check-directory-exists', directoryPath),

    // ===== IMAGE PROCESSING =====
    processFitsFile: (filePath) => ipcRenderer.invoke('process-fits-file', filePath),
    processFitsFileStretched: (filePath, applyStretch) => ipcRenderer.invoke('process-fits-file-stretched', filePath, applyStretch),
    getFitsThumbnail: (filePath, applyStretch) => ipcRenderer.invoke('get-fits-thumbnail', filePath, applyStretch),

    // ===== FILE MANAGEMENT =====
    selectMoveDestination: (defaultPath) => ipcRenderer.invoke('select-move-destination', defaultPath),
    moveFiles: (filePaths, destinationPath) => ipcRenderer.invoke('move-files', filePaths, destinationPath),
    moveFilesToTrash: (filePaths) => ipcRenderer.invoke('move-files-to-trash', filePaths),
    updateMenuState: (hasSelection) => ipcRenderer.send('update-menu-state', hasSelection),

    // ===== DIRECTORY MONITORING =====
    startWatchingDirectory: (directoryPath) => ipcRenderer.invoke('start-watching-directory', directoryPath),
    stopWatchingDirectory: () => ipcRenderer.invoke('stop-watching-directory'),

    // ===== FITS METADATA =====
    getFitsHeaders: (filePath) => ipcRenderer.invoke('get-fits-headers', filePath),

    // ===== UI MANAGEMENT =====
    updateWindowTitle: (directoryPath) => ipcRenderer.invoke('update-window-title', directoryPath),

    // ===== EVENT LISTENERS =====
    // Register callbacks for events from the main process
    onFolderSelected: (callback) => {
        ipcRenderer.on('folder-selected', (event, folderPath) => callback(folderPath));
    },

    onShowMoveDialog: (callback) => {
        ipcRenderer.on('show-move-dialog', () => callback());
    },

    onShowKeywordsDialog: (callback) => {
        ipcRenderer.on('show-keywords-dialog', () => callback());
    },

    onShowFitsHeadersDialog: (callback) => {
        ipcRenderer.on('show-fits-headers-dialog', () => callback());
    },

    // Zoom events
    onZoomIn: (callback) => {
        ipcRenderer.on('zoom-in', () => callback());
    },

    onZoomOut: (callback) => {
        ipcRenderer.on('zoom-out', () => callback());
    },

    onZoomActual: (callback) => {
        ipcRenderer.on('zoom-actual', () => callback());
    },

    onZoomFit: (callback) => {
        ipcRenderer.on('zoom-fit', () => callback());
    },

    // Directory change events
    onDirectoryChanged: (callback) => {
        ipcRenderer.on('directory-changed', (event, changeInfo) => callback(changeInfo));
    },

    onWatcherError: (callback) => {
        ipcRenderer.on('watcher-error', (event, error) => callback(error));
    },

    // Remove listener
    removeAllListeners: (channel) => {
        ipcRenderer.removeAllListeners(channel);
    }
});