<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

const jobs = ref([])
const categories = ref([])
const loading = ref(false)
const categoryId = ref('')
const keyword = ref('')
const error = ref('')

const myAppliedJobIds = ref([])
const applyingJobId = ref('')

const aiReviewJobIds = ref([])

function aiReviewStorageKey() {
  const userId = auth.user?.userId || ''
  return `aiReviewJobIds:${userId}`
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
    myAppliedJobIds.value = (data || []).map(a => a.jobId)
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
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Tìm kiếm việc làm</h2>
    <p class="btc-page-subtitle">Lọc theo danh mục và từ khóa để tìm JD phù hợp với kỹ năng của bạn.</p>

    <div class="btc-card mb-4 grid gap-3 md:grid-cols-3">
      <select v-model="categoryId" class="btc-input">
        <option value="">Tất cả danh mục</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input v-model="keyword" class="btc-input" placeholder="Tìm theo title hoặc skill" />
      <button class="btc-btn-primary" @click="loadJobs">Lọc</button>
    </div>

    <p v-if="error" class="mb-3 text-rose-600">{{ error }}</p>
    <p v-if="loading">Đang tải...</p>

    <div class="grid gap-4">
      <div v-for="job in jobs" :key="job.id" class="btc-card">
        <h3 class="font-semibold text-lg">{{ job.title }}</h3>
        <p class="text-sm text-slate-500">{{ job.companyName }} • {{ job.categoryName || 'Chưa phân loại' }}</p>
        <p class="mt-2 text-sm">{{ job.description }}</p>
        <div class="mt-2 flex flex-wrap gap-2 text-xs">
          <span v-for="s in job.mustHaveSkills" :key="s" class="rounded-full bg-cyan-50 px-2.5 py-1 text-cyan-700">{{ s }}</span>
        </div>

        <div v-if="auth.role === 'candidate'" class="mt-4 flex justify-end">
          <div class="flex flex-wrap gap-2">
            <button
              class="btc-btn-secondary"
              :disabled="isAddedForAiReview(job.id)"
              @click="addForAiReview(job)"
            >
              <span v-if="isAddedForAiReview(job.id)">Đã thêm để AI review</span>
              <span v-else>Muốn apply (AI review trước)</span>
            </button>

            <button
              class="btc-btn-primary"
              :disabled="applyingJobId === job.id || isApplied(job.id)"
              @click="applyJob(job)"
            >
              <span v-if="isApplied(job.id)">Đã apply</span>
              <span v-else>{{ applyingJobId === job.id ? 'Đang apply...' : 'Apply' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
