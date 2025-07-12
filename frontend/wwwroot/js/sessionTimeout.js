window.sessionTimeout = {
    dotNetHelper: null,
    timeoutMinutes: 480,
    activityEvents: ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'],
    lastActivity: Date.now(),
    debounceTimer: null,
    warningShown: false,

    initialize: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.lastActivity = Date.now();
        this.setupActivityListeners();
        console.log('Session timeout JavaScript initialized');
    },

    setupActivityListeners: function () {
        const self = this;
        
        // Add event listeners for user activity
        this.activityEvents.forEach(event => {
            document.addEventListener(event, function() {
                self.onActivity();
            }, true);
        });

        // Track page visibility changes
        document.addEventListener('visibilitychange', function() {
            if (!document.hidden) {
                self.onActivity();
            }
        });

        // Track focus events
        window.addEventListener('focus', function() {
            self.onActivity();
        });
    },

    onActivity: function () {
        this.lastActivity = Date.now();
        this.warningShown = false;

        // Debounce the activity reporting to avoid too many calls
        if (this.debounceTimer) {
            clearTimeout(this.debounceTimer);
        }

        this.debounceTimer = setTimeout(() => {
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('ResetActivityAsync')
                    .catch(error => {
                        console.warn('Failed to reset activity timer:', error);
                    });
            }
        }, 1000); // Report activity at most once per second
    },

    updateTimeout: function (timeoutMinutes) {
        this.timeoutMinutes = timeoutMinutes;
        this.lastActivity = Date.now();
        this.warningShown = false;
        console.log('Session timeout updated to', timeoutMinutes, 'minutes');
    },

    showExpiringWarning: function () {
        if (this.warningShown) return;
        this.warningShown = true;

        // Calculate minutes until expiry
        const warningMinutes = this.timeoutMinutes > 50 ? 5 : Math.max(1, Math.floor(this.timeoutMinutes / 10));
        
        // Show a native browser notification if possible
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Session Expiring', {
                body: `Your session will expire in ${warningMinutes} minutes due to inactivity.`,
                icon: '/favicon.ico',
                requireInteraction: true
            });
        }

        // Show console warning
        console.warn(`Session will expire in ${warningMinutes} minutes due to inactivity`);

        // You can also trigger a custom event that the Blazor app can listen to
        window.dispatchEvent(new CustomEvent('sessionExpiring', {
            detail: { warningMinutes: warningMinutes }
        }));
    },

    onSessionExpired: function () {
        console.log('Session expired due to inactivity');
        
        // Show notification
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Session Expired', {
                body: 'You have been logged out due to inactivity.',
                icon: '/favicon.ico'
            });
        }

        // Clear any stored tokens (backup cleanup)
        try {
            localStorage.removeItem('authToken');
            localStorage.removeItem('refreshToken');
            sessionStorage.removeItem('authToken');
            sessionStorage.removeItem('refreshToken');
        } catch (error) {
            console.warn('Failed to clear tokens:', error);
        }

        // Trigger custom event
        window.dispatchEvent(new CustomEvent('sessionExpired'));

        // Redirect to login page after a short delay
        setTimeout(() => {
            window.location.href = '/login';
        }, 2000);
    },

    cleanup: function () {
        if (this.debounceTimer) {
            clearTimeout(this.debounceTimer);
            this.debounceTimer = null;
        }
        
        this.dotNetHelper = null;
        console.log('Session timeout JavaScript cleaned up');
    },

    // Utility function to request notification permission
    requestNotificationPermission: function () {
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission().then(permission => {
                console.log('Notification permission:', permission);
            });
        }
    }
};

// Request notification permission when the script loads
document.addEventListener('DOMContentLoaded', function() {
    window.sessionTimeout.requestNotificationPermission();
});