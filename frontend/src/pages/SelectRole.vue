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
  <div class="flex min-h-screen items-center justify-center px-4 py-8">
    <div class="btc-card w-full max-w-2xl md:p-8">
      <h2 class="text-3xl font-bold">Chọn vai trò</h2>
      <p class="mb-6 mt-2 text-slate-500">Bạn muốn sử dụng Breakthrough CV với vai trò nào?</p>
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <button :disabled="loading" class="rounded-2xl border border-slate-200 bg-white p-5 text-left transition hover:-translate-y-0.5 hover:border-blue-300" @click="chooseRole('candidate')">
          <p class="mb-1 text-sm font-semibold text-blue-700">Candidate</p>
          Người tìm việc
        </button>
        <button :disabled="loading" class="rounded-2xl border border-slate-200 bg-white p-5 text-left transition hover:-translate-y-0.5 hover:border-cyan-300" @click="chooseRole('recruiter')">
          <p class="mb-1 text-sm font-semibold text-cyan-700">Recruiter</p>
          Nhà tuyển dụng
        </button>
      </div>
      <p v-if="error" class="mt-4 text-sm text-rose-600">{{ error }}</p>
    </div>
  </div>
</template>
