<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.clearSession()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-screen">
    <header class="bg-white border-b">
      <div class="max-w-7xl mx-auto px-4 py-3 flex justify-between items-center">
        <h1 class="font-bold text-lg">Breakthrough CV</h1>
        <div class="flex items-center gap-3 text-sm">
          <span>{{ auth.user?.name }}</span>
          <button class="px-3 py-1 rounded bg-slate-800 text-white" @click="logout">Logout</button>
        </div>
      </div>
    </header>
    <main class="max-w-7xl mx-auto p-4">
      <slot />
    </main>
  </div>
</template>
