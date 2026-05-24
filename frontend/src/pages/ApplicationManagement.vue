<script setup>
import { onMounted, onBeforeUnmount, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import api from '../services/api'

const jobs = ref([])
const selectedJobId = ref('')
const applications = ref([])
const error = ref('')
const updatingStatusId = ref('')
const loading = ref(false)

const refreshing = ref(false)
const lastRefreshTime = ref(null)
const pollingInterval = ref(null)
const POLLING_INTERVAL = 30000 // 30 seconds

const openCvApplicationId = ref('')
const cvBlobUrl = ref('')
const cvLoading = ref(false)
const cvError = ref('')

function closeCvViewer() {
  openCvApplicationId.value = ''
  cvError.value = ''

  if (cvBlobUrl.value) {
    try {
      URL.revokeObjectURL(cvBlobUrl.value)
    } catch {
    }
  }
  cvBlobUrl.value = ''
}

async function toggleCvViewer(item) {
  if (!item?.id) return

  if (openCvApplicationId.value === item.id) {
    closeCvViewer()
    return
  }

  closeCvViewer()
  openCvApplicationId.value = item.id
  cvLoading.value = true
  cvError.value = ''

  try {
    const resp = await api.get(`/applications/${item.id}/cv-file`, { responseType: 'blob' })
    const blob = resp.data
    cvBlobUrl.value = URL.createObjectURL(blob)
  } catch (e) {
    cvError.value = e?.response?.data?.message || 'Không tải được CV'
    openCvApplicationId.value = ''
  } finally {
    cvLoading.value = false
  }
}

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

async function loadApplications() {
  if (!selectedJobId.value) return
  try {
    error.value = ''
    loading.value = true
    const { data } = await api.get(`/applications/job/${selectedJobId.value}`)
    applications.value = data
    closeCvViewer()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được ứng viên'
    applications.value = []
  } finally {
    loading.value = false
  }
}

async function refreshApplications() {
  if (!selectedJobId.value) return
  try {
    refreshing.value = true
    error.value = ''
    const { data } = await api.get(`/applications/job/${selectedJobId.value}`)
    applications.value = data
    lastRefreshTime.value = new Date()
    closeCvViewer()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được ứng viên'
  } finally {
    refreshing.value = false
  }
}

function setupPolling() {
  // Clear existing polling
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }
  
  // Start new polling if job selected and has applications
  if (selectedJobId.value && applications.value.length > 0) {
    pollingInterval.value = setInterval(() => {
      refreshApplications()
    }, POLLING_INTERVAL)
  }
}

async function updateStatus(item, status) {
  if (!item?.id || updatingStatusId.value) return

  updatingStatusId.value = item.id
  error.value = ''

  try {
    await api.put(`/applications/${item.id}/status`, { status })
    item.status = status
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không cập nhật được trạng thái'
  } finally {
    updatingStatusId.value = ''
  }
}

// Auto-load applications when job selection changes
watch(selectedJobId, async (newJobId) => {
  if (newJobId) {
    await loadApplications()
  }
})

// Auto-refresh polling setup when applications change
watch(applications, (newApps) => {
  setupPolling()
})

onMounted(async () => {
  await loadJobs()
})

onBeforeUnmount(() => {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }
})
</script>

