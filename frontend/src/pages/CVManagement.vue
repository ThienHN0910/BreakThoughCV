<script setup>
import { ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'

const cvFile = ref(null)
const uploadedUrl = ref('')
const error = ref('')
const loading = ref(false)

async function uploadCv() {
  if (!cvFile.value) return
  const formData = new FormData()
  formData.append('cvFile', cvFile.value)

  try {
    loading.value = true
    const { data } = await api.post('/cv/upload', formData)
    uploadedUrl.value = data.url
    error.value = ''
  } catch (e) {
    error.value = e?.response?.data?.message || 'Upload CV thất bại'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Quản lý CV cá nhân</h2>
    <p class="btc-page-subtitle">Tải CV để dùng cho ứng tuyển và phân tích AI.</p>

    <div class="btc-card max-w-2xl">
      <input class="btc-input" type="file" accept=".pdf,.doc,.docx" @change="(e) => (cvFile = e.target.files[0] || null)" />
      <button class="btc-btn-primary mt-3" :disabled="loading" @click="uploadCv">
        Upload CV
      </button>
      <p v-if="uploadedUrl" class="mt-3 text-sm">CV URL: <a :href="uploadedUrl" target="_blank" class="text-blue-600">{{ uploadedUrl }}</a></p>
      <p v-if="error" class="mt-2 text-sm text-rose-600">{{ error }}</p>
    </div>
  </AppLayout>
</template>
