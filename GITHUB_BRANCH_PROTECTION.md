# GitHub Branch Protection Setup for NeighborTools

## 🛡️ Preventing Direct Pushes with Secrets

To ensure GitLeaks blocks ALL commits (not just PRs), set up branch protection rules.

## 📋 Setup Instructions

### 1. Navigate to Branch Protection
1. Go to your GitHub repository
2. Click **Settings** tab
3. Click **Branches** in left sidebar
4. Click **Add rule** or edit existing rule

### 2. Configure Protection Rule

**Branch name pattern:** `master` (or `main`)

**Required Settings:**
- ✅ **Restrict pushes that create files**
- ✅ **Require status checks to pass before merging**
  - Search for: `🔍 Secret Detection` or `gitleaks`
  - ✅ Check the GitLeaks workflow
- ✅ **Require branches to be up to date before merging**
- ✅ **Include administrators** (optional but recommended)

**Advanced Settings:**
- ✅ **Restrict pushes that create files**
- ✅ **Require a pull request before merging**
  - Minimum 1 review required
  - ✅ **Dismiss stale reviews when new commits are pushed**
- ✅ **Do not allow bypassing the above settings**

### 3. Result After Setup

#### ✅ What Gets Blocked:
- Direct pushes to `master` with secrets → **BLOCKED**
- PRs with secrets → **CANNOT MERGE**
- Force pushes → **BLOCKED** (if enabled)

#### 🔄 Required Workflow:
1. Developer creates feature branch
2. Makes changes and commits (pre-commit hook scans)
3. Pushes to feature branch
4. Creates PR to `master`
5. GitLeaks workflow runs on PR
6. **If secrets found → PR cannot merge**
7. **If clean → PR can be merged**

## 🚀 Recommended Configuration

```yaml
# Complete protection setup
Branch: master
├── Require a pull request before merging ✅
│   ├── Required approving reviews: 1
│   ├── Dismiss stale reviews: ✅
│   └── Require review from code owners: ✅ (if CODEOWNERS exists)
├── Require status checks to pass ✅
│   ├── Require branches to be up to date: ✅
│   └── Status checks:
│       ├── 🔍 Secret Detection (GitLeaks) ✅
│       ├── Build / Test workflows ✅
│       └── Any other CI checks ✅
├── Restrict pushes that create files ✅
├── Include administrators ✅
└── Allow force pushes: ❌
```

## 🎯 Alternative: Admin-Only Direct Push

If you need emergency access:

```yaml
Branch: master
├── Require pull request: ✅
├── Required status checks: ✅ (including GitLeaks)
├── Include administrators: ❌  # Admins can bypass
└── Restrict pushes: ✅
```

This allows repository admins to push directly in emergencies while still requiring checks for regular developers.

## 🔍 Verification

After setup, test by:
1. Try pushing directly to `master` → Should be blocked
2. Create PR with test content → Should require GitLeaks check
3. Verify GitLeaks appears in required status checks

## 📊 Security Levels

| Setup | Direct Push Block | PR Block | Emergency Access |
|-------|------------------|----------|------------------|
| **No Protection** | ❌ | ❌ | ✅ |
| **Workflow Only** | ❌ | ✅* | ✅ |
| **Branch Protection** | ✅ | ✅ | ❌ |
| **Branch Protection + Admin Exception** | ✅ | ✅ | ✅ (Admin only) |

*Only if configured as required check

## 🆘 Emergency Procedures

If branch protection is too strict and you need emergency access:

### Temporary Bypass (Admin Only)
1. Go to Settings → Branches
2. Edit protection rule
3. Temporarily disable "Include administrators"
4. Make emergency push
5. **Re-enable protection immediately**

### Override Protection
```bash
# Admin emergency push (if protection allows)
git push origin master

# If blocked, temporarily disable protection in GitHub UI
```

⚠️ **Always re-enable protection after emergency access!**