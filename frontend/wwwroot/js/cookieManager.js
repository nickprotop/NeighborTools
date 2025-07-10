// Cookie Management Utilities for GDPR Compliance

window.cookieManager = {
    // Generate or get session ID
    getSessionId: function() {
        let sessionId = sessionStorage.getItem('sessionId');
        if (!sessionId) {
            sessionId = 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            sessionStorage.setItem('sessionId', sessionId);
        }
        return sessionId;
    },

    // Set a cookie with consent checking
    setCookie: function(name, value, days, category = 'essential') {
        if (!this.hasConsentForCategory(category)) {
            console.log(`Cookie ${name} not set - no consent for category ${category}`);
            return false;
        }

        const expires = days ? `; expires=${new Date(Date.now() + days * 864e5).toUTCString()}` : '';
        document.cookie = `${name}=${value}${expires}; path=/; SameSite=Strict`;
        return true;
    },

    // Get a cookie value
    getCookie: function(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    },

    // Delete a cookie
    deleteCookie: function(name) {
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
    },

    // Check if we have consent for a specific category
    hasConsentForCategory: function(category) {
        const consent = localStorage.getItem('cookieConsent');
        if (!consent) return category === 'essential';
        
        try {
            const consentData = JSON.parse(consent);
            return consentData[category] === true;
        } catch {
            return category === 'essential';
        }
    },

    // Apply cookie settings based on consent
    applyCookieSettings: function(settings) {
        const consentData = {
            essential: true, // Always true
            functional: settings.functional || false,
            analytics: settings.analytics || false,
            marketing: settings.marketing || false,
            timestamp: Date.now()
        };

        localStorage.setItem('cookieConsent', JSON.stringify(consentData));

        // Clean up non-consented cookies
        if (!settings.functional) {
            this.cleanupFunctionalCookies();
        }
        if (!settings.analytics) {
            this.cleanupAnalyticsCookies();
        }
        if (!settings.marketing) {
            this.cleanupMarketingCookies();
        }

        // Initialize allowed services
        if (settings.analytics) {
            this.initializeAnalytics();
        }
        if (settings.marketing) {
            this.initializeMarketing();
        }
    },

    // Enable all cookies (accept all)
    enableAllCookies: function() {
        this.applyCookieSettings({
            functional: true,
            analytics: true,
            marketing: true
        });
    },

    // Disable non-essential cookies (reject all)
    disableNonEssentialCookies: function() {
        this.applyCookieSettings({
            functional: false,
            analytics: false,
            marketing: false
        });
    },

    // Cleanup functions for different cookie categories
    cleanupFunctionalCookies: function() {
        // List of functional cookies to remove
        const functionalCookies = ['user_preferences', 'theme', 'language'];
        functionalCookies.forEach(cookie => this.deleteCookie(cookie));
    },

    cleanupAnalyticsCookies: function() {
        // List of analytics cookies to remove
        const analyticsCookies = ['_ga', '_gid', '_gat', '_gtag'];
        analyticsCookies.forEach(cookie => this.deleteCookie(cookie));
        
        // Disable Google Analytics if present
        if (window.gtag) {
            window.gtag('config', 'GA_MEASUREMENT_ID', {
                'send_page_view': false
            });
        }
    },

    cleanupMarketingCookies: function() {
        // List of marketing cookies to remove
        const marketingCookies = ['_fbp', '_fbc', 'ads_prefs'];
        marketingCookies.forEach(cookie => this.deleteCookie(cookie));
    },

    // Initialize analytics services
    initializeAnalytics: function() {
        // Google Analytics initialization
        if (window.gtag && window.GA_MEASUREMENT_ID) {
            window.gtag('config', window.GA_MEASUREMENT_ID, {
                'send_page_view': true,
                'anonymize_ip': true
            });
            console.log('Analytics initialized with consent');
        }
    },

    // Initialize marketing services
    initializeMarketing: function() {
        // Marketing service initialization
        console.log('Marketing services initialized with consent');
    },

    // Get current consent status
    getConsentStatus: function() {
        const consent = localStorage.getItem('cookieConsent');
        if (!consent) return null;
        
        try {
            return JSON.parse(consent);
        } catch {
            return null;
        }
    },

    // Check if consent is expired (1 year)
    isConsentExpired: function() {
        const consent = this.getConsentStatus();
        if (!consent || !consent.timestamp) return true;
        
        const oneYear = 365 * 24 * 60 * 60 * 1000; // milliseconds
        return Date.now() - consent.timestamp > oneYear;
    }
};

// Global functions for Blazor interop
window.getSessionId = function() {
    return window.cookieManager.getSessionId();
};

window.applyCookieSettings = function(settings) {
    window.cookieManager.applyCookieSettings(settings);
};

window.enableAllCookies = function() {
    window.cookieManager.enableAllCookies();
};

window.disableNonEssentialCookies = function() {
    window.cookieManager.disableNonEssentialCookies();
};

window.downloadFile = function(filename, content, contentType) {
    const blob = new Blob([atob(content)], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};

// Auto-check consent on page load
document.addEventListener('DOMContentLoaded', function() {
    if (window.cookieManager.isConsentExpired()) {
        localStorage.removeItem('cookieConsent');
        console.log('Cookie consent expired - banner will be shown');
    }
});