<template>
  <AppLayout>
    <div class="mb-6">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="btc-page-title">Ứng viên đã apply</h2>
          <p class="btc-page-subtitle">Theo dõi hồ sơ ứng tuyển và cập nhật trạng thái xử lý.</p>
        </div>
        <div v-if="selectedJobId && applications.length > 0" class="flex items-center gap-2">
          <button
            class="btc-btn-secondary px-3 py-1.5 text-xs"
            :disabled="refreshing"
            @click="refreshApplications()"
            title="Làm mới danh sách ứng viên"
          >
            {{ refreshing ? '⏳ Đang làm mới...' : '🔄 Làm mới' }}
          </button>
          <span v-if="lastRefreshTime" class="text-xs text-gray-500">
            Cập nhật: {{ lastRefreshTime.toLocaleTimeString('vi-VN') }}
          </span>
        </div>
      </div>
    </div>

    <div v-if="error" class="mb-4 rounded-lg border border-rose-200 bg-rose-50 p-4 text-rose-700">
      {{ error }}
    </div>

    <div class="btc-card mb-4 flex flex-wrap items-center gap-2">
      <label class="text-sm font-medium">Chọn tin tuyển dụng:</label>
      <select v-model="selectedJobId" class="btc-input max-w-sm" :disabled="loading || jobs.length === 0">
        <option value="">-- Chọn tin tuyển dụng --</option>
        <option v-for="j in jobs" :key="j.id" :value="j.id">{{ j.title || j.id }}</option>
      </select>
      <div v-if="loading" class="text-sm text-gray-600">
        ⏳ Đang tải...
      </div>
      <div v-if="selectedJobId && applications.length > 0" class="ml-auto flex items-center gap-1 text-xs text-gray-600 bg-blue-50 px-2 py-1 rounded">
        🔄 Auto-refresh mỗi 30s
      </div>
    </div>

    <div v-if="!selectedJobId && jobs.length === 0" class="rounded-lg border border-amber-200 bg-amber-50 p-8 text-center text-amber-800">
      <p class="font-medium">Bạn chưa tạo tin tuyển dụng nào</p>
      <p class="mt-2 text-sm">Hãy tạo tin tuyển dụng trước để xem ứng viên nộp hồ sơ</p>
      <RouterLink to="/recruiter/jobs" class="btc-btn-primary mt-4 inline-block">
        Tạo tin tuyển dụng
      </RouterLink>
    </div>

    <div v-else-if="selectedJobId">
      <div v-if="applications.length === 0" class="rounded-lg border border-slate-200 bg-slate-50 p-8 text-center text-slate-600">
        Chưa có ứng viên nào nộp hồ sơ cho tin này
      </div>

      <div v-else class="space-y-3">
        <div v-for="item in applications" :key="item.id" class="btc-card">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <h3 class="font-semibold">{{ item.candidateName }}</h3>
              <p class="text-sm text-slate-600">{{ item.candidateEmail }}</p>
              <p class="mt-2 text-xs text-slate-500">
                Nộp vào: {{ new Date(item.appliedAt).toLocaleString('vi-VN') }}
              </p>
            </div>
          </div>

          <div class="mt-3 flex flex-wrap items-center gap-2">
            <button class="btc-btn-secondary" @click="toggleCvViewer(item)">
              {{ openCvApplicationId === item.id ? '📄 Đóng CV' : '📄 Xem CV' }}
            </button>
            <span class="text-sm font-medium" :class="{
              'text-yellow-600': item.status === 'Pending',
              'text-blue-600': item.status === 'Reviewed',
              'text-green-600': item.status === 'Accepted',
              'text-red-600': item.status === 'Rejected'
            }">
              Trạng thái: {{ item.status }}
            </span>
          </div>

          <div v-if="openCvApplicationId === item.id" class="mt-3 border-t pt-3">
            <p v-if="cvError" class="text-sm text-rose-600 mb-2">{{ cvError }}</p>
            <p v-if="cvLoading" class="text-sm">⏳ Đang tải CV...</p>
            <PdfViewer v-else-if="cvBlobUrl" :pdfUrl="cvBlobUrl" />
          </div>

          <div class="mt-3 flex flex-wrap items-center gap-2 border-t pt-3">
            <button
              class="btc-btn-secondary px-2.5 py-1 text-xs"
              :disabled="updatingStatusId === item.id || item.status === 'Rejected'"
              @click="updateStatus(item, 'Pending')"
              title="Đánh dấu là chưa xử lý"
            >
              ⏳ Pending
            </button>
            <button
              class="btc-btn-secondary px-2.5 py-1 text-xs"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Reviewed')"
              title="Đánh dấu là đã xem"
            >
              👁️ Reviewed
            </button>
            <button
              class="btc-btn-primary px-2.5 py-1 text-xs disabled:opacity-60"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Accepted')"
              title="Chấp nhận ứng viên"
            >
              ✅ Chấp nhận
            </button>
            <button
              class="rounded-xl border border-rose-200 bg-rose-50 px-2.5 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-100 disabled:opacity-60"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Rejected')"
              title="Từ chối ứng viên"
            >
              ❌ Từ chối
            </button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
