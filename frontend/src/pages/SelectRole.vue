<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const loading = ref(false)
const error = ref('')

async function chooseRole(role) {
  try {
    loading.value = true
    await auth.updateRole(role)
    router.push('/')
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể cập nhật vai trò'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center">
    <div class="bg-white rounded-xl shadow p-8 w-full max-w-lg">
      <h2 class="text-2xl font-bold mb-2">Chọn vai trò</h2>
      <p class="text-slate-500 mb-6">Bạn muốn sử dụng Breakthrough CV với vai trò nào?</p>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <button :disabled="loading" class="border rounded-lg p-4 hover:bg-slate-50" @click="chooseRole('candidate')">
          Người tìm việc
        </button>
        <button :disabled="loading" class="border rounded-lg p-4 hover:bg-slate-50" @click="chooseRole('recruiter')">
          Nhà tuyển dụng
        </button>
      </div>
      <p v-if="error" class="text-red-600 text-sm mt-4">{{ error }}</p>
    </div>
  </div>
</template>
