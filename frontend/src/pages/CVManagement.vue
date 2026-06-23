<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useNotificationsStore } from '../stores/notifications'

const auth = useAuthStore()
const notifications = useNotificationsStore()

const cvFile = ref(null)
const cvUrl = ref('')
const cvPreviewBlobUrl = ref('')
const cvPreviewLoading = ref(false)
const cvPreviewError = ref('')
const error = ref('')
const loading = ref(false)
const hasCv = ref(false)
const showFullCv = ref(false)

function revokeCvPreviewUrl() {
  if (cvPreviewBlobUrl.value) {
    try {
      URL.revokeObjectURL(cvPreviewBlobUrl.value)
    } catch {
    }
  }
  cvPreviewBlobUrl.value = ''
}

async function loadCvPreview() {
  if (!auth.user?.userId) return
  if (!hasCv.value) {
    revokeCvPreviewUrl()
    showFullCv.value = false
    return
  }

  cvPreviewLoading.value = true
  cvPreviewError.value = ''
  revokeCvPreviewUrl()

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

function onCvFileSelected(e) {
  cvFile.value = e?.target?.files?.[0] || null
  error.value = ''
}

const loadMyCv = async () => {
  try {
    const { data } = await api.get('/cv/my')
    cvUrl.value = data.cvUrl || ''
    hasCv.value = data.hasCV
    await loadCvPreview()
  } catch (e) {
    console.error('Failed to load CV:', e)
  }
}

async function uploadCv() {
  if (!cvFile.value) {
    error.value = 'Please select a PDF file'
    return
  }

  const fileName = (cvFile.value.name || '').toLowerCase()
  const isPdfByName = fileName.endsWith('.pdf')
  const isPdfByType = !cvFile.value.type || cvFile.value.type === 'application/pdf'
  if (!isPdfByName || !isPdfByType) {
    error.value = 'Only PDF files are allowed'
    return
  }

  if (cvFile.value.size > 10 * 1024 * 1024) {
    error.value = 'File size must be less than 10MB'
    return
  }

  const formData = new FormData()
  formData.append('cvFile', cvFile.value, cvFile.value.name)

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/cv/upload', formData)
    cvUrl.value = data.cvUrl
    hasCv.value = true
    cvFile.value = null
    await loadCvPreview()

    notifications.add({
      type: 'success',
      title: 'Nộp CV thành công',
      message: 'CV đã được tải lên và sẵn sàng để ứng tuyển / AI Review.',
      href: '/candidate/cv'
    })
  } catch (e) {
    error.value = e?.response?.data?.message || 'CV upload failed'
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function deleteCv() {
  if (!confirm('Are you sure you want to delete your CV?')) return

  try {
    loading.value = true
    await api.delete('/cv')
    cvUrl.value = ''
    hasCv.value = false
    showFullCv.value = false
    error.value = ''
    revokeCvPreviewUrl()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Failed to delete CV'
    console.error(e)
  } finally {
    loading.value = false
  }
}

function downloadCv() {
  if (cvUrl.value) {
    window.open(cvUrl.value, '_blank')
  }
}

onMounted(() => {
  loadMyCv()
})

onBeforeUnmount(() => {
  revokeCvPreviewUrl()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Quản lý CV cá nhân</h2>
    <p class="btc-page-subtitle">Tải CV để dùng cho ứng tuyển và phân tích AI.</p>

    <!-- Upload Section -->
    <div class="btc-card max-w-2xl mb-8">
      <h3 class="text-lg font-semibold mb-4">Upload CV</h3>
      <div class="space-y-4">
        <div class="border-2 border-dashed border-indigo-200 bg-slate-50/50 hover:bg-slate-50 transition-colors rounded-2xl p-8 text-center">
          <input
            type="file"
            accept=".pdf"
            @change="onCvFileSelected"
            class="hidden"
            id="cv-file-input"
          />
          <label for="cv-file-input" class="cursor-pointer block">
            <div class="text-slate-600 flex flex-col items-center gap-2">
              <svg class="w-10 h-10 text-indigo-400 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path></svg>
              <p class="font-semibold text-slate-700">Click to upload or drag and drop</p>
              <p class="text-sm text-slate-500">PDF files only, max 5MB</p>
            </div>
          </label>
          <div v-if="cvFile" class="mt-4 inline-flex items-center gap-2 px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded-lg text-sm font-medium border border-emerald-200">
            <svg class="w-4 h-4 text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            {{ cvFile.name }}
          </div>
        </div>

        <div class="flex flex-wrap gap-3 mt-2">
          <button
            class="btc-btn-primary"
            :disabled="loading || !cvFile"
            @click="uploadCv"
          >
            {{ loading ? 'Uploading...' : 'Upload CV' }}
          </button>
          <button
            v-if="hasCv"
            class="btc-btn-secondary !text-rose-600 !border-rose-200 hover:!bg-rose-50"
            :disabled="loading"
            @click="deleteCv"
          >
            Delete Current CV
          </button>
        </div>

        <p v-if="error" class="text-sm text-rose-600 font-medium">{{ error }}</p>
        <div v-if="hasCv && !error" class="inline-flex items-center gap-1.5 text-sm font-medium text-emerald-600 mt-2">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          CV uploaded successfully
        </div>
      </div>
    </div>

    <!-- CV Preview Section -->
    <div class="btc-card max-w-4xl">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-2">
        <h3 class="text-lg font-semibold text-slate-800">CV Preview</h3>
        <button
          v-if="hasCv"
          class="btc-btn-secondary !py-1.5 !px-3 text-sm"
          type="button"
          @click="showFullCv = !showFullCv"
        >
          {{ showFullCv ? 'Ẩn full CV' : 'Xem full CV' }}
        </button>
      </div>
      <p v-if="cvPreviewError" class="mb-2 text-sm text-rose-600">{{ cvPreviewError }}</p>
      <p v-if="cvPreviewLoading" class="text-sm">Đang tải CV...</p>
      <PdfViewer
        v-else
        :pdfUrl="cvPreviewBlobUrl"
        :onDownload="hasCv ? downloadCv : null"
        :maxHeight="showFullCv ? 'none' : 600"
      />
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

.mb-8 {
  margin-bottom: 2rem;
}

.space-y-4 {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.border-dashed {
  border-style: dashed;
}

.cursor-pointer {
  cursor: pointer;
}

.hidden {
  display: none;
}

.mt-2 {
  margin-top: 0.5rem;
}

.gap-3 {
  display: flex;
  gap: 0.75rem;
}

.flex {
  display: flex;
}

.text-center {
  text-align: center;
}

.p-6 {
  padding: 1.5rem;
}

.rounded-lg {
  border-radius: 0.5rem;
}
</style>
