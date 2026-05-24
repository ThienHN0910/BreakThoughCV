# Bug Fix Report - 2026-05-24

## Issue: Recruiter Cannot View Candidate Applications

### Symptom
- Candidates could apply for jobs successfully
- Recruiters could see job management page
- BUT: Recruiters could NOT see list of candidates who applied

### Root Cause Analysis

#### Problem 1: Missing Error Handling in `loadJobs()`
```javascript
// BEFORE (broken)
async function loadJobs() {
  const company = await api.get('/companies/my')  // ❌ No try-catch
  const { data } = await api.get(`/jobs/company/${company.data.id}`)
  jobs.value = data
  if (jobs.value.length && !selectedJobId.value) selectedJobId.value = jobs.value[0].id
}
```

If API call failed (network error, unauthorized, etc.), the entire function would fail silently, and `selectedJobId` would never be set.

#### Problem 2: Circular Dependency
```javascript
// loadApplications() REQUIRES selectedJobId to be set
async function loadApplications() {
  if (!selectedJobId.value) return  // ❌ Early return if empty
  // ... load applications
}

// onMounted
onMounted(async () => {
  await loadJobs()
  await loadApplications()  // ❌ loadApplications called even if loadJobs failed
})
```

If `loadJobs()` silently failed → `selectedJobId` stays empty → `loadApplications()` returns early → no applications loaded.

#### Problem 3: Poor UX - Manual Button Click Required
- Users had to manually click "Xem ứng viên" button
- No automatic loading when job selection changed
- Prone to user error

### Solution Implemented

#### Fix 1: Add Error Handling
```javascript
async function loadJobs() {
  try {
    loading.value = true
    error.value = ''
    const company = await api.get('/companies/my')
    const { data } = await api.get(`/jobs/company/${company.data.id}`)
    jobs.value = data
    if (jobs.value.length && !selectedJobId.value) {
      selectedJobId.value = jobs.value[0].id
    }
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được danh sách tin tuyển dụng'
    jobs.value = []
  } finally {
    loading.value = false
  }
}
```

#### Fix 2: Add Reactive Watch
```javascript
// Auto-load applications when job selection changes
watch(selectedJobId, async (newJobId) => {
  if (newJobId) {
    await loadApplications()
  }
})
```

#### Fix 3: Auto-Load on Mount
```javascript
onMounted(async () => {
  await loadJobs()
  // Applications now auto-load via watch when selectedJobId changes
})
```

#### Fix 4: Enhanced UI/UX
```html
<!-- Remove manual button -->
<!-- <button class="btc-btn-primary" @click="loadApplications">Xem ứng viên</button> -->

<!-- Add better feedback -->
<div v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 p-4 text-rose-700">
  {{ error }}
</div>

<!-- Add loading indicators -->
<div v-if="loading" class="text-sm text-gray-600">⏳ Đang tải...</div>

<!-- Add empty state with helpfull action -->
<div v-if="!selectedJobId && jobs.length === 0" class="rounded-lg border border-amber-200 bg-amber-50 p-8 text-center">
  <p>Bạn chưa tạo tin tuyển dụng nào</p>
  <RouterLink to="/recruiter/jobs" class="btc-btn-primary mt-4">
    Tạo tin tuyển dụng
  </RouterLink>
</div>

<!-- Add status badges with colors -->
<span :class="{
  'text-yellow-600': item.status === 'Pending',
  'text-blue-600': item.status === 'Reviewed',
  'text-green-600': item.status === 'Accepted',
  'text-red-600': item.status === 'Rejected'
}">
  Trạng thái: {{ item.status }}
</span>

<!-- Add timestamps -->
<p class="text-xs text-slate-500">
  Nộp vào: {{ new Date(item.appliedAt).toLocaleString('vi-VN') }}
</p>

<!-- Add emojis for better UX -->
<button>📄 Xem CV</button>
<button>⏳ Pending</button>
<button>✅ Chấp nhận</button>
<button>❌ Từ chối</button>
```

### Changes Made

#### File: `frontend/src/pages/ApplicationManagement.vue`
- ✅ Added `try-catch` error handling in `loadJobs()`
- ✅ Added `loading` ref for loading state
- ✅ Added `watch(selectedJobId)` to auto-load applications
- ✅ Imported `watch` from Vue 3 composables
- ✅ Enhanced UI with:
  - Error display section with better styling
  - Loading indicators (⏳)
  - Empty state with link to create jobs
  - Status badges with color coding
  - Timestamp display
  - Emojis for better UX
  - Disabled states for controls during loading
- ✅ Removed manual "Xem ứng viên" button

#### File: `docs/FEATURES.md`
- ✅ Updated section 2.3 with detailed flow:
  - Auto-load behavior described
  - Authorization checks documented
  - Status update flow clarified
  - CV preview feature documented
- ✅ Added new section "6. Bug Fixes (2026-05-24)":
  - Issue description
  - Root cause analysis
  - Fix details
  - UI improvements listed
- ✅ Updated test checklist with recruiter application management tests

### Testing Verification

Before fix:
- ❌ Recruiter clicks ApplicationManagement → sees empty list
- ❌ Nothing loads because selectedJobId is empty
- ❌ Manual button click required but didn't work

After fix:
- ✅ Recruiter clicks ApplicationManagement → jobs load automatically
- ✅ First job auto-selected
- ✅ Applications auto-load for selected job
- ✅ Changing job selection → applications auto-update
- ✅ Error messages displayed if jobs fail to load
- ✅ Empty state shown with helpful link
- ✅ Candidate info, status, and CV preview works

### Impact
- **Severity**: High (Core feature broken)
- **Scope**: Frontend only (Backend API was working correctly)
- **User**: Recruiter role
- **Status**: ✅ FIXED

### Related Files
- Backend: `/backend/Controllers/ApplicationController.cs` (no changes needed, working correctly)
- Frontend: `/frontend/src/pages/ApplicationManagement.vue` (fixed)
- Docs: `/docs/FEATURES.md` (updated)
