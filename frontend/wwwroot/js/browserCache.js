// Browser cache management with localStorage and LRU eviction
window.browserCache = {
    // Storage keys
    CACHE_PREFIX: 'nt_cache_',
    METADATA_KEY: 'nt_cache_metadata',
    
    // Get cache metadata
    getMetadata() {
        try {
            const metadata = localStorage.getItem(this.METADATA_KEY);
            return metadata ? JSON.parse(metadata) : { entries: {}, accessOrder: [] };
        } catch {
            return { entries: {}, accessOrder: [] };
        }
    },
    
    // Update cache metadata
    updateMetadata(metadata) {
        try {
            localStorage.setItem(this.METADATA_KEY, JSON.stringify(metadata));
        } catch {
            // Silently fail if localStorage is full
        }
    },
    
    // Get item from cache
    getItem(key) {
        try {
            const fullKey = this.CACHE_PREFIX + key;
            const data = localStorage.getItem(fullKey);
            
            if (data) {
                // Update access order for LRU
                this.updateAccessOrder(key);
                return data;
            }
            
            return null;
        } catch {
            return null;
        }
    },
    
    // Set item in cache
    setItem(key, value) {
        try {
            const fullKey = this.CACHE_PREFIX + key;
            const metadata = this.getMetadata();
            
            // Store the data
            localStorage.setItem(fullKey, value);
            
            // Update metadata
            metadata.entries[key] = {
                size: value.length,
                timestamp: Date.now()
            };
            
            // Update access order
            metadata.accessOrder = metadata.accessOrder.filter(k => k !== key);
            metadata.accessOrder.push(key);
            
            this.updateMetadata(metadata);
        } catch (error) {
            // If storage is full, try to make space and retry
            this.clearLRU(10);
            try {
                const fullKey = this.CACHE_PREFIX + key;
                localStorage.setItem(fullKey, value);
                
                const metadata = this.getMetadata();
                metadata.entries[key] = {
                    size: value.length,
                    timestamp: Date.now()
                };
                metadata.accessOrder = metadata.accessOrder.filter(k => k !== key);
                metadata.accessOrder.push(key);
                this.updateMetadata(metadata);
            } catch {
                // Still failed, silently ignore
            }
        }
    },
    
    // Remove item from cache
    removeItem(key) {
        try {
            const fullKey = this.CACHE_PREFIX + key;
            localStorage.removeItem(fullKey);
            
            const metadata = this.getMetadata();
            delete metadata.entries[key];
            metadata.accessOrder = metadata.accessOrder.filter(k => k !== key);
            this.updateMetadata(metadata);
        } catch {
            // Silently fail
        }
    },
    
    // Remove items by prefix
    removeByPrefix(prefix) {
        try {
            const metadata = this.getMetadata();
            const keysToRemove = Object.keys(metadata.entries).filter(key => key.startsWith(prefix));
            
            keysToRemove.forEach(key => {
                const fullKey = this.CACHE_PREFIX + key;
                localStorage.removeItem(fullKey);
                delete metadata.entries[key];
            });
            
            metadata.accessOrder = metadata.accessOrder.filter(key => !keysToRemove.includes(key));
            this.updateMetadata(metadata);
        } catch {
            // Silently fail
        }
    },
    
    // Clear expired entries
    clearExpired() {
        try {
            const now = Date.now();
            const metadata = this.getMetadata();
            const keysToRemove = [];
            
            Object.keys(metadata.entries).forEach(key => {
                try {
                    const fullKey = this.CACHE_PREFIX + key;
                    const data = localStorage.getItem(fullKey);
                    if (data) {
                        const parsed = JSON.parse(data);
                        const expiresAt = new Date(parsed.expiresAt).getTime();
                        if (now > expiresAt) {
                            keysToRemove.push(key);
                        }
                    }
                } catch {
                    // If we can't parse the data, consider it expired
                    keysToRemove.push(key);
                }
            });
            
            keysToRemove.forEach(key => {
                const fullKey = this.CACHE_PREFIX + key;
                localStorage.removeItem(fullKey);
                delete metadata.entries[key];
            });
            
            metadata.accessOrder = metadata.accessOrder.filter(key => !keysToRemove.includes(key));
            this.updateMetadata(metadata);
            
            return keysToRemove.length;
        } catch {
            return 0;
        }
    },
    
    // Clear least recently used entries
    clearLRU(count) {
        try {
            const metadata = this.getMetadata();
            const keysToRemove = metadata.accessOrder.slice(0, count);
            
            keysToRemove.forEach(key => {
                const fullKey = this.CACHE_PREFIX + key;
                localStorage.removeItem(fullKey);
                delete metadata.entries[key];
            });
            
            metadata.accessOrder = metadata.accessOrder.slice(count);
            this.updateMetadata(metadata);
            
            return keysToRemove.length;
        } catch {
            return 0;
        }
    },
    
    // Update access order for LRU
    updateAccessOrder(key) {
        try {
            const metadata = this.getMetadata();
            metadata.accessOrder = metadata.accessOrder.filter(k => k !== key);
            metadata.accessOrder.push(key);
            this.updateMetadata(metadata);
        } catch {
            // Silently fail
        }
    },
    
    // Get total storage size used by cache
    getStorageSize() {
        try {
            const metadata = this.getMetadata();
            return Object.values(metadata.entries).reduce((total, entry) => total + (entry.size || 0), 0);
        } catch {
            return 0;
        }
    },
    
    // Get cache statistics
    getStats() {
        try {
            const metadata = this.getMetadata();
            return {
                entryCount: Object.keys(metadata.entries).length,
                totalSize: this.getStorageSize(),
                oldestEntry: Math.min(...Object.values(metadata.entries).map(e => e.timestamp || Date.now())),
                newestEntry: Math.max(...Object.values(metadata.entries).map(e => e.timestamp || 0))
            };
        } catch {
            return {
                entryCount: 0,
                totalSize: 0,
                oldestEntry: Date.now(),
                newestEntry: 0
            };
        }
    },
    
    // Clear all cache data
    clearAll() {
        try {
            const metadata = this.getMetadata();
            Object.keys(metadata.entries).forEach(key => {
                const fullKey = this.CACHE_PREFIX + key;
                localStorage.removeItem(fullKey);
            });
            localStorage.removeItem(this.METADATA_KEY);
        } catch {
            // Silently fail
        }
    }
};

// Auto-cleanup expired entries on page load
document.addEventListener('DOMContentLoaded', () => {
    // Clear expired entries
    window.browserCache.clearExpired();
    
    // Clean up if storage is getting full (run cleanup every 5 minutes)
    setInterval(() => {
        const stats = window.browserCache.getStats();
        if (stats.totalSize > 4 * 1024 * 1024) { // If over 4MB
            window.browserCache.clearLRU(10);
        }
        window.browserCache.clearExpired();
    }, 5 * 60 * 1000);
});

export { browserCache };