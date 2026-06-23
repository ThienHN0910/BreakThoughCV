<script setup>
import { ref, onMounted, computed, watch, onBeforeUnmount, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import CircleScore from '../components/CircleScore.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useNotificationsStore } from '../stores/notifications'

const auth = useAuthStore()
const notifications = useNotificationsStore()
const route = useRoute()
const router = useRouter()

const cvUrl = ref('')
const showCvPreview = ref(false)
const cvPreviewBlobUrl = ref('')
const cvPreviewLoading = ref(false)
const cvPreviewError = ref('')
const jobs = ref([])
const selectedJobId = ref('')
const selectedJob = ref(null)
const review = ref(null)
const loading = ref(false)
const error = ref('')
const actionRequired = ref('') // 'BUY_AI' | 'UPLOAD_CV' | ''
const suggestions = ref([])
const lastReviewedJobId = ref('')
const reviewResultsRef = ref(null)

const reviewHistory = ref([])
const reviewHistoryLoading = ref(false)
const reviewHistoryError = ref('')
const selectedHistoryId = ref('')
const expandedHistoryMap = ref({})

function isHistoryExpanded(id) {
  if (!id) return false
  return Boolean(expandedHistoryMap.value?.[id])
}

function toggleHistoryExpanded(id) {
  if (!id) return
  const next = { ...(expandedHistoryMap.value || {}) }
  next[id] = !next[id]
  expandedHistoryMap.value = next
}

async function scrollToReviewResults() {
  await nextTick()
  try {
    reviewResultsRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  } catch {
  }
}

async function openAndToggleHistory(item) {
  const id = item?.id
  if (!id) return

  // If collapsed → open review + expand
  if (!isHistoryExpanded(id)) {
    openHistoryItem(item)
    // Only keep one expanded at a time
    expandedHistoryMap.value = { [id]: true }
    await scrollToReviewResults()
    return
  }

  // If expanded → collapse only
  expandedHistoryMap.value = {}
}

const aiAccessEnabled = computed(() => Boolean(auth.user?.aiAccessEnabled))

const applications = ref([])
const applicationsLoading = ref(false)
const applicationsError = ref('')
const cancelingApplicationIds = ref(new Set())

function formatDate(value) {
  try {
    return new Date(value).toLocaleString()
  } catch {
    return value
  }
}

const loadMyApplications = async () => {
  try {
    applicationsLoading.value = true
    applicationsError.value = ''
    const { data } = await api.get('/applications/my')
    const nextApplications = data || []
    applications.value = nextApplications

    const userKey = auth.user?.userId || auth.user?.email || ''
    const statusKey = userKey ? `applicationStatus:${userKey}` : ''
    if (statusKey) {
      let prevMap = {}
      try {
        const raw = localStorage.getItem(statusKey)
        prevMap = raw ? JSON.parse(raw) : {}
      } catch {
        prevMap = {}
      }

      const nextMap = {}
      for (const a of nextApplications) {
        if (!a?.id) continue
        const id = String(a.id)
        const nextStatus = a.status || ''
        nextMap[id] = nextStatus

        const prevStatus = prevMap?.[id]
        if (prevStatus && prevStatus !== nextStatus) {
          const type = nextStatus === 'Accepted' ? 'success' : nextStatus === 'Rejected' ? 'warning' : 'info'
          const jobLabel = a.jobTitle || a.jobName || a.jobId || 'Hồ sơ ứng tuyển'
          notifications.add({
            type,
            title: 'Cập nhật hồ sơ ứng tuyển',
            message: `${jobLabel}: ${prevStatus} → ${nextStatus}`,
            href: '/candidate/ai-review'
          })
        }
      }

      try {
        localStorage.setItem(statusKey, JSON.stringify(nextMap))
      } catch {
      }
    }
  } catch (e) {
    applicationsError.value = e?.response?.data?.message || 'Không tải được danh sách việc đã apply'
    applications.value = []
  } finally {
    applicationsLoading.value = false
  }
}

const cancelApplication = async (app) => {
  if (!app?.id) return

  try {
    applicationsError.value = ''
    cancelingApplicationIds.value = new Set([...cancelingApplicationIds.value, app.id])
    await api.delete(`/applications/${app.id}`)
    await loadMyApplications()
  } catch (e) {
    applicationsError.value = e?.response?.data?.message || 'Không hủy được apply'
  } finally {
    const next = new Set(cancelingApplicationIds.value)
    next.delete(app.id)
    cancelingApplicationIds.value = next
  }
}

const wantedJobIds = ref([])
const reviewedJobIds = ref([])
const applyingWantedJobIds = ref(new Set())
const wantedActionError = ref('')

function wantedStorageKey() {
  const userKey = auth.user?.userId || auth.user?.email || ''
  return `aiReviewJobIds:${userKey}`
}

function reviewedStorageKey() {
  const userKey = auth.user?.userId || auth.user?.email || ''
  return `aiReviewedJobIds:${userKey}`
}

function loadWantedJobIds() {
  if (auth.role !== 'candidate') {
    wantedJobIds.value = []
    return
  }

  try {
    const raw = localStorage.getItem(wantedStorageKey())
    const parsed = raw ? JSON.parse(raw) : []
    wantedJobIds.value = Array.isArray(parsed) ? parsed : []
  } catch {
    wantedJobIds.value = []
  }
}

function persistWantedJobIds(nextIds) {
  wantedJobIds.value = nextIds
  try {
    localStorage.setItem(wantedStorageKey(), JSON.stringify(nextIds))
  } catch {
  }
}

function loadReviewedJobIds() {
  if (auth.role !== 'candidate') {
    reviewedJobIds.value = []
    return
  }

  try {
    const raw = localStorage.getItem(reviewedStorageKey())
    const parsed = raw ? JSON.parse(raw) : []
    reviewedJobIds.value = Array.isArray(parsed) ? parsed : []
  } catch {
    reviewedJobIds.value = []
  }
}

function persistReviewedJobIds(nextIds) {
  reviewedJobIds.value = nextIds
  try {
    localStorage.setItem(reviewedStorageKey(), JSON.stringify(nextIds))
  } catch {
  }
}

function markJobReviewed(jobId) {
  if (!jobId) return
  const normalized = String(jobId)
  const nextSet = new Set((reviewedJobIds.value || []).map(String))
  nextSet.add(normalized)
  persistReviewedJobIds(Array.from(nextSet))
}

function hasReviewedJob(jobId) {
  if (!jobId) return false
  const normalized = String(jobId)
  if (review.value && String(lastReviewedJobId.value) === normalized) return true
  return (reviewedJobIds.value || []).map(String).includes(normalized)
}

function removeWantedJob(jobId) {
  const next = (wantedJobIds.value || []).filter(id => id !== jobId)
  persistWantedJobIds(next)

  if (selectedJobId.value === jobId) {
    selectedJobId.value = ''
    selectedJob.value = null
  }
}

const applyWantedJob = async (job) => {
  if (!job?.id) return

  wantedActionError.value = ''

  // Only allow applying after AI review for this exact job (cached across logins)
  if (!hasReviewedJob(job.id)) {
    wantedActionError.value = 'Hãy AI review job này trước khi apply.'
    return
  }

  try {
    applyingWantedJobIds.value = new Set([...applyingWantedJobIds.value, job.id])
    await api.post('/applications/quick', { jobId: job.id })
    removeWantedJob(job.id)
    await loadMyApplications()
  } catch (e) {
    wantedActionError.value = e?.response?.data?.message || 'Apply thất bại'
  } finally {
    const next = new Set(applyingWantedJobIds.value)
    next.delete(job.id)
    applyingWantedJobIds.value = next
  }
}

const wantedJobs = computed(() => {
  if (!wantedJobIds.value.length) return []
  const idSet = new Set(wantedJobIds.value)
  return (jobs.value || []).filter(j => idSet.has(j.id))
})

function selectWantedJob(job) {
  selectedJobId.value = job.id
  onJobSelected()
}

const loadMyCv = async () => {
  try {
    const { data } = await api.get('/cv/my')
    cvUrl.value = data.cvUrl || ''
  } catch (e) {
    cvUrl.value = ''
    console.error('Failed to load CV:', e)
  }
}

const ensureAiAccessAndCv = async () => {
  error.value = ''
  actionRequired.value = ''

  try {
    await auth.refreshMe()
  } catch {
  }

  if (!auth.user?.aiAccessEnabled) {
    error.value = 'Bạn cần mua Gói cước AI để sử dụng AI Review.'
    actionRequired.value = 'BUY_AI'
    return false
  }

  await loadMyCv()
  if (!cvUrl.value) {
    error.value = 'Bạn cần upload CV trước khi dùng AI Review.'
    actionRequired.value = 'UPLOAD_CV'
    return false
  }

  return true
}

function revokeCvPreviewUrl() {
  if (cvPreviewBlobUrl.value) {
    try {
      URL.revokeObjectURL(cvPreviewBlobUrl.value)
    } catch {
    }
  }
  cvPreviewBlobUrl.value = ''
}

const loadCvPreview = async () => {
  if (!auth.user?.userId) return
  if (!cvUrl.value) return
  if (cvPreviewLoading.value) return
  if (cvPreviewBlobUrl.value) return

  cvPreviewLoading.value = true
  cvPreviewError.value = ''

  try {
    const resp = await api.get(`/cv/preview/${auth.user.userId}`, { responseType: 'blob' })
    const blob = resp.data instanceof Blob
      ? resp.data
      : new Blob([resp.data], { type: 'application/pdf' })
    cvPreviewBlobUrl.value = URL.createObjectURL(blob)
  } catch (e) {
    cvPreviewError.value = e?.response?.data?.message || 'Không tải được CV'
    revokeCvPreviewUrl()
  } finally {
    cvPreviewLoading.value = false
  }
}

const loadJobs = async () => {
  try {
    const { data } = await api.get('/jobs?page=1&pageSize=100')
    jobs.value = data.data || []
  } catch (e) {
    console.error('Failed to load jobs:', e)
  }
}

const onJobSelected = async (arg) => {
  const preserveReview = Boolean(arg && typeof arg === 'object' && arg.preserveReview === true)
  const job = jobs.value.find(j => j.id === selectedJobId.value)
  selectedJob.value = job || null

  // prevent applying with stale review result
  if (!preserveReview) {
    review.value = null
    selectedHistoryId.value = ''
    error.value = ''
  }
}

const loadReviewHistory = async () => {
  if (auth.role !== 'candidate') {
    reviewHistory.value = []
    return
  }

  try {
    reviewHistoryLoading.value = true
    reviewHistoryError.value = ''
    const { data } = await api.get('/ai/review-history')
    const next = Array.isArray(data) ? data : []
    reviewHistory.value = next

    // Sync reviewed job ids from server history into local cache
    const historyJobIds = next
      .map(r => r?.jobId)
      .filter(Boolean)
      .map(String)

    if (historyJobIds.length) {
      const nextSet = new Set((reviewedJobIds.value || []).map(String))
      for (const id of historyJobIds) nextSet.add(id)
      persistReviewedJobIds(Array.from(nextSet))
    }
  } catch (e) {
    if (e?.response?.status === 402) {
      reviewHistoryError.value = e?.response?.data?.message || 'Vui lòng thanh toán để sử dụng AI.'
      actionRequired.value = 'BUY_AI'
    } else {
      reviewHistoryError.value = e?.response?.data?.message || 'Không tải được lịch sử AI review'
    }
    reviewHistory.value = []
  } finally {
    reviewHistoryLoading.value = false
  }
}

function openHistoryItem(item) {
  if (!item) return
  selectedHistoryId.value = item.id || ''

  if (item.jobId) {
    // Keep dropdown stable: only set selectedJobId if the job is currently in "wanted" list
    if ((wantedJobIds.value || []).includes(item.jobId)) {
      selectedJobId.value = item.jobId
    }
    selectedJob.value = (jobs.value || []).find(j => j.id === item.jobId) || null
  }

  review.value = {
    score: item.score,
    missingKeywords: item.missingKeywords || [],
    criticalFixes: item.criticalFixes || [],
    tailoredSuggestions: item.tailoredSuggestions || [],
    createdAt: item.createdAt,
    jobId: item.jobId,
    jobTitle: item.jobTitle,
    id: item.id
  }
  lastReviewedJobId.value = item.jobId || ''
  if (item.jobId) markJobReviewed(item.jobId)
}

const reviewAgainFromHistory = async (item) => {
  if (!item?.jobId) return

  // Set selected job for the API call (dropdown may remain unchanged)
  selectedJobId.value = item.jobId
  selectedJob.value = (jobs.value || []).find(j => j.id === item.jobId) || null
  selectedHistoryId.value = item.id || ''

  // Fresh review
  review.value = null
  error.value = ''
  await reviewCv()

  // Reload history so the newest review appears on top
  await loadReviewHistory()
  await scrollToReviewResults()
}

const suggestJobs = async () => {
  const ok = await ensureAiAccessAndCv()
  if (!ok) return

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/suggest-jobs', { cvUrl: cvUrl.value })
    suggestions.value = data.suggestions || []
  } catch (e) {
    if (e?.response?.status === 402) {
      error.value = e?.response?.data?.message || 'Vui lòng thanh toán để sử dụng AI.'
      actionRequired.value = 'BUY_AI'
    } else {
      error.value = e?.response?.data?.message || 'Unable to suggest jobs'
    }
  } finally {
    loading.value = false
  }
}

