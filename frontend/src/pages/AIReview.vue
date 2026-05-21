<script setup>
import { ref, onMounted, computed } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import CircleScore from '../components/CircleScore.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

const cvUrl = ref('')
const showCvPreview = ref(false)
const jobs = ref([])
const selectedJobId = ref('')
const selectedJob = ref(null)
const review = ref(null)
const loading = ref(false)
const error = ref('')
const suggestions = ref([])
const lastReviewedJobId = ref('')

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
    applications.value = data || []
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
const applyingWantedJobIds = ref(new Set())
const wantedActionError = ref('')

function wantedStorageKey() {
  const userId = auth.user?.userId || ''
  return `aiReviewJobIds:${userId}`
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

  // Only allow applying after AI review for this exact job
  if (!review.value || lastReviewedJobId.value !== job.id) {
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
    console.error('Failed to load CV:', e)
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

const onJobSelected = async () => {
  const job = jobs.value.find(j => j.id === selectedJobId.value)
  selectedJob.value = job || null

  // prevent applying with stale review result
  review.value = null
  error.value = ''
}

const suggestJobs = async () => {
  if (!cvUrl.value) {
    error.value = 'Please upload your CV first'
    return
  }

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/suggest-jobs', { cvUrl: cvUrl.value })
    suggestions.value = data.suggestions || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Unable to suggest jobs'
  } finally {
    loading.value = false
  }
}

const reviewCv = async () => {
  if (!selectedJobId.value) {
    error.value = 'Please select a job'
    return
  }

  if (!cvUrl.value) {
    error.value = 'Please upload your CV first'
    return
  }

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/review-cv', { jobId: selectedJobId.value, cvUrl: cvUrl.value })
    review.value = data
    lastReviewedJobId.value = selectedJobId.value
  } catch (e) {
    error.value = e?.response?.data?.message || 'Unable to review CV'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadMyCv()
  loadJobs()
  loadMyApplications()
  loadWantedJobIds()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">AI Review CV theo JD</h2>
    <p class="btc-page-subtitle">Upload CV để nhận gợi ý chỉnh sửa và điểm phù hợp với vị trí ứng tuyển.</p>

    <!-- Wanted Jobs (AI review first) -->
    <div class="btc-card max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Việc bạn muốn apply (AI review trước)</h3>
      <p v-if="wantedActionError" class="text-sm text-rose-600 mb-3">{{ wantedActionError }}</p>
      <p v-if="!wantedJobIds.length" class="text-sm text-slate-600">Chưa thêm job nào từ trang Việc làm.</p>
      <div v-else>
        <p v-if="!wantedJobs.length" class="text-sm text-slate-600">Đang tải danh sách job...</p>
        <div v-else class="space-y-3">
          <div v-for="j in wantedJobs" :key="j.id" class="border border-slate-200 rounded-lg p-4">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p class="font-semibold">{{ j.title }}</p>
                <p class="text-sm text-slate-600">{{ j.companyName }} • {{ j.categoryName || 'Chưa phân loại' }}</p>
              </div>
              <div class="flex items-center gap-2">
                <button class="btc-btn-secondary" @click="selectWantedJob(j)">Review job này</button>
                <button
                  class="btc-btn-primary"
                  :disabled="!review || lastReviewedJobId !== j.id || applyingWantedJobIds.has(j.id)"
                  @click="applyWantedJob(j)"
                >
                  {{ applyingWantedJobIds.has(j.id) ? 'Đang apply...' : 'Apply' }}
                </button>
                <button class="btc-btn-secondary border-rose-200 text-rose-700" @click="removeWantedJob(j.id)">Hủy</button>
              </div>
            </div>
            <p v-if="lastReviewedJobId !== j.id" class="text-xs text-slate-600 mt-2">(Cần review job này trước khi apply)</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Applied Jobs List -->
    <div class="btc-card max-w-4xl mb-6">
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
    <div class="btc-card max-w-2xl mb-6">
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
    <div class="btc-card max-w-4xl mb-6">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-2">
        <h3 class="text-lg font-semibold">Your CV</h3>
        <button
          v-if="cvUrl"
          class="btc-btn-secondary"
          type="button"
          @click="showCvPreview = !showCvPreview"
        >
          {{ showCvPreview ? 'Ẩn CV' : 'Hiện CV' }}
        </button>
      </div>
      <div v-if="!cvUrl" class="bg-amber-50 border border-amber-200 text-amber-800 p-4 rounded-lg">
        <p>Please upload your CV first in <router-link to="/candidate/cv" class="font-semibold underline">CV Management</router-link></p>
      </div>
      <PdfViewer v-else-if="showCvPreview" :pdfUrl="cvUrl" />
      <p v-else class="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
        CV đang được ẩn.
      </p>
    </div>

    <!-- Action Buttons -->
    <div class="btc-card max-w-2xl mb-6">
      <div class="flex gap-3 flex-wrap">
        <button
          class="btc-btn-secondary"
          :disabled="!cvUrl || loading"
          @click="suggestJobs"
        >
          {{ loading ? 'Analyzing...' : 'Get Job Suggestions' }}
        </button>
        <button
          class="btc-btn-primary"
          :disabled="!cvUrl || !selectedJobId || loading"
          @click="reviewCv"
        >
          {{ loading ? 'Reviewing...' : 'Review CV for This Job' }}
        </button>
      </div>
      <p v-if="error" class="text-sm text-rose-600 mt-3">{{ error }}</p>
    </div>

    <!-- Job Suggestions -->
    <div v-if="suggestions.length" class="btc-card max-w-4xl mb-6">
      <h3 class="font-semibold mb-4">AI Job Recommendations</h3>
      <div class="space-y-2">
        <div v-for="(s, idx) in suggestions" :key="idx" class="bg-green-50 border border-green-200 p-3 rounded-lg">
          <p class="text-sm text-green-900">{{ idx + 1 }}. {{ s.jobId }} - {{ s.reason }}</p>
        </div>
      </div>
    </div>

    <!-- Review Results -->
    <div v-if="review" class="mt-6 space-y-6">
      <!-- Score Card -->
      <div class="btc-card max-w-2xl">
        <h3 class="text-lg font-semibold mb-4">Review Score</h3>
        <div class="flex items-center gap-8">
          <CircleScore :score="review.score" />
          <div class="flex-1">
            <h4 class="font-semibold mb-3">Missing Keywords</h4>
            <div class="space-y-1">
              <p v-for="k in review.missingKeywords" :key="k" class="text-sm bg-red-50 text-red-700 px-3 py-1 rounded">
                • {{ k }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Suggestions -->
      <div class="btc-card max-w-4xl">
        <h3 class="font-semibold mb-4">AI Suggestions</h3>
        <div class="space-y-4">
          <div v-for="(s, idx) in review.tailoredSuggestions" :key="idx" class="border border-slate-200 rounded-lg overflow-hidden">
            <div class="bg-slate-100 px-4 py-2 font-medium">{{ s.section }}</div>
            <div class="grid md:grid-cols-2 gap-0">
              <div class="p-4 border-r border-slate-200">
                <p class="text-xs font-semibold text-slate-600 mb-2">Original</p>
                <p class="text-sm whitespace-pre-wrap bg-slate-50 p-3 rounded">{{ s.originalText }}</p>
              </div>
              <div class="p-4">
                <p class="text-xs font-semibold text-teal-700 mb-2">Suggested</p>
                <p class="text-sm whitespace-pre-wrap bg-teal-50 p-3 rounded">{{ s.suggestedText }}</p>
              </div>
            </div>
          </div>
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
