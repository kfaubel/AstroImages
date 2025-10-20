/**
 * Application State Manager
 * 
 * Centralized state management for the AstroImages application.
 * Provides a single source of truth for all application state with
 * reactive updates and state persistence.
 */

class AppState {
    constructor() {
        // Initialize all application state
        this.state = {
            // Directory and file management
            currentDirectory: null,
            currentFiles: [],
            selectedFiles: new Set(),
            isWatchingDirectory: false,
            
            // Configuration
            keywords: [],
            fitsHeaders: [],
            
            // Table sorting
            sortColumn: null,
            sortDirection: 'asc',
            
            // Image display
            currentImageIndex: -1,
            imageNaturalWidth: 0,
            imageNaturalHeight: 0,
            
            // Playback control
            isPlaying: false,
            playbackInterval: 2000,
            
            // Zoom and pan
            currentZoom: 1.0,
            fitToWindowScale: 1.0,
            isFitToWindow: true,
            isDragging: false,
            lastMouseX: 0,
            lastMouseY: 0
        };
        
        // Event listeners for state changes
        this.listeners = new Map();
        
        // Timers and handles
        this.timers = {
            playbackTimer: null,
            loadingTimeout: null
        };
    }
    
    /**
     * Get current state value
     * @param {string} key - State key to retrieve
     * @returns {any} Current state value
     */
    get(key) {
        return this.state[key];
    }
    
    /**
     * Set state value and notify listeners
     * @param {string} key - State key to update
     * @param {any} value - New value
     */
    set(key, value) {
        const oldValue = this.state[key];
        this.state[key] = value;
        
        // Notify listeners of state change
        this.notifyListeners(key, value, oldValue);
    }
    
    /**
     * Update multiple state values at once
     * @param {Object} updates - Object with key-value pairs to update
     */
    update(updates) {
        Object.entries(updates).forEach(([key, value]) => {
            this.set(key, value);
        });
    }
    
    /**
     * Subscribe to state changes
     * @param {string} key - State key to watch
     * @param {Function} callback - Function to call when state changes
     */
    subscribe(key, callback) {
        if (!this.listeners.has(key)) {
            this.listeners.set(key, new Set());
        }
        this.listeners.get(key).add(callback);
        
        // Return unsubscribe function
        return () => {
            this.listeners.get(key)?.delete(callback);
        };
    }
    
    /**
     * Notify all listeners of a state change
     * @param {string} key - State key that changed
     * @param {any} newValue - New value
     * @param {any} oldValue - Previous value
     */
    notifyListeners(key, newValue, oldValue) {
        const listeners = this.listeners.get(key);
        if (listeners) {
            listeners.forEach(callback => {
                try {
                    callback(newValue, oldValue, key);
                } catch (error) {
                    console.error(`Error in state listener for ${key}:`, error);
                }
            });
        }
    }
    
    /**
     * Get timer handle
     * @param {string} name - Timer name
     * @returns {number|null} Timer handle
     */
    getTimer(name) {
        return this.timers[name];
    }
    
    /**
     * Set timer handle
     * @param {string} name - Timer name
     * @param {number|null} handle - Timer handle
     */
    setTimer(name, handle) {
        // Clear existing timer if present
        if (this.timers[name]) {
            clearTimeout(this.timers[name]);
        }
        this.timers[name] = handle;
    }
    
    /**
     * Clear all timers
     */
    clearAllTimers() {
        Object.values(this.timers).forEach(timer => {
            if (timer) clearTimeout(timer);
        });
        this.timers = {
            playbackTimer: null,
            loadingTimeout: null
        };
    }
    
    /**
     * Reset application state to defaults
     */
    reset() {
        this.clearAllTimers();
        
        const defaultState = {
            currentDirectory: null,
            currentFiles: [],
            selectedFiles: new Set(),
            isWatchingDirectory: false,
            keywords: [],
            fitsHeaders: [],
            sortColumn: null,
            sortDirection: 'asc',
            currentImageIndex: -1,
            imageNaturalWidth: 0,
            imageNaturalHeight: 0,
            isPlaying: false,
            playbackInterval: 2000,
            currentZoom: 1.0,
            fitToWindowScale: 1.0,
            isFitToWindow: true,
            isDragging: false,
            lastMouseX: 0,
            lastMouseY: 0
        };
        
        Object.entries(defaultState).forEach(([key, value]) => {
            this.set(key, value);
        });
    }
}

// Export singleton instance
const appState = new AppState();
export default appState;