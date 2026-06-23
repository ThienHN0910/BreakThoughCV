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
  <div class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4 py-8">
    <div class="btc-card w-full max-w-2xl p-8 md:p-10 text-center">
      <h2 class="text-3xl font-extrabold text-slate-800">Chọn vai trò</h2>
      <p class="mb-8 mt-2 text-slate-500 text-lg">Bạn muốn sử dụng BreakThroughCV với vai trò nào?</p>
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <button :disabled="loading" class="group rounded-2xl border border-slate-200 bg-white p-6 text-center transition-all hover:-translate-y-1 hover:border-indigo-400 hover:shadow-md" @click="chooseRole('candidate')">
          <div class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-indigo-50 text-indigo-600 group-hover:bg-indigo-100 transition-colors">
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path></svg>
          </div>
          <p class="mb-1 text-base font-bold text-slate-800 group-hover:text-indigo-700">Người tìm việc</p>
          <p class="text-xs font-semibold uppercase tracking-wider text-indigo-500">Candidate</p>
        </button>
        <button :disabled="loading" class="group rounded-2xl border border-slate-200 bg-white p-6 text-center transition-all hover:-translate-y-1 hover:border-emerald-400 hover:shadow-md" @click="chooseRole('recruiter')">
          <div class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600 group-hover:bg-emerald-100 transition-colors">
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path></svg>
          </div>
          <p class="mb-1 text-base font-bold text-slate-800 group-hover:text-emerald-700">Nhà tuyển dụng</p>
          <p class="text-xs font-semibold uppercase tracking-wider text-emerald-500">Recruiter</p>
        </button>
      </div>
      <p v-if="error" class="mt-6 text-sm font-medium text-rose-600">{{ error }}</p>
    </div>
  </div>
</template>
