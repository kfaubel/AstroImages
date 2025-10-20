// Simple test renderer script without ES6 modules
console.log('Test renderer script loaded - immediate execution');

document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM Content Loaded - test script');
    
    // Test basic functionality
    const sidebar = document.getElementById('sidebar');
    const fileList = document.getElementById('file-list');
    
    if (sidebar && fileList) {
        console.log('DOM elements found');
        
        // Add test button to the top of the sidebar instead of replacing file-list content
        const testDiv = document.createElement('div');
        testDiv.style.cssText = 'padding: 10px; background: #f0f0f0; border-bottom: 1px solid #ccc; margin-bottom: 10px;';
        testDiv.innerHTML = `
            <div style="color: green; font-weight: bold;">
                Test Mode Active
                <br>
                <button id="test-folder-btn" style="padding: 8px 16px; margin: 5px 0; background: #007acc; color: white; border: none; border-radius: 4px; cursor: pointer;" onmouseover="this.style.background='#005a9e'" onmouseout="this.style.background='#007acc'" onclick="console.log('Inline click detected')">Test Folder Selection</button>
                <div id="test-results" style="margin-top: 5px; color: blue; font-size: 12px;"></div>
            </div>
        `;
        
        // Insert at the top of the sidebar
        sidebar.insertBefore(testDiv, sidebar.firstChild);
        
        // Add test button functionality
        const testBtn = document.getElementById('test-folder-btn');
        const testResults = document.getElementById('test-results');
        
        if (testBtn && testResults) {
            console.log('Setting up test button event listener');
            
            testBtn.addEventListener('click', async (event) => {
                console.log('Test button clicked!', event);
                testResults.innerHTML = 'Button clicked! Testing folder selection...';
                
                // Check if electronAPI exists and log its contents
                if (window.electronAPI) {
                    console.log('ElectronAPI is available');
                    console.log('ElectronAPI methods:', Object.keys(window.electronAPI));
                    
                    if (window.electronAPI.selectDirectory) {
                        console.log('selectDirectory method found');
                        try {
                            testResults.innerHTML = 'Calling selectDirectory...';
                            const folderPath = await window.electronAPI.selectDirectory();
                            console.log('Selected folder:', folderPath);
                            
                            if (folderPath) {
                                testResults.innerHTML = `Selected: ${folderPath}`;
                                
                                // Try to read directory
                                if (window.electronAPI.readDirectory) {
                                    console.log('Attempting to read directory');
                                    testResults.innerHTML += '<br>Reading directory...';
                                    const files = await window.electronAPI.readDirectory(folderPath);
                                    console.log('Files found:', files);
                                    console.log('Files with details:', JSON.stringify(files, null, 2));
                                    testResults.innerHTML += `<br>Found ${files.length} files`;
                                    
                                    // Show first few files
                                    if (files.length > 0) {
                                        testResults.innerHTML += '<br>Files:';
                                        files.slice(0, 5).forEach(file => {
                                            testResults.innerHTML += `<br>- ${file.name} (${file.isFits ? 'FITS' : 'Regular'})`;
                                        });
                                        if (files.length > 5) {
                                            testResults.innerHTML += `<br>... and ${files.length - 5} more`;
                                        }
                                    } else {
                                        testResults.innerHTML += '<br><strong>No files found! Check console for details.</strong>';
                                    }
                                } else {
                                    testResults.innerHTML += '<br>readDirectory method not found';
                                }
                            } else {
                                testResults.innerHTML = 'No folder selected (user cancelled)';
                            }
                        } catch (error) {
                            console.error('Error testing folder selection:', error);
                            testResults.innerHTML = `Error: ${error.message}`;
                        }
                    } else {
                        console.error('selectDirectory method not found');
                        testResults.innerHTML = 'selectDirectory method not available';
                    }
                } else {
                    console.error('ElectronAPI not available');
                    testResults.innerHTML = 'ElectronAPI not available';
                }
            });
            
            // Test that the button is clickable
            testBtn.style.cursor = 'pointer';
            testBtn.style.backgroundColor = '#007acc';
            testBtn.style.color = 'white';
            testBtn.style.border = 'none';
            testBtn.style.borderRadius = '4px';
            
            console.log('Test button event listener set up successfully');
        } else {
            console.error('Test button or results div not found');
        }
        
    } else {
        console.error('DOM elements not found');
    }
    
    // Test if electronAPI is available
    if (window.electronAPI) {
        console.log('ElectronAPI is available');
        console.log('Available methods:', Object.keys(window.electronAPI));
    } else {
        console.error('ElectronAPI is not available');
    }
});