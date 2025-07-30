// Geolocation JavaScript interop functions
window.geolocationService = {
    
    // Check if geolocation is supported
    isSupported: function() {
        return "geolocation" in navigator;
    },

    // Get current position with options
    getCurrentPosition: function(options) {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject({
                    error: 4, // NotSupported
                    message: "Geolocation is not supported by this browser"
                });
                return;
            }

            const defaultOptions = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 300000 // 5 minutes
            };

            const finalOptions = { ...defaultOptions, ...options };

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        success: true,
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        timestamp: new Date(position.timestamp).toISOString()
                    });
                },
                (error) => {
                    let errorType;
                    let errorMessage;

                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            errorType = 1; // PermissionDenied
                            errorMessage = "Location access was denied by the user";
                            break;
                        case error.POSITION_UNAVAILABLE:
                            errorType = 2; // PositionUnavailable
                            errorMessage = "Location information is unavailable";
                            break;
                        case error.TIMEOUT:
                            errorType = 3; // Timeout
                            errorMessage = "Location request timed out";
                            break;
                        default:
                            errorType = 2; // PositionUnavailable
                            errorMessage = "An unknown error occurred";
                            break;
                    }

                    reject({
                        success: false,
                        error: errorType,
                        message: errorMessage
                    });
                },
                finalOptions
            );
        });
    },

    // Watch position changes (for future use)
    watchPosition: function(options) {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject({
                    error: 4, // NotSupported
                    message: "Geolocation is not supported by this browser"
                });
                return;
            }

            const defaultOptions = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 300000 // 5 minutes
            };

            const finalOptions = { ...defaultOptions, ...options };

            const watchId = navigator.geolocation.watchPosition(
                (position) => {
                    resolve({
                        success: true,
                        watchId: watchId,
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        timestamp: new Date(position.timestamp).toISOString()
                    });
                },
                (error) => {
                    let errorType;
                    let errorMessage;

                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            errorType = 1; // PermissionDenied
                            errorMessage = "Location access was denied by the user";
                            break;
                        case error.POSITION_UNAVAILABLE:
                            errorType = 2; // PositionUnavailable
                            errorMessage = "Location information is unavailable";
                            break;
                        case error.TIMEOUT:
                            errorType = 3; // Timeout
                            errorMessage = "Location request timed out";
                            break;
                        default:
                            errorType = 2; // PositionUnavailable
                            errorMessage = "An unknown error occurred";
                            break;
                    }

                    reject({
                        success: false,
                        error: errorType,
                        message: errorMessage
                    });
                },
                finalOptions
            );
        });
    },

    // Clear position watch
    clearWatch: function(watchId) {
        if (navigator.geolocation && watchId) {
            navigator.geolocation.clearWatch(watchId);
        }
    },

    // Check permission status (for browsers that support it)
    checkPermissionStatus: function() {
        return new Promise((resolve) => {
            if (navigator.permissions && navigator.permissions.query) {
                navigator.permissions.query({ name: 'geolocation' })
                    .then((result) => {
                        resolve({
                            state: result.state, // 'granted', 'denied', or 'prompt'
                            supported: true
                        });
                    })
                    .catch(() => {
                        resolve({
                            state: 'unknown',
                            supported: false
                        });
                    });
            } else {
                resolve({
                    state: 'unknown',
                    supported: false
                });
            }
        });
    }
};