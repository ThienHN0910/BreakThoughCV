<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import api from '../services/api'

const jobs = ref([])
const selectedJobId = ref('')
const applications = ref([])
const error = ref('')

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
  const company = await api.get('/companies/my')
  const { data } = await api.get(`/jobs/company/${company.data.id}`)
  jobs.value = data
  if (jobs.value.length && !selectedJobId.value) selectedJobId.value = jobs.value[0].id
}

async function loadApplications() {
  if (!selectedJobId.value) return
  try {
    const { data } = await api.get(`/applications/job/${selectedJobId.value}`)
    applications.value = data
    closeCvViewer()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được ứng viên'
  }
}

async function updateStatus(item, status) {
  await api.put(`/applications/${item.id}/status`, { status })
  item.status = status
}

onMounted(async () => {
  await loadJobs()
  await loadApplications()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Ứng viên đã apply</h2>
    <p class="btc-page-subtitle">Theo dõi hồ sơ ứng tuyển và cập nhật trạng thái xử lý.</p>

    <div class="btc-card mb-4 flex flex-wrap items-center gap-2">
      <select v-model="selectedJobId" class="btc-input max-w-sm">
        <option v-for="j in jobs" :key="j.id" :value="j.id">{{ j.title }}</option>
      </select>
      <button class="btc-btn-primary" @click="loadApplications">Xem ứng viên</button>
    </div>

    <p v-if="error" class="mb-3 text-rose-600">{{ error }}</p>

    <div class="space-y-3">
      <div v-for="item in applications" :key="item.id" class="btc-card">
        <h3 class="font-semibold">{{ item.candidateName }}</h3>
        <p class="text-sm text-slate-600">{{ item.candidateEmail }}</p>
        <div class="mt-2 flex flex-wrap items-center gap-2">
          <button class="btc-btn-secondary" @click="toggleCvViewer(item)">
            {{ openCvApplicationId === item.id ? 'Đóng CV' : 'Xem CV' }}
          </button>
          <a v-if="item.cvUrl" :href="item.cvUrl" target="_blank" class="text-blue-600 text-sm">Mở tab mới</a>
        </div>

        <div v-if="openCvApplicationId === item.id" class="mt-3">
          <p v-if="cvError" class="text-sm text-rose-600 mb-2">{{ cvError }}</p>
          <p v-if="cvLoading" class="text-sm">Đang tải CV...</p>
          <PdfViewer v-else :pdfUrl="cvBlobUrl" />
        </div>
        <div class="mt-2 flex items-center gap-2">
          <span class="text-sm">Trạng thái: {{ item.status }}</span>
          <button class="btc-btn-secondary px-2.5 py-1 text-xs" @click="updateStatus(item, 'Pending')">Pending</button>
          <button class="btc-btn-secondary px-2.5 py-1 text-xs" @click="updateStatus(item, 'Reviewed')">Reviewed</button>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
