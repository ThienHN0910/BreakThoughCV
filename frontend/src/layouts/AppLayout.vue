<script setup>
import { computed, watch } from 'vue'
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
</script>

<template>
  <div class="min-h-screen pb-8">
    <header class="sticky top-0 z-20 border-b border-slate-200/70 bg-white/75 backdrop-blur-md">
      <div class="btc-shell flex flex-col gap-3 py-3 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-3">
          <button class="rounded-xl bg-slate-900 px-3 py-1 text-sm font-bold text-white" @click="router.push('/')">BTCV</button>
          <div>
            <h1 class="text-base font-bold md:text-lg">BreakThroughCV</h1>
            <p class="text-xs text-slate-500">Resume matching for candidates and recruiters</p>
          </div>
        </div>

        <div class="flex items-center gap-2 md:gap-4">
          <nav class="flex items-center gap-1 rounded-xl bg-slate-100/80 p-1">
            <button
              v-for="item in navItems"
              :key="item.to"
              class="relative rounded-lg px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-white"
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
            <p class="text-xs uppercase tracking-wide text-slate-500">{{ auth.role }}</p>
          </div>
          <button class="btc-btn-secondary" @click="logout">Logout</button>
        </div>
      </div>
    </header>

    <main class="btc-shell mt-6">
      <slot />
    </main>
  </div>
</template>
