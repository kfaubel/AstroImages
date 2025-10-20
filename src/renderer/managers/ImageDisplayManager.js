/**
 * Image Display Manager
 * 
 * Handles all image display operations including loading, rendering,
 * zoom controls, and image transformations. Provides a clean interface
 * for image manipulation and maintains display state.
 */

import appState from '../core/AppState.js';
import domElements from '../core/DOMElements.js';

class ImageDisplayManager {
    constructor() {
        this.initialized = false;
    }
    
    /**
     * Initialize the image display manager
     */
    init() {
        if (this.initialized) return;
        
        // Subscribe to state changes
        appState.subscribe('currentImageIndex', (index) => this.handleImageIndexChange(index));
        appState.subscribe('currentZoom', () => this.updateZoomDisplay());
        appState.subscribe('isFitToWindow', () => this.updateZoomButtons());
        
        // Setup event listeners
        this.setupImageEventListeners();
        this.setupZoomEventListeners();
        this.setupResizeObserver();
        
        this.initialized = true;
        console.log('ImageDisplayManager initialized');
    }
    
    /**
     * Setup image container event listeners
     */
    setupImageEventListeners() {
        const imageContainer = domElements.get('imageContainer');
        if (!imageContainer) return;
        
        // Image dragging for pan
        imageContainer.addEventListener('mousedown', this.startImageDrag.bind(this));
        imageContainer.addEventListener('mousemove', this.handleImageDrag.bind(this));
        imageContainer.addEventListener('mouseup', this.stopImageDrag.bind(this));
        imageContainer.addEventListener('mouseleave', this.stopImageDrag.bind(this));
        
        // Mouse wheel for zoom
        imageContainer.addEventListener('wheel', this.handleWheel.bind(this));
        
        // Prevent context menu
        imageContainer.addEventListener('contextmenu', (e) => e.preventDefault());
    }
    
    /**
     * Setup zoom control event listeners
     */
    setupZoomEventListeners() {
        domElements.addEventListener('zoomInBtn', 'click', () => this.zoomIn());
        domElements.addEventListener('zoomOutBtn', 'click', () => this.zoomOut());
        domElements.addEventListener('zoomActualBtn', 'click', () => this.zoomActual());
        domElements.addEventListener('zoomFitBtn', 'click', () => this.zoomFit());
        domElements.addEventListener('autoStretchCheckbox', 'change', this.handleAutoStretchChange.bind(this));
    }
    
