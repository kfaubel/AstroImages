/**
 * DOM Element Manager
 * 
 * Centralized management of DOM elements and event listeners.
 * Provides safe access to DOM elements with error handling and
 * automatic initialization.
 */

class DOMElements {
    constructor() {
        this.elements = new Map();
        this.initialized = false;
    }
    
    /**
     * Initialize all DOM elements
     * Should be called after DOM is loaded
     */
    init() {
        if (this.initialized) return;
        
        try {
            // Main layout elements
            this.register('fileListDiv', 'file-list');
            this.register('imageContainer', 'image-container');
            this.register('imageDisplay', 'image-display');
            this.register('noImageDiv', '.no-image', true); // querySelector
            this.register('sidebar', 'sidebar');
            this.register('mainContent', 'main-content');
            this.register('resizeHandle', 'resize-handle');
            
            // Playback control elements
            this.register('playbackControls', 'playback-controls');
            this.register('moveSelectedBtn', 'move-selected-btn');
            this.register('gotoFirstBtn', 'goto-first');
            this.register('gotoPreviousBtn', 'goto-previous');
            this.register('playPauseBtn', 'play-pause');
            this.register('gotoNextBtn', 'goto-next');
            this.register('gotoLastBtn', 'goto-last');
            
            // Playback button icons
            this.registerSubElement('playIcon', 'playPauseBtn', '.play-icon');
            this.registerSubElement('pauseIcon', 'playPauseBtn', '.pause-icon');
            
            // Zoom control elements
            this.register('zoomControls', 'zoom-controls');
            this.register('zoomInBtn', 'zoom-in');
            this.register('zoomOutBtn', 'zoom-out');
            this.register('zoomActualBtn', 'zoom-actual');
            this.register('zoomFitBtn', 'zoom-fit');
            this.register('zoomLevel', 'zoom-level');
            this.register('autoStretchCheckbox', 'auto-stretch');
            
            // Loading overlay
            this.register('loadingOverlay', 'loading-overlay');
            this.register('loadingText', 'loading-text');
            
            // Keyword dialog elements
            this.register('keywordDialog', 'keyword-dialog');
            this.register('closeDialogBtn', 'close-dialog');
            this.register('keywordInput', 'keyword-input');
            this.register('addKeywordBtn', 'add-keyword');
            this.register('keywordsContainer', 'keywords-container');
            this.register('testFilename', 'test-filename');
            this.register('testResults', 'test-results');
            this.register('saveKeywordsBtn', 'save-keywords');
            this.register('cancelKeywordsBtn', 'cancel-keywords');
            
            // Move dialog elements
            this.register('moveDialog', 'move-dialog');
            this.register('closeMoveDialogBtn', 'close-move-dialog');
            this.register('moveFileCount', 'move-file-count');
            this.register('destinationPath', 'destination-path');
            this.register('browseDestinationBtn', 'browse-destination');
            this.register('executeMoveBtn', 'execute-move');
            this.register('cancelMoveBtn', 'cancel-move');
            
            // FITS headers dialog elements
            this.register('fitsHeadersDialog', 'fits-headers-dialog');
            this.register('closeFitsHeadersDialogBtn', 'close-fits-headers-dialog');
            this.register('commonFitsHeaders', 'common-fits-headers');
            this.register('customHeaderInput', 'custom-header-input');
            this.register('addCustomHeaderBtn', 'add-custom-header');
            this.register('selectedFitsHeaders', 'selected-fits-headers');
            this.register('saveFitsHeadersBtn', 'save-fits-headers');
            this.register('cancelFitsHeadersBtn', 'cancel-fits-headers');
            
            this.initialized = true;
            console.log('DOM elements initialized successfully');
            
        } catch (error) {
            console.error('Failed to initialize DOM elements:', error);
            throw error;
        }
    }
    
