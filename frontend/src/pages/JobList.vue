<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useNotificationsStore } from '../stores/notifications'

const auth = useAuthStore()
const notifications = useNotificationsStore()

const jobs = ref([])
const categories = ref([])
const loading = ref(false)
const categoryId = ref('')
const keyword = ref('')
const error = ref('')

const myAppliedJobIds = ref([])
const applyingJobId = ref('')

const aiReviewJobIds = ref([])
const reviewedJobIds = ref([])

function aiReviewStorageKey() {
  const userKey = auth.user?.userId || auth.user?.email || ''
  return `aiReviewJobIds:${userKey}`
}

function reviewedStorageKey() {
  const userKey = auth.user?.userId || auth.user?.email || ''
  return `aiReviewedJobIds:${userKey}`
}

function loadAiReviewJobIds() {
  if (auth.role !== 'candidate') {
    aiReviewJobIds.value = []
    return
  }
  try {
    const raw = localStorage.getItem(aiReviewStorageKey())
    const parsed = raw ? JSON.parse(raw) : []
    aiReviewJobIds.value = Array.isArray(parsed) ? parsed : []
  } catch {
    aiReviewJobIds.value = []
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

function isReviewed(jobId) {
  if (!jobId) return false
  const normalized = String(jobId)
  return (reviewedJobIds.value || []).map(String).includes(normalized)
}

function saveAiReviewJobIds() {
  if (auth.role !== 'candidate') return
  localStorage.setItem(aiReviewStorageKey(), JSON.stringify(aiReviewJobIds.value))
}

function isAddedForAiReview(jobId) {
  return aiReviewJobIds.value.includes(jobId)
}

function addForAiReview(job) {
  if (auth.role !== 'candidate') return
  if (isAddedForAiReview(job.id)) return
  aiReviewJobIds.value = [...aiReviewJobIds.value, job.id]
  saveAiReviewJobIds()
}

async function loadMyApplications() {
  if (auth.role !== 'candidate') return
  try {
    const { data } = await api.get('/applications/my')
    const apps = data || []
    myAppliedJobIds.value = apps.map(a => a.jobId)

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
      for (const a of apps) {
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
            href: '/notifications'
          })
        }
      }

      try {
        localStorage.setItem(statusKey, JSON.stringify(nextMap))
      } catch {
      }
    }
  } catch {
    // ignore: candidate may have none or token may be missing
    myAppliedJobIds.value = []
  }
}

function isApplied(jobId) {
  return myAppliedJobIds.value.includes(jobId)
}

async function applyJob(job) {
  if (auth.role !== 'candidate') return
  if (isApplied(job.id)) return

  try {
    applyingJobId.value = job.id
    error.value = ''
    await api.post('/applications/quick', { jobId: job.id })
    myAppliedJobIds.value = [...myAppliedJobIds.value, job.id]

    notifications.add({
      type: 'success',
      title: 'Nộp hồ sơ thành công',
      message: job?.title ? `Bạn đã nộp hồ sơ cho: ${job.title}` : 'Bạn đã nộp hồ sơ thành công.',
      href: '/jobs'
    })
  } catch (e) {
    error.value = e?.response?.data?.message || 'Apply thất bại'
  } finally {
    applyingJobId.value = ''
  }
}

async function loadCategories() {
  const { data } = await api.get('/categories')
  categories.value = data
}

async function loadJobs() {
  try {
    loading.value = true
    const { data } = await api.get('/jobs', { params: { categoryId: categoryId.value || undefined, keyword: keyword.value || undefined } })
    jobs.value = data.data || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể tải danh sách công việc'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadCategories(), loadJobs()])
  await loadMyApplications()
  loadAiReviewJobIds()
  loadReviewedJobIds()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Tìm kiếm việc làm</h2>
    <p class="btc-page-subtitle">Lọc theo danh mục và từ khóa để tìm JD phù hợp với kỹ năng của bạn.</p>

    <div class="btc-card mb-6 grid gap-4 md:grid-cols-12 items-center">
      <div class="md:col-span-4">
        <select v-model="categoryId" class="btc-input">
          <option value="">Tất cả danh mục</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div class="md:col-span-6 relative">
        <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <svg class="h-5 w-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
        </div>
        <input v-model="keyword" class="btc-input pl-10" placeholder="Tìm theo chức danh hoặc kỹ năng..." />
      </div>
      <div class="md:col-span-2">
        <button class="btc-btn-primary w-full" @click="loadJobs">Lọc</button>
      </div>
    </div>

    <p v-if="error" class="mb-3 text-rose-600">{{ error }}</p>
    <p v-if="loading">Đang tải...</p>

    <div class="grid gap-5">
      <div v-for="job in jobs" :key="job.id" class="btc-card group">
        <div class="flex flex-wrap items-center gap-3 mb-1.5">
          <h3 class="font-bold text-slate-800 text-lg group-hover:text-indigo-600 transition-colors">{{ job.title }}</h3>
          <span
            v-if="isReviewed(job.id)"
            class="inline-flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700"
          >
            <svg class="w-3.5 h-3.5 text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            AI đã review
          </span>
        </div>
        <div class="flex items-center gap-2 text-sm text-slate-500 font-medium mb-3">
          <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"></path></svg>
          {{ job.companyName }}
          <span class="text-slate-300">•</span>
          <span>{{ job.categoryName || 'Chưa phân loại' }}</span>
        </div>
        <p class="text-sm text-slate-600 leading-relaxed line-clamp-2">{{ job.description }}</p>
        <div class="mt-4 flex flex-wrap gap-2 text-xs">
          <span v-for="s in job.mustHaveSkills" :key="s" class="inline-flex items-center rounded-md bg-indigo-50 border border-indigo-100 px-2.5 py-1 text-indigo-700 font-medium">
            {{ s }}
          </span>
        </div>

        <div v-if="auth.role === 'candidate'" class="mt-5 pt-4 border-t border-slate-100 flex justify-end">
          <div class="flex flex-wrap gap-3">
            <button
              class="btc-btn-secondary"
              :class="isAddedForAiReview(job.id) ? '!bg-emerald-50 !border-emerald-200 !text-emerald-700 hover:!bg-emerald-100 hover:!border-emerald-300' : ''"
              :disabled="isAddedForAiReview(job.id)"
              @click="addForAiReview(job)"
            >
              <span v-if="isAddedForAiReview(job.id)" class="inline-flex items-center gap-1.5"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg> Đã thêm để AI review</span>
              <span v-else class="inline-flex items-center gap-1.5"><svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg> Muốn apply (AI review trước)</span>
            </button>

            <button
              class="btc-btn-primary"
              :disabled="applyingJobId === job.id || isApplied(job.id)"
              @click="applyJob(job)"
            >
              <span v-if="isApplied(job.id)" class="inline-flex items-center gap-1.5"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg> Đã apply</span>
              <span v-else>{{ applyingJobId === job.id ? 'Đang apply...' : 'Apply ngay' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
