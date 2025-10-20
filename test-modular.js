/**
 * Simple test to verify modular loading
 */

console.log('Test modular application starting...');

// Simple test without imports first
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM Content Loaded - Test successful');
    
    // Test if basic elements exist
    const fileList = document.getElementById('file-list');
    const imageContainer = document.getElementById('image-container');
    
    if (fileList && imageContainer) {
        console.log('Basic DOM elements found');
        fileList.innerHTML = '<div class="no-files">Modular test: Select a folder to view images</div>';
    } else {
        console.error('Basic DOM elements not found');
    }
    
    // Test IPC communication
    if (window.electronAPI) {
        console.log('Electron API available');
        
        // Test directory selection
        const testButton = document.createElement('button');
        testButton.textContent = 'Test Select Directory';
        testButton.onclick = async () => {
            try {
                const result = await window.electronAPI.selectDirectory();
                console.log('Directory selection result:', result);
            } catch (error) {
                console.error('Directory selection error:', error);
            }
        };
        
        if (fileList) {
            fileList.appendChild(testButton);
        }
    } else {
        console.error('Electron API not available');
    }
});