    /**
     * Setup resize observer for responsive image display
     */
    setupResizeObserver() {
        const mainContent = domElements.get('mainContent');
        if (!mainContent) return;
        
        // Add ResizeObserver to detect image pane geometry changes
        const resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                if (entry.target === mainContent && 
                    appState.get('isFitToWindow') && 
                    this.hasImageLoaded()) {
                    // Debounce the resize to avoid excessive calls
                    clearTimeout(resizeObserver.timeoutId);
                    resizeObserver.timeoutId = setTimeout(() => {
                        this.zoomFit();
                    }, 100);
                }
            }
        });
        
        resizeObserver.observe(mainContent);
        
        // Also listen for window resize
        window.addEventListener('resize', () => {
            if (appState.get('isFitToWindow') && this.hasImageLoaded()) {
                this.recalculateFitToWindow();
            }
        });
    }
    
    /**
     * Handle image index change
     * @param {number} index - New image index
     */
    async handleImageIndexChange(index) {
        const files = appState.get('currentFiles');
        if (!files || index < 0 || index >= files.length) {
            this.hideImage();
            return;
        }
        
        try {
            await this.displayImage(files[index]);
        } catch (error) {
            console.error('Error displaying image:', error);
            this.hideImage();
        }
    }
    
    /**
     * Display an image
     * @param {Object} file - File object to display
     */
    async displayImage(file) {
        try {
            // Show loading for image processing
            this.showLoading(file.isFits ? 'Processing FITS file...' : 'Loading image...');
            
            // Hide no-image placeholder
            const noImageDiv = domElements.get('noImageDiv');
            if (noImageDiv) noImageDiv.style.display = 'none';
            
            // Show loading state in image container
            const imageDisplay = domElements.get('imageDisplay');
            if (imageDisplay) {
                imageDisplay.style.display = 'block';
                imageDisplay.src = this.getLoadingImageSrc();
            }
            
            let imageSrc;
            
            if (file.isFits) {
                // Process FITS file with optional stretching
                console.log('Processing FITS file:', file.path);
                const applyStretch = appState.get('autoStretch') || false;
                imageSrc = await window.electronAPI.processFitsFileStretched(file.path, applyStretch);
                console.log('FITS processing completed successfully with stretch:', applyStretch);
            } else {
                // Regular image file
                imageSrc = await window.electronAPI.getFilePath(file.path);
            }
            
            // Load the processed image and wait for it to complete
            await this.loadImage(imageSrc);
            
            // Always fit to window after image is loaded and displayed
            setTimeout(() => {
                this.zoomFit();
                this.applyAutoStretch();
            }, 50);
            
            // Show zoom controls
            const zoomControls = domElements.get('zoomControls');
            if (zoomControls) zoomControls.style.display = 'flex';
            
            // Handle slideshow continuation
            if (appState.get('isPlaying')) {
                this.scheduleNextSlide();
            }
            
        } catch (error) {
            console.error('Error displaying image:', error);
            this.hideImage();
            
            // Still continue slideshow if playing, even if image failed to load
            if (appState.get('isPlaying')) {
                this.scheduleNextSlide();
            }
        } finally {
            this.hideLoading();
        }
    }
    
    /**
     * Load image and wait for completion
     * @param {string} imageSrc - Image source URL
     * @returns {Promise} Promise that resolves when image is loaded
     */
    loadImage(imageSrc) {
        return new Promise((resolve, reject) => {
            const tempImage = new Image();
            const imageDisplay = domElements.get('imageDisplay');
            
            tempImage.onload = () => {
                // Image loaded successfully
                if (imageDisplay) imageDisplay.src = imageSrc;
                
                // Store natural dimensions for zoom calculations
                appState.update({
                    imageNaturalWidth: tempImage.naturalWidth,
                    imageNaturalHeight: tempImage.naturalHeight
                });
                
                resolve();
            };
            
            tempImage.onerror = () => {
                reject(new Error('Failed to load image'));
            };
            
            tempImage.src = imageSrc;
        });
    }
    
    /**
     * Hide the current image
     */
    hideImage() {
        const imageDisplay = domElements.get('imageDisplay');
        const noImageDiv = domElements.get('noImageDiv');
        const zoomControls = domElements.get('zoomControls');
        
        if (imageDisplay) {
            imageDisplay.style.display = 'none';
            imageDisplay.src = '';
        }
        
        if (noImageDiv) noImageDiv.style.display = 'flex';
        if (zoomControls) zoomControls.style.display = 'none';
        
        appState.update({
            currentImageIndex: -1,
            imageNaturalWidth: 0,
            imageNaturalHeight: 0,
            currentZoom: 1.0,
            isFitToWindow: true
        });
    }
    
    /**
     * Check if an image is currently loaded
     * @returns {boolean} True if image is loaded
     */
    hasImageLoaded() {
        const imageDisplay = domElements.get('imageDisplay');
        return imageDisplay && imageDisplay.src && imageDisplay.style.display !== 'none';
    }
    
    /**
     * Get loading image source (animated spinner)
     * @returns {string} Data URL for loading spinner
     */
    getLoadingImageSrc() {
        return 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNTAiIGhlaWdodD0iNTAiIHZpZXdCb3g9IjAgMCA1MCA1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPGNpcmNsZSBjeD0iMjUiIGN5PSIyNSIgcj0iMjAiIGZpbGw9Im5vbmUiIHN0cm9rZT0iIzk5OSIgc3Ryb2tlLXdpZHRoPSI0IiBzdHJva2UtZGFzaGFycmF5PSI4MCA4MCI+CjxhbmltYXRlVHJhbnNmb3JtIGF0dHJpYnV0ZU5hbWU9InRyYW5zZm9ybSIgdHlwZT0icm90YXRlIiB2YWx1ZXM9IjAgMjUgMjU7MzYwIDI1IDI1IiBkdXI9IjFzIiByZXBlYXRDb3VudD0iaW5kZWZpbml0ZSIvPgo8L2NpcmNsZT4KPC9zdmc+';
    }
    
    /**
     * Show loading overlay
     * @param {string} message - Loading message
     */
    showLoading(message = 'Loading...') {
        const loadingOverlay = domElements.get('loadingOverlay');
        const loadingText = domElements.get('loadingText');
        
        // Clear any pending hide timeout
        const loadingTimeout = appState.getTimer('loadingTimeout');
        if (loadingTimeout) {
            clearTimeout(loadingTimeout);
            appState.setTimer('loadingTimeout', null);
        }
        
        if (loadingText) loadingText.textContent = message;
        if (loadingOverlay) loadingOverlay.style.display = 'flex';
    }
    
    /**
     * Hide loading overlay
     */
    hideLoading() {
        const loadingOverlay = domElements.get('loadingOverlay');
        
        // Add a small delay to prevent flashing for very quick operations
        const timeout = setTimeout(() => {
            if (loadingOverlay) loadingOverlay.style.display = 'none';
            appState.setTimer('loadingTimeout', null);
        }, 100);
        
        appState.setTimer('loadingTimeout', timeout);
    }
    
    /**
     * Zoom in
     */
    zoomIn() {
        appState.set('isFitToWindow', false);
        this.setZoom(appState.get('currentZoom') * 1.25);
    }
    
    /**
     * Zoom out
     */
    zoomOut() {
        appState.set('isFitToWindow', false);
        this.setZoom(appState.get('currentZoom') / 1.25);
    }
    
    /**
     * Zoom to actual size (100%)
     */
    zoomActual() {
        appState.set('isFitToWindow', false);
        this.setZoom(1.0);
    }
    
    /**
     * Zoom to fit window
     */
    zoomFit() {
        const imageContainer = domElements.get('imageContainer');
        if (!imageContainer || !this.hasImageLoaded()) return;
        
        const containerRect = imageContainer.getBoundingClientRect();
        const containerWidth = containerRect.width - 20; // Account for minimal padding
        const containerHeight = containerRect.height - 20;
        
        const imageNaturalWidth = appState.get('imageNaturalWidth');
        const imageNaturalHeight = appState.get('imageNaturalHeight');
        
        if (imageNaturalWidth > 0 && imageNaturalHeight > 0) {
            const scaleX = containerWidth / imageNaturalWidth;
            const scaleY = containerHeight / imageNaturalHeight;
            const scale = Math.min(scaleX, scaleY);
            
            appState.update({
                fitToWindowScale: scale,
                isFitToWindow: true,
                currentZoom: scale
            });
            
            this.applyImageTransform(scale);
            this.updateZoomDisplay();
            this.updateZoomButtons();
            this.updateScrollBars();
        }
    }
    
    /**
     * Set specific zoom level
     * @param {number} zoom - Zoom level
     */
    setZoom(zoom) {
        const clampedZoom = Math.max(0.1, Math.min(10.0, zoom));
        appState.set('currentZoom', clampedZoom);
        
        this.applyImageTransform(clampedZoom);
        this.updateZoomDisplay();
        this.updateZoomButtons();
        this.updateScrollBars();
    }
    
    /**
     * Apply image transform
     * @param {number} scale - Scale factor
     */
    applyImageTransform(scale) {
        const imageDisplay = domElements.get('imageDisplay');
        if (imageDisplay) {
            imageDisplay.style.transform = `scale(${scale})`;
        }
    }
    
    /**
     * Update zoom display
     */
    updateZoomDisplay() {
        const zoomLevel = domElements.get('zoomLevel');
        if (zoomLevel) {
            const zoom = Math.round(appState.get('currentZoom') * 100);
            zoomLevel.textContent = `${zoom}%`;
        }
    }
    
    /**
     * Update zoom button states
     */
    updateZoomButtons() {
        const isFitToWindow = appState.get('isFitToWindow');
        const zoomFitBtn = domElements.get('zoomFitBtn');
        
        if (zoomFitBtn) {
            zoomFitBtn.classList.toggle('active', isFitToWindow);
        }
    }
    
    /**
     * Update scroll bars visibility
     */
    updateScrollBars() {
        const imageContainer = domElements.get('imageContainer');
        const currentZoom = appState.get('currentZoom');
        const isFitToWindow = appState.get('isFitToWindow');
        
        if (imageContainer) {
            if (isFitToWindow || currentZoom <= 1.0) {
                imageContainer.style.overflow = 'hidden';
            } else {
                imageContainer.style.overflow = 'auto';
            }
        }
    }
    
    /**
     * Recalculate fit-to-window scaling
     */
    recalculateFitToWindow() {
        const imageContainer = domElements.get('imageContainer');
        if (!imageContainer) return;
        
        const containerRect = imageContainer.getBoundingClientRect();
        const containerWidth = containerRect.width - 20;
        const containerHeight = containerRect.height - 20;
        
        const imageNaturalWidth = appState.get('imageNaturalWidth');
        const imageNaturalHeight = appState.get('imageNaturalHeight');
        
        if (imageNaturalWidth > 0 && imageNaturalHeight > 0) {
            const scaleX = containerWidth / imageNaturalWidth;
            const scaleY = containerHeight / imageNaturalHeight;
            const scale = Math.min(scaleX, scaleY);
            
            appState.update({
                fitToWindowScale: scale,
                currentZoom: scale
            });
            
            this.applyImageTransform(scale);
            this.updateZoomDisplay();
            this.updateZoomButtons();
            this.updateScrollBars();
        }
    }
    
    /**
     * Handle auto stretch checkbox change
     * @param {Event} event - Change event
     */
    handleAutoStretchChange(event) {
        appState.set('autoStretch', event.target.checked);
        
        // Re-display current image if one is loaded
        const currentImageIndex = appState.get('currentImageIndex');
        if (currentImageIndex >= 0) {
            appState.set('currentImageIndex', currentImageIndex); // Trigger re-display
        }
    }
    
    /**
     * Apply auto stretch if enabled
     */
    applyAutoStretch() {
        const autoStretch = appState.get('autoStretch');
        const imageDisplay = domElements.get('imageDisplay');
        
        if (imageDisplay && autoStretch) {
            // Apply any additional stretch effects if needed
            // This is a placeholder for future auto-stretch functionality
        }
    }
    
    /**
     * Start image dragging
     * @param {MouseEvent} event - Mouse event
     */
    startImageDrag(event) {
        if (event.button !== 0) return; // Only left mouse button
        
        appState.update({
            isDragging: true,
            lastMouseX: event.clientX,
            lastMouseY: event.clientY
        });
        
        event.preventDefault();
    }
    
    /**
     * Handle image dragging
     * @param {MouseEvent} event - Mouse event
     */
    handleImageDrag(event) {
        if (!appState.get('isDragging')) return;
        
        const deltaX = event.clientX - appState.get('lastMouseX');
        const deltaY = event.clientY - appState.get('lastMouseY');
        
        const imageContainer = domElements.get('imageContainer');
        if (imageContainer) {
            imageContainer.scrollLeft -= deltaX;
            imageContainer.scrollTop -= deltaY;
        }
        
        appState.update({
            lastMouseX: event.clientX,
            lastMouseY: event.clientY
        });
    }
    
    /**
     * Stop image dragging
     */
    stopImageDrag() {
        appState.set('isDragging', false);
    }
    
    /**
     * Handle mouse wheel for zoom
     * @param {WheelEvent} event - Wheel event
     */
    handleWheel(event) {
        if (!event.ctrlKey) return; // Only zoom with Ctrl+wheel
        
        event.preventDefault();
        
        const delta = event.deltaY > 0 ? 0.9 : 1.1;
        const newZoom = appState.get('currentZoom') * delta;
        
        appState.set('isFitToWindow', false);
        this.setZoom(newZoom);
    }
    
    /**
     * Schedule next slide for slideshow
     */
    scheduleNextSlide() {
        const playbackInterval = appState.get('playbackInterval');
        const timer = setTimeout(() => {
            if (appState.get('isPlaying')) {
                // Trigger next image
                window.dispatchEvent(new CustomEvent('playbackNext'));
            }
        }, playbackInterval);
        
        appState.setTimer('playbackTimer', timer);
    }
}

// Export singleton instance
const imageDisplayManager = new ImageDisplayManager();
export default imageDisplayManager;