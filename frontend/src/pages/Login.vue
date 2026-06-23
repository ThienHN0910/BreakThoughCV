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
  <div class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4 py-12">
    <div class="grid w-full max-w-5xl gap-6 lg:grid-cols-[1.2fr_1fr]">
      <div class="btc-card hidden lg:flex flex-col justify-center bg-indigo-50 border-indigo-100 p-10">
        <div>
          <p class="inline-flex items-center rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wider bg-indigo-500 text-white shadow-sm">
            BreakThroughCV
          </p>
        </div>
        <h1 class="mt-6 text-4xl font-extrabold leading-tight text-slate-800">
          Đăng nhập nhanh<br />để tối ưu CV và tuyển dụng.
        </h1>
        <p class="mt-4 text-slate-600 leading-relaxed text-lg">
          Candidate có thể đánh giá CV theo JD bằng AI, recruiter quản lý tin tuyển dụng và hồ sơ ứng viên tại cùng một nơi.
        </p>
      </div>

      <div class="btc-card w-full p-8 md:p-10 flex flex-col justify-center">
        <h2 class="text-3xl font-bold text-slate-800">Đăng nhập</h2>
        <p class="mb-8 mt-2 text-slate-500">Sử dụng tài khoản Google để truy cập hệ thống.</p>
        <div id="googleBtn" class="min-h-10 flex justify-center lg:justify-start"></div>
        <div v-if="loadingGoogle" class="mt-4 flex items-center gap-2 text-sm text-indigo-600 font-medium">
          <svg class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
          Đang xử lý đăng nhập...
        </div>
        <p v-if="error" class="mt-4 text-sm font-medium text-rose-600">{{ error }}</p>
      </div>
    </div>
  </div>
</template>
