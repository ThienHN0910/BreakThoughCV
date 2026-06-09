<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const error = ref('')
const loadingGoogle = ref(false)

async function onGoogleCredentialResponse(response) {
  try {
    loadingGoogle.value = true
    const result = await auth.loginWithGoogleIdToken(response.credential)
      try {
        sessionStorage.setItem('welcomePending', new Date().toISOString())
      } catch {
      }
    if (result.role === 'admin') router.push('/admin/users')
    else if (result.isNewUser || result.role === 'none') router.push('/select-role')
    else router.push('/')
  } catch (e) {
    error.value = e?.response?.data?.message || 'Google login failed'
  } finally {
    loadingGoogle.value = false
  }
}

onMounted(() => {
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID
  if (!clientId) {
    error.value = 'Thiếu VITE_GOOGLE_CLIENT_ID trong .env'
    return
  }

  const initButton = () => {
    if (!window.google) {
      error.value = 'Google SDK chưa sẵn sàng. Hãy thử tải lại trang.'
      return
    }
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: onGoogleCredentialResponse
    })
    window.google.accounts.id.renderButton(document.getElementById('googleBtn'), {
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text: 'signin_with'
    })
  }

  if (window.google) {
    initButton()
    return
  }

  const script = document.createElement('script')
  script.src = 'https://accounts.google.com/gsi/client'
  script.async = true
  script.defer = true
  script.onload = initButton
  script.onerror = () => {
    error.value = 'Không tải được Google SDK.'
  }
  document.head.appendChild(script)
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center px-4 py-8">
    <div class="grid w-full max-w-5xl gap-5 lg:grid-cols-[1.15fr_1fr]">
      <div class="btc-card hidden lg:block">
        <p class="inline-block rounded-full bg-blue-100 px-3 py-1 text-xs font-bold uppercase tracking-wider text-blue-700">BreakThroughCV</p>
        <h1 class="mt-4 text-4xl font-bold leading-tight">Đăng nhập nhanh<br />để tối ưu CV và tuyển dụng.</h1>
        <p class="mt-3 text-slate-600">Candidate có thể đánh giá CV theo JD bằng AI, recruiter quản lý tin tuyển dụng và hồ sơ ứng viên tại cùng một nơi.</p>
      </div>

      <div class="btc-card w-full md:p-8">
        <h2 class="text-2xl font-bold">Đăng nhập</h2>
        <p class="mb-6 mt-2 text-sm text-slate-500">Sử dụng Google để truy cập hệ thống.</p>
        <div id="googleBtn" class="min-h-10"></div>
        <p v-if="loadingGoogle" class="mt-3 text-sm">Đang xử lý đăng nhập...</p>
        <p v-if="error" class="mt-3 text-sm text-rose-600">{{ error }}</p>
      </div>
    </div>
  </div>
</template>
