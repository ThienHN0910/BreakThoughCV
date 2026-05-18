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
    if (result.isNewUser || result.role === 'none') router.push('/select-role')
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
  <div class="min-h-screen flex items-center justify-center">
    <div class="bg-white rounded-xl shadow p-8 w-full max-w-md">
      <h2 class="text-2xl font-bold mb-2">Đăng nhập</h2>
      <p class="text-slate-500 mb-6">Đăng nhập với Google để tiếp tục.</p>
      <div id="googleBtn" class="min-h-10"></div>
      <p v-if="loadingGoogle" class="text-sm mt-3">Đang xử lý đăng nhập...</p>
      <p v-if="error" class="text-sm mt-3 text-red-600">{{ error }}</p>
    </div>
  </div>
</template>