const reviewCv = async () => {
  const ok = await ensureAiAccessAndCv()
  if (!ok) return

  if (!selectedJobId.value) {
    error.value = 'Please select a job'
    return
  }

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/review-cv', { jobId: selectedJobId.value, cvUrl: cvUrl.value })
    review.value = {
      ...(data || {}),
      jobId: selectedJobId.value,
      jobTitle: selectedJob.value?.title || null
    }
    lastReviewedJobId.value = selectedJobId.value
    markJobReviewed(selectedJobId.value)
  } catch (e) {
    if (e?.response?.status === 402) {
      error.value = e?.response?.data?.message || 'Vui lòng thanh toán để sử dụng AI.'
      actionRequired.value = 'BUY_AI'
    } else {
      error.value = e?.response?.data?.message || 'Unable to review CV'
    }
  } finally {
    loading.value = false
  }
}


onMounted(async () => {
  try {
    await auth.refreshMe()
  } catch {
  }

  loadMyCv()
  loadJobs()
  loadMyApplications()
  loadWantedJobIds()
  loadReviewedJobIds()
  loadReviewHistory()
})

watch(showCvPreview, (next) => {
  if (next) loadCvPreview()
})

onBeforeUnmount(() => {
  revokeCvPreviewUrl()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">AI Review CV theo JD</h2>
    <p class="btc-page-subtitle">Upload CV để nhận gợi ý chỉnh sửa và điểm phù hợp với vị trí ứng tuyển.</p>

    <!-- Wanted Jobs (AI review first) -->
    <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Việc bạn muốn apply (AI review trước)</h3>
      <p v-if="wantedActionError" class="text-sm text-rose-600 mb-3">{{ wantedActionError }}</p>
      <p v-if="!wantedJobIds.length" class="text-sm text-slate-600">Chưa thêm job nào từ trang Việc làm.</p>
      <div v-else>
        <p v-if="!wantedJobs.length" class="text-sm text-slate-600">Đang tải danh sách job...</p>
        <div v-else class="space-y-3">
          <div v-for="j in wantedJobs" :key="j.id" class="border border-slate-200 rounded-lg p-4">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div>
                <div class="flex flex-wrap items-center gap-2">
                  <p class="font-semibold">{{ j.title }}</p>
                  <span
                    v-if="hasReviewedJob(j.id)"
                    class="inline-flex items-center rounded-full border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700"
                  >
                    AI đã review
                  </span>
                </div>
                <p class="text-sm text-slate-600">{{ j.companyName }} • {{ j.categoryName || 'Chưa phân loại' }}</p>
              </div>
              <div class="flex items-center gap-2">
                <button v-if="!hasReviewedJob(j.id)" class="btc-btn-secondary" @click="selectWantedJob(j)">Review job này</button>
                <button
                  class="btc-btn-primary"
                  :disabled="!hasReviewedJob(j.id) || applyingWantedJobIds.has(j.id)"
                  @click="applyWantedJob(j)"
                >
                  {{ applyingWantedJobIds.has(j.id) ? 'Đang apply...' : 'Apply' }}
                </button>
                <button class="btc-btn-secondary border-rose-200 text-rose-700" @click="removeWantedJob(j.id)">Hủy</button>
              </div>
            </div>
            <p v-if="!hasReviewedJob(j.id)" class="text-xs text-slate-600 mt-2">(Cần review job này trước khi apply)</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Applied Jobs List -->
    <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Việc bạn đã apply</h3>
      <p v-if="applicationsError" class="text-sm text-rose-600 mb-3">{{ applicationsError }}</p>
      <p v-if="applicationsLoading" class="text-sm">Đang tải...</p>
      <div v-else>
        <p v-if="!applications.length" class="text-sm text-slate-600">Chưa có job nào được apply.</p>
        <div v-else class="space-y-3">
          <div v-for="a in applications" :key="a.id" class="border border-slate-200 rounded-lg p-4">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p class="font-semibold">{{ a.jobTitle }}</p>
                <p class="text-sm text-slate-600">Apply lúc: {{ formatDate(a.appliedAt) }}</p>
              </div>
              <div class="flex items-center gap-3">
                <div class="text-sm">
                  Trạng thái: <span class="font-medium">{{ a.status }}</span>
                </div>
                <button
                  class="btc-btn-secondary border-rose-200 text-rose-700"
                  :disabled="cancelingApplicationIds.has(a.id)"
                  @click="cancelApplication(a)"
                >
                  {{ cancelingApplicationIds.has(a.id) ? 'Đang hủy...' : 'Hủy apply' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Job Selection Panel -->
    <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-2xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Choose Job Position</h3>
      <p v-if="!wantedJobIds.length" class="text-sm text-slate-600 mb-3">
        Hãy thêm job ở trang Việc làm bằng nút “Muốn apply (AI review trước)” để chọn tại đây.
      </p>
      <select v-model="selectedJobId" @change="onJobSelected" class="btc-input w-full mb-4">
        <option value="">Select a job to review against</option>
        <option v-for="job in wantedJobs" :key="job.id" :value="job.id">
          {{ job.title }} - {{ job.companyName || job.companyId }}
        </option>
      </select>

      <!-- Selected Job Info -->
      <div v-if="selectedJob" class="bg-blue-50 border border-blue-200 rounded-lg p-4 mt-4">
        <h4 class="font-semibold text-blue-900 mb-2">{{ selectedJob.title }}</h4>
        <p class="text-sm text-blue-800 mb-3">{{ selectedJob.description }}</p>
        <div class="space-y-2 text-sm">
          <p><strong>Experience:</strong> {{ selectedJob.minExperienceYears }}+ years</p>
          <p v-if="selectedJob.mustHaveSkills?.length">
            <strong>Required Skills:</strong> {{ selectedJob.mustHaveSkills.join(', ') }}
          </p>
        </div>
      </div>
    </div>

    <!-- CV Preview -->
    
    <div v-if="!aiAccessEnabled" class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-2xl mb-6">
      <h3 class="text-lg font-semibold mb-2">Bạn chưa có quyền AI</h3>
      <p class="text-sm text-slate-600">Vui lòng mua gói AI ở trang “Gói AI đã mua” để sử dụng AI Review.</p>
      <div class="mt-4">
        <button class="btc-btn-primary" type="button" @click="router.push('/candidate/ai-purchases')">
          Mở trang gói AI đã mua
        </button>
      </div>
    </div>

    <!-- Action Buttons -->
    <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-2xl mb-6">
      <div class="flex gap-3 flex-wrap">
        <button
          class="btc-btn-secondary"
          type="button"
          :disabled="loading"
          @click="suggestJobs"
        >
          {{ loading ? 'Analyzing...' : 'Get Job Suggestions' }}
        </button>
        <button
          class="btc-btn-primary"
          type="button"
          :disabled="loading"
          @click="reviewCv"
        >
          {{ loading ? 'Reviewing...' : 'Review CV for This Job' }}
        </button>
      </div>
      <p v-if="error" class="text-sm text-rose-600 mt-3">{{ error }}</p>
      <div v-if="actionRequired" class="mt-3">
        <button
          v-if="actionRequired === 'BUY_AI'"
          class="btc-btn-primary"
          type="button"
          @click="router.push('/candidate/ai-purchases')"
        >
          Mua gói AI
        </button>
        <button
          v-else-if="actionRequired === 'UPLOAD_CV'"
          class="btc-btn-primary"
          type="button"
          @click="router.push('/candidate/cv')"
        >
          Upload CV
        </button>
      </div>
    </div>

    <!-- Job Suggestions -->
    <div v-if="suggestions.length" class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl mb-6">
      <h3 class="font-semibold mb-4">AI Job Recommendations</h3>
      <div class="space-y-2">
        <div v-for="(s, idx) in suggestions" :key="idx" class="bg-green-50 border border-green-200 p-3 rounded-lg">
          <p class="text-sm text-green-900">{{ idx + 1 }}. {{ s.jobId }} - {{ s.reason }}</p>
        </div>
      </div>
    </div>

    <!-- AI Review History -->
    <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Lịch sử AI review</h3>
      <p v-if="reviewHistoryError" class="text-sm text-rose-600 mb-3">{{ reviewHistoryError }}</p>
      <p v-if="reviewHistoryLoading" class="text-sm">Đang tải...</p>
      <div v-else>
        <p v-if="!reviewHistory.length" class="text-sm text-slate-600">Chưa có lần AI review nào.</p>
        <div v-else class="space-y-3">
          <div
            v-for="r in reviewHistory"
            :key="r.id"
            class="border border-slate-200 rounded-lg"
            :class="r.id === selectedHistoryId ? 'bg-slate-50' : ''"
          >
            <div class="p-4">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <p class="font-semibold">
                    {{ r.jobTitle || 'Job' }}
                    <span class="text-xs text-slate-500" v-if="r.jobId">({{ r.jobId }})</span>
                  </p>
                  <p class="text-sm text-slate-600">
                    Review lúc: {{ formatDate(r.createdAt) }} • Score: <span class="font-medium">{{ r.score }}</span>
                  </p>
                </div>
                <div class="flex items-center gap-2">
                  <button
                    class="btc-btn-secondary"
                    type="button"
                    @click="openAndToggleHistory(r)"
                  >
                    {{ isHistoryExpanded(r.id) ? 'Ẩn' : 'Hiện' }}
                  </button>
                  <button
                    class="btc-btn-primary"
                    type="button"
                    :disabled="loading"
                    @click="reviewAgainFromHistory(r)"
                  >
                    Review lại
                  </button>
                </div>
              </div>
            </div>

            <div v-if="isHistoryExpanded(r.id)" class="border-t border-slate-200 p-4">
              <p class="text-sm text-slate-600">
                Đang hiển thị kết quả ở phần AI Review bên dưới.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Review Results -->
    <div v-if="review" ref="reviewResultsRef" class="mt-6 space-y-6">
      
      <!-- Score Card -->  
      <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-2xl">
        <h3 class="text-lg font-semibold mb-4">Review Score</h3>
        <div class="flex items-center gap-8">
          <CircleScore :score="review.score" />
          <div class="flex-1">
            <h4 class="font-semibold mb-3">Missing Keywords</h4>
            <div class="flex flex-wrap gap-2">
              <span v-for="k in review.missingKeywords" :key="k" class="inline-flex items-center gap-1.5 bg-red-50 text-red-600 px-3 py-1.5 rounded-lg text-sm font-medium border border-red-100">
                <svg class="w-3.5 h-3.5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
                {{ k }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Suggestions -->
      <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl">
        <h3 class="font-semibold mb-4">AI Suggestions</h3>
        <div class="space-y-4">
          <div v-for="(s, idx) in review.tailoredSuggestions" :key="idx" class="border border-slate-100 rounded-2xl overflow-hidden shadow-sm bg-white mb-4">
            <div class="bg-slate-50 px-5 py-3 font-semibold text-slate-700 border-b border-slate-100 flex items-center gap-2">
              <svg class="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>
              {{ s.section }}
            </div>
            <div class="grid md:grid-cols-2 gap-4 p-5">
              <div class="rounded-xl bg-slate-50 p-4 border border-slate-100">
                <div class="flex items-center gap-2 mb-3">
                  <div class="w-2 h-2 rounded-full bg-slate-400"></div>
                  <p class="text-sm font-semibold text-slate-600">Bản cũ</p>
                </div>
                <p class="text-sm text-slate-600 whitespace-pre-wrap leading-relaxed">{{ s.originalText }}</p>
              </div>
              <div class="rounded-xl bg-emerald-50/40 p-4 border border-emerald-100 shadow-sm">
                <div class="flex items-center gap-2 mb-3">
                  <div class="w-2 h-2 rounded-full bg-emerald-500"></div>
                  <p class="text-sm font-semibold text-emerald-700">Gợi ý mới (AI)</p>
                </div>
                <p class="text-sm text-slate-800 whitespace-pre-wrap leading-relaxed">{{ s.suggestedText }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

<!-- Summary (moved from History) -->
      <div class="bg-white shadow-sm rounded-2xl border border-slate-100 p-6 max-w-4xl">
        <h3 class="text-lg font-semibold mb-2">Tóm tắt</h3>
        <p class="text-sm text-slate-600">
          {{ review.jobTitle || selectedJob?.title || 'Job' }}
          <span v-if="review.createdAt"> • Review lúc: {{ formatDate(review.createdAt) }}</span>
          <span v-if="typeof review.score === 'number'"> • Score: <span class="font-medium">{{ review.score }}</span></span>
        </p>
        <p class="text-sm text-slate-600 mt-2">
          Missing keywords: <span class="font-medium">{{ (review.missingKeywords || []).length }}</span>
          • Critical fixes: <span class="font-medium">{{ (review.criticalFixes || []).length }}</span>
          • Suggestions: <span class="font-medium">{{ (review.tailoredSuggestions || []).length }}</span>
        </p>
        <div v-if="(review.missingKeywords || []).length" class="flex flex-wrap gap-2 mt-3">
          <span
            v-for="k in (review.missingKeywords || []).slice(0, 12)"
            :key="k"
            class="inline-flex items-center gap-1 bg-red-50 text-red-600 px-2 py-1 rounded-md text-xs font-medium border border-red-100"
          >
            <svg class="w-3 h-3 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            {{ k }}
          </span>
          <span
            v-if="(review.missingKeywords || []).length > 12"
            class="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-slate-100 text-slate-600"
          >
            +{{ (review.missingKeywords || []).length - 12 }}
          </span>
        </div>
      </div>
    </div>
  </AppLayout>
</template>

<style scoped>
.max-w-2xl {
  max-width: 42rem;
}

.max-w-4xl {
  max-width: 56rem;
}

.mb-6 {
  margin-bottom: 1.5rem;
}

.mb-4 {
  margin-bottom: 1rem;
}

.p-4 {
  padding: 1rem;
}

.px-3 {
  padding-left: 0.75rem;
  padding-right: 0.75rem;
}

.py-1 {
  padding-top: 0.25rem;
  padding-bottom: 0.25rem;
}

.gap-3 {
  gap: 0.75rem;
}

.gap-8 {
  gap: 2rem;
}

.flex {
  display: flex;
}

.flex-wrap {
  flex-wrap: wrap;
}

.items-center {
  align-items: center;
}

.flex-1 {
  flex: 1;
}

.space-y-1 > * + * {
  margin-top: 0.25rem;
}

.space-y-2 > * + * {
  margin-top: 0.5rem;
}

.space-y-4 > * + * {
  margin-top: 1rem;
}

.rounded {
  border-radius: 0.25rem;
}

.rounded-lg {
  border-radius: 0.5rem;
}

.w-full {
  width: 100%;
}
</style>
