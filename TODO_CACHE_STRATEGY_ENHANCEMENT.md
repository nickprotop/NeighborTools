# TODO: Cache Strategy Enhancement

## Overview
Improve the service worker cache system with automatic update type detection and intelligent cache invalidation strategies.

## Current State
- **Manual versioning**: Hard-coded `CACHE_VERSION = '1.11.0'`
- **Manual bump scripts**: `bump-cache-version.sh` with semantic versioning
- **Fixed intervals**: 60-second update checks
- **Basic update notification**: Simple popup for all updates
- **No automatic classification**: All updates treated the same

## Proposed Enhancements

### 1. Automatic Update Type Detection
Implement intelligent detection of update severity based on:
- Git commit analysis
- File change patterns (API vs frontend vs framework)
- Conventional commit messages
- Build metadata integration

### 2. Dynamic Update Strategies
Different update handling based on severity:
- **Critical**: Immediate overlay with progress
- **Major**: Notification with recommended update
- **Minor**: Background update with optional notification
- **Patch**: Silent background update

### 3. Intelligent Cache Invalidation
- Progressive cache clearing based on what changed
- Pre-loading critical resources during updates
- Staged cache refresh to prevent version conflicts

### 4. Dynamic Update Intervals
Replace fixed 60-second intervals with:
- **Critical updates**: 30 seconds
- **Major updates**: 1 minute
- **Minor updates**: 5 minutes
- **Patch updates**: 15 minutes

## Implementation Options

### Phase 1: Enhanced Bump Script (Easy - 2-4 hours)
```bash
# Auto-detect update type based on git changes
function auto_detect_update_type() {
    local api_changes=$(git diff --name-only HEAD~1 | grep -E "\.(cs|csproj)$" | wc -l)
    local blazor_changes=$(git diff --name-only HEAD~1 | grep -E "\.razor$" | wc -l)
    local framework_changes=$(git diff --name-only HEAD~1 | grep -E "(_framework|\.dll)" | wc -l)
    
    if [[ $framework_changes -gt 0 ]]; then
        echo "major"
    elif [[ $api_changes -gt 0 ]] || [[ $blazor_changes -gt 0 ]]; then
        echo "minor"
    else
        echo "patch"
    fi
}
```

### Phase 2: MSBuild Integration (Medium - 4-8 hours)
```xml
<!-- Auto-generate version metadata during build -->
<Target Name="GenerateUpdateMetadata" BeforeTargets="Build">
    <WriteLinesToFile File="wwwroot/update-metadata.json" 
                      Lines='{"version": "$(Version)", "buildTime": "$([System.DateTime]::Now)", "autoDetected": true}' 
                      Overwrite="true" />
</Target>
```

### Phase 3: Service Worker Intelligence (Advanced - 1-2 days)
```javascript
// Smart update detection and handling
async function detectUpdateSeverity() {
    const metadata = await fetch('/update-metadata.json');
    return analyzeChanges(currentMetadata, metadata);
}

function getUpdateInterval(severity) {
    const intervals = {
        'critical': 30 * 1000,
        'major': 60 * 1000,
        'minor': 5 * 60 * 1000,
        'patch': 15 * 60 * 1000
    };
    return intervals[severity] || intervals.minor;
}
```

## Benefits
- **Improved UX**: Users see appropriate update prompts
- **Reduced interruptions**: Minor updates happen silently
- **Better reliability**: Prevents version conflicts
- **Automated workflow**: Less manual intervention required
- **Scalable**: Foundation for future PWA enhancements

## Effort Estimation
- **Phase 1**: 2-4 hours (immediate improvement)
- **Phase 2**: 4-8 hours (build integration)
- **Phase 3**: 1-2 days (full intelligence)

## Priority
**Medium Priority** - Enhances user experience but not critical for core functionality

## Dependencies
- Current service worker system (✅ exists)
- Git workflow (✅ exists)
- Build pipeline (✅ exists)

## Success Metrics
- Reduced user complaints about update interruptions
- Improved cache hit rates
- Faster update adoption for critical fixes
- Reduced support tickets related to version conflicts

## Implementation Strategy
1. **Start with Phase 1** - enhance existing bump script
2. **Add commit message analysis** for conventional commits
3. **Test with manual deployments** before automating
4. **Gradually implement phases 2-3** based on need

## Related Files
- `frontend/wwwroot/service-worker.js` - Main service worker
- `frontend/wwwroot/index.html` - Update notification UI
- `frontend/bump-cache-version.sh` - Version bumping script
- `frontend/bump-cache-version.bat` - Windows version

## Notes
- Consider implementing alongside automated deployment (TODO_AUTOMATED_CLOUD_DEPLOYMENT.md)
- May benefit from monitoring/observability features (TODO_ORCHESTRATION_OBSERVABILITY.md)
- Could be enhanced with user preferences for update behavior