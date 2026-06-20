<script setup>
import { computed, watch, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useNotificationsStore } from '../stores/notifications'

const auth = useAuthStore()
const router = useRouter()
const notifications = useNotificationsStore()

const isVip = computed(() => Boolean(auth.user?.aiAccessEnabled))

watch(
  () => auth.user?.userId || auth.user?.email || '',
  () => {
    notifications.ensureLoaded()
  },
  { immediate: true }
)

const unreadNotifications = computed(() => notifications.unreadCount)

const navItems = computed(() => {
  if (auth.role === 'admin') {
    return [
      { to: '/admin/users', label: 'Người dùng' },
      { to: '/admin/reviews', label: 'Đánh giá' },
      { to: '/', label: 'Tổng quan' }
    ]
  }

  if (auth.role === 'recruiter') {
    return [
      { to: '/recruiter/company', label: 'Công ty' },
      { to: '/recruiter/jobs', label: 'Tuyển dụng' },
      { to: '/recruiter/applications', label: 'Ứng viên' },
      { to: '/recruiter/review', label: 'Đánh giá' },
      { to: '/notifications', label: 'Thông báo' }
    ]
  }

  return [
    { to: '/jobs', label: 'Việc làm' },
    { to: '/candidate/cv', label: 'CV của tôi' },
    { to: '/candidate/ai-review', label: 'AI Review' },
    { to: '/candidate/ai-purchases', label: 'Gói AI đã mua' },
    { to: '/candidate/review', label: 'Đánh giá' },
    { to: '/notifications', label: 'Thông báo' }
  ]
})

function logout() {
  auth.clearSession()
  router.push('/login')
}

const isDark = ref(false)

onMounted(() => {
  isDark.value = document.documentElement.classList.contains('dark')
})

function toggleTheme() {
  isDark.value = !isDark.value
  if (isDark.value) {
    document.documentElement.classList.add('dark')
    localStorage.setItem('btc-theme', 'dark')
  } else {
    document.documentElement.classList.remove('dark')
    localStorage.setItem('btc-theme', 'light')
  }
}
</script>

<template>
  <div class="min-h-screen pb-8">
    <header class="sticky top-0 z-20 border-b backdrop-blur-md" :style="{ background: 'var(--btc-surface)', borderColor: 'var(--btc-border)' }">
      <div class="btc-shell flex flex-col gap-3 py-3 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-3">
          <button class="rounded-xl px-3 py-1 text-sm font-bold shadow-sm transition-transform hover:scale-105" :style="{ background: 'var(--btc-ink)', color: 'var(--btc-bg-1)' }" @click="router.push('/')">BTCV</button>
          <div>
            <h1 class="text-base font-bold md:text-lg">BreakThroughCV</h1>
            <p class="text-xs font-medium" :style="{ color: 'var(--btc-muted)' }">Resume matching for candidates and recruiters</p>
          </div>
        </div>

        <div class="flex items-center gap-2 md:gap-4">
          <nav class="flex items-center gap-1 rounded-xl p-1" :style="{ background: 'var(--btc-bg-2)' }">
            <button
              v-for="item in navItems"
              :key="item.to"
              class="relative rounded-lg px-3 py-1.5 text-xs font-semibold transition hover:opacity-80"
              :style="{ color: 'var(--btc-ink)' }"
              @click="router.push(item.to)"
            >
              <span class="inline-flex items-center gap-1">
                <span>{{ item.label }}</span>
                <span
                  v-if="item.to === '/notifications' && unreadNotifications"
                  class="absolute left-1 top-1 h-2 w-2 rounded-full bg-rose-600"
                >
                </span>
              </span>
            </button>
          </nav>
          <div class="hidden text-right md:block">
            <p class="text-sm font-semibold">
              <span>{{ auth.user?.name }}</span>
              <span
                v-if="isVip"
                class="ml-2 inline-flex items-center rounded-lg border px-2 py-0.5 text-[11px] font-bold"
                :style="{ borderColor: 'var(--btc-border)', background: 'var(--btc-bg-2)', color: 'var(--btc-primary)' }"
              >
                VIP
              </span>
            </p>
            <p class="text-xs uppercase tracking-wide font-medium" :style="{ color: 'var(--btc-muted)' }">{{ auth.role }}</p>
          </div>
          <button
            @click="toggleTheme"
            class="flex h-9 w-9 items-center justify-center rounded-xl border transition hover:-translate-y-0.5"
            :style="{ background: 'var(--btc-surface)', borderColor: 'var(--btc-border)', color: 'var(--btc-ink)' }"
            :title="isDark ? 'Chuyển sang nền sáng' : 'Chuyển sang nền tối'"
          >
            <span v-if="isDark">☀️</span>
            <span v-else>🌙</span>
          </button>
          <button class="btc-btn-secondary" @click="logout">Logout</button>
        </div>
      </div>
    </header>

    <main class="btc-shell mt-6">
      <slot />
    </main>
  </div>
</template>
