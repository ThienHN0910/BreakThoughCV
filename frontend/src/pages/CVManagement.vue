<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

const cvFile = ref(null)
const cvUrl = ref('')
const cvPreviewBlobUrl = ref('')
const cvPreviewLoading = ref(false)
const cvPreviewError = ref('')
const error = ref('')
const loading = ref(false)
const hasCv = ref(false)

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
        <div class="border-2 border-dashed border-blue-200 rounded-lg p-6 text-center">
          <input
            type="file"
            accept=".pdf"
            @change="onCvFileSelected"
            class="hidden"
            id="cv-file-input"
          />
          <label for="cv-file-input" class="cursor-pointer">
            <div class="text-gray-600">
              <p class="font-medium">Click to upload or drag and drop</p>
              <p class="text-sm text-gray-500">PDF files only, max 5MB</p>
            </div>
          </label>
          <p v-if="cvFile" class="mt-2 text-sm text-green-600">
            ✓ Selected: {{ cvFile.name }}
          </p>
        </div>

        <div class="flex gap-3">
          <button
            class="btc-btn-primary"
            :disabled="loading || !cvFile"
            @click="uploadCv"
          >
            {{ loading ? 'Uploading...' : 'Upload CV' }}
          </button>
          <button
            v-if="hasCv"
            class="btc-btn-secondary"
            :disabled="loading"
            @click="deleteCv"
          >
            Delete Current CV
          </button>
        </div>

        <p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
        <p v-if="hasCv && !error" class="text-sm text-green-600">✓ CV uploaded successfully</p>
      </div>
    </div>

    <!-- CV Preview Section -->
    <div class="btc-card max-w-4xl">
      <h3 class="text-lg font-semibold mb-4">CV Preview</h3>
      <p v-if="cvPreviewError" class="mb-2 text-sm text-rose-600">{{ cvPreviewError }}</p>
      <p v-if="cvPreviewLoading" class="text-sm">Đang tải CV...</p>
      <PdfViewer v-else :pdfUrl="cvPreviewBlobUrl" :onDownload="hasCv ? downloadCv : null" />
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
