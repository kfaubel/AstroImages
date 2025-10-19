const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  selectDirectory: () => ipcRenderer.invoke('select-directory'),
  readDirectory: (path) => ipcRenderer.invoke('read-directory', path),
  getFilePath: (filePath) => ipcRenderer.invoke('get-file-path', filePath),
  processFitsFile: (filePath) => ipcRenderer.invoke('process-fits-file', filePath),
  checkDirectoryExists: (directoryPath) => ipcRenderer.invoke('check-directory-exists', directoryPath)
});