    /**
     * Register a DOM element
     * @param {string} name - Internal name for the element
     * @param {string} selector - CSS selector or element ID
     * @param {boolean} useQuerySelector - Whether to use querySelector instead of getElementById
     */
    register(name, selector, useQuerySelector = false) {
        try {
            const element = useQuerySelector 
                ? document.querySelector(selector)
                : document.getElementById(selector);
                
            if (!element) {
                console.warn(`DOM element not found: ${selector}`);
                return;
            }
            
            this.elements.set(name, element);
        } catch (error) {
            console.error(`Error registering DOM element ${name}:`, error);
        }
    }
    
    /**
     * Register a sub-element (child element of an already registered element)
     * @param {string} name - Internal name for the sub-element
     * @param {string} parentName - Name of the parent element
     * @param {string} selector - CSS selector for the sub-element
     */
    registerSubElement(name, parentName, selector) {
        try {
            const parent = this.get(parentName);
            if (!parent) {
                console.warn(`Parent element not found: ${parentName}`);
                return;
            }
            
            const element = parent.querySelector(selector);
            if (!element) {
                console.warn(`Sub-element not found: ${selector} in ${parentName}`);
                return;
            }
            
            this.elements.set(name, element);
        } catch (error) {
            console.error(`Error registering sub-element ${name}:`, error);
        }
    }
    
    /**
     * Get a registered DOM element
     * @param {string} name - Internal name of the element
     * @returns {HTMLElement|null} DOM element or null if not found
     */
    get(name) {
        const element = this.elements.get(name);
        if (!element) {
            console.warn(`DOM element not found: ${name}`);
        }
        return element || null;
    }
    
    /**
     * Check if an element exists and is registered
     * @param {string} name - Internal name of the element
     * @returns {boolean} True if element exists
     */
    has(name) {
        return this.elements.has(name) && this.elements.get(name) !== null;
    }
    
    /**
     * Add event listener to a registered element
     * @param {string} name - Internal name of the element
     * @param {string} event - Event type
     * @param {Function} handler - Event handler function
     * @param {Object} options - Event listener options
     * @returns {boolean} True if listener was added successfully
     */
    addEventListener(name, event, handler, options = {}) {
        const element = this.get(name);
        if (!element) {
            console.error(`Cannot add event listener: element ${name} not found`);
            return false;
        }
        
        try {
            element.addEventListener(event, handler, options);
            return true;
        } catch (error) {
            console.error(`Error adding event listener to ${name}:`, error);
            return false;
        }
    }
    
    /**
     * Remove event listener from a registered element
     * @param {string} name - Internal name of the element
     * @param {string} event - Event type
     * @param {Function} handler - Event handler function
     * @returns {boolean} True if listener was removed successfully
     */
    removeEventListener(name, event, handler) {
        const element = this.get(name);
        if (!element) {
            console.error(`Cannot remove event listener: element ${name} not found`);
            return false;
        }
        
        try {
            element.removeEventListener(event, handler);
            return true;
        } catch (error) {
            console.error(`Error removing event listener from ${name}:`, error);
            return false;
        }
    }
    
    /**
     * Safely set property on a registered element
     * @param {string} name - Internal name of the element
     * @param {string} property - Property name
     * @param {any} value - Property value
     * @returns {boolean} True if property was set successfully
     */
    setProperty(name, property, value) {
        const element = this.get(name);
        if (!element) {
            console.error(`Cannot set property: element ${name} not found`);
            return false;
        }
        
        try {
            element[property] = value;
            return true;
        } catch (error) {
            console.error(`Error setting property ${property} on ${name}:`, error);
            return false;
        }
    }
    
    /**
     * Safely get property from a registered element
     * @param {string} name - Internal name of the element
     * @param {string} property - Property name
     * @returns {any} Property value or null if not found
     */
    getProperty(name, property) {
        const element = this.get(name);
        if (!element) {
            console.error(`Cannot get property: element ${name} not found`);
            return null;
        }
        
        try {
            return element[property];
        } catch (error) {
            console.error(`Error getting property ${property} from ${name}:`, error);
            return null;
        }
    }
}

// Export singleton instance
const domElements = new DOMElements();
export default domElements;