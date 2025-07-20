// Page visibility detection for refreshing content when user returns to the page
let pageVisibilityCallbacks = [];

// Function to add a visibility change listener for a Blazor component
window.addVisibilityChangeListener = (dotNetHelper) => {
    // Store the callback
    pageVisibilityCallbacks.push(dotNetHelper);
    
    // Add the visibility change event listener if this is the first callback
    if (pageVisibilityCallbacks.length === 1) {
        document.addEventListener('visibilitychange', handleVisibilityChange);
        
        // Also listen for focus events as a fallback
        window.addEventListener('focus', handlePageFocus);
    }
};

// Function to remove a visibility change listener
window.removeVisibilityChangeListener = (dotNetHelper) => {
    const index = pageVisibilityCallbacks.indexOf(dotNetHelper);
    if (index > -1) {
        pageVisibilityCallbacks.splice(index, 1);
    }
    
    // Remove event listeners if no more callbacks
    if (pageVisibilityCallbacks.length === 0) {
        document.removeEventListener('visibilitychange', handleVisibilityChange);
        window.removeEventListener('focus', handlePageFocus);
    }
};

// Handle visibility change events
function handleVisibilityChange() {
    if (!document.hidden) {
        // Page became visible
        callPageVisibleHandlers();
    }
}

// Handle window focus events
function handlePageFocus() {
    callPageVisibleHandlers();
}

// Call all registered page visible handlers
function callPageVisibleHandlers() {
    pageVisibilityCallbacks.forEach(callback => {
        try {
            callback.invokeMethodAsync('OnPageVisible');
        } catch (error) {
            console.warn('Error calling OnPageVisible:', error);
        }
    });
}

// Cleanup function for when components are disposed
window.disposeVisibilityListener = (dotNetHelper) => {
    window.removeVisibilityChangeListener(dotNetHelper);
    
    // Dispose the .NET object reference
    if (dotNetHelper) {
        dotNetHelper.dispose();
    }
};