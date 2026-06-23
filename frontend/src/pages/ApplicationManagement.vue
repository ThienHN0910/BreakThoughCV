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

    <div class="btc-card mb-6 flex flex-wrap items-center gap-4">
      <div class="flex items-center gap-3 flex-1">
        <label class="text-sm font-medium text-slate-700 whitespace-nowrap">Chọn tin tuyển dụng:</label>
        <select v-model="selectedJobId" class="btc-input max-w-md" :disabled="loading || jobs.length === 0">
          <option value="">-- Chọn tin tuyển dụng --</option>
          <option v-for="j in jobs" :key="j.id" :value="j.id">{{ j.title || j.id }}</option>
        </select>
        <div v-if="loading" class="text-sm text-slate-500 flex items-center gap-2">
          <svg class="animate-spin h-4 w-4 text-indigo-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
          Đang tải...
        </div>
      </div>
      <div v-if="selectedJobId && applications.length > 0" class="flex items-center gap-1.5 text-xs font-medium text-indigo-600 bg-indigo-50 border border-indigo-100 px-3 py-1.5 rounded-lg">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path></svg>
        Auto-refresh mỗi 30s
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
              <h3 class="font-bold text-slate-800 text-lg">{{ item.candidateName }}</h3>
              <p class="text-sm font-medium text-slate-500 mt-0.5">{{ item.candidateEmail }}</p>
              <div class="mt-3 flex items-center gap-1.5 text-xs text-slate-400 font-medium">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                Nộp lúc: {{ new Date(item.appliedAt).toLocaleString('vi-VN') }}
              </div>
            </div>
          </div>

          <div class="mt-3 flex flex-wrap items-center gap-2">
            <button class="btc-btn-secondary !py-1.5 !px-3 text-xs" @click="toggleCvViewer(item)">
              <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>
              {{ openCvApplicationId === item.id ? 'Đóng CV' : 'Xem CV' }}
            </button>
            <span class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-semibold border" :class="{
              'bg-amber-50 text-amber-700 border-amber-200': item.status === 'Pending',
              'bg-blue-50 text-blue-700 border-blue-200': item.status === 'Reviewed',
              'bg-emerald-50 text-emerald-700 border-emerald-200': item.status === 'Accepted',
              'bg-rose-50 text-rose-700 border-rose-200': item.status === 'Rejected'
            }">
              {{ item.status }}
            </span>
          </div>

          <div v-if="openCvApplicationId === item.id" class="mt-3 border-t pt-3">
            <p v-if="cvError" class="text-sm text-rose-600 mb-2">{{ cvError }}</p>
            <p v-if="cvLoading" class="text-sm">⏳ Đang tải CV...</p>
            <PdfViewer v-else-if="cvBlobUrl" :pdfUrl="cvBlobUrl" />
          </div>

          <div class="mt-4 flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
            <span class="text-xs font-semibold text-slate-500 uppercase mr-2">Đổi trạng thái:</span>
            <button
              class="btc-btn-secondary !py-1.5 !px-3 text-xs"
              :disabled="updatingStatusId === item.id || item.status === 'Rejected'"
              @click="updateStatus(item, 'Pending')"
            >
              Pending
            </button>
            <button
              class="btc-btn-secondary !py-1.5 !px-3 text-xs"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Reviewed')"
            >
              Reviewed
            </button>
            <button
              class="btc-btn-primary !py-1.5 !px-3 text-xs disabled:opacity-60"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Accepted')"
            >
              Chấp nhận
            </button>
            <button
              class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-700 transition hover:bg-rose-100 disabled:opacity-60"
              :disabled="updatingStatusId === item.id"
              @click="updateStatus(item, 'Rejected')"
            >
              Từ chối
            </button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
