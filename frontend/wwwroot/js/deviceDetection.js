// Device Detection JavaScript Service
window.deviceDetection = {
    // Detect if the device is mobile based on multiple criteria
    isMobile: function() {
        // Check user agent for mobile patterns
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;
        const mobileRegex = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;
        const isUserAgentMobile = mobileRegex.test(userAgent.toLowerCase());
        
        // Check screen size (consider tablets as desktop for pagination purposes)
        const isSmallScreen = window.innerWidth <= 768;
        
        // Check for touch capability
        const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
        
        // Check if it's likely a tablet (larger screen but touch-enabled)
        const isTablet = isTouchDevice && window.innerWidth >= 768 && window.innerWidth <= 1024;
        
        // Mobile detection logic:
        // - Small screen AND touch-enabled = mobile
        // - Mobile user agent = mobile
        // - Tablets are treated as desktop for better pagination UX
        return (isSmallScreen && isTouchDevice) || (isUserAgentMobile && !isTablet);
    },
    
    // Get detailed device information
    getDeviceInfo: function() {
        return {
            isMobile: this.isMobile(),
            screenWidth: window.innerWidth,
            screenHeight: window.innerHeight,
            userAgent: navigator.userAgent,
            isTouchDevice: 'ontouchstart' in window || navigator.maxTouchPoints > 0,
            platform: navigator.platform,
            devicePixelRatio: window.devicePixelRatio || 1
        };
    },
    
    // Listen for screen size changes (device rotation, window resize)
    onScreenSizeChange: function(callback) {
        let timeoutId;
        const debouncedCallback = () => {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                callback(this.getDeviceInfo());
            }, 150); // Debounce to avoid excessive calls
        };
        
        window.addEventListener('resize', debouncedCallback);
        window.addEventListener('orientationchange', debouncedCallback);
        
        // Return cleanup function
        return () => {
            window.removeEventListener('resize', debouncedCallback);
            window.removeEventListener('orientationchange', debouncedCallback);
            clearTimeout(timeoutId);
        };
    }
};