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
    <h2 class="text-2xl font-bold mb-4">Quản lý CV cá nhân</h2>
    <div class="bg-white border rounded-lg p-4 max-w-xl">
      <input type="file" accept=".pdf,.doc,.docx" @change="(e) => (cvFile = e.target.files[0])" />
      <button class="mt-3 px-4 py-2 rounded bg-slate-900 text-white" :disabled="loading" @click="uploadCv">
        Upload CV
      </button>
      <p v-if="uploadedUrl" class="mt-3 text-sm">CV URL: <a :href="uploadedUrl" target="_blank" class="text-blue-600">{{ uploadedUrl }}</a></p>
      <p v-if="error" class="mt-2 text-red-600 text-sm">{{ error }}</p>
    </div>
  </AppLayout>
</template>
