<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import { useAuthStore } from '../stores/auth'
import heroImage from '../assets/hero.png'

const auth = useAuthStore()

const showWelcome = ref(false)
const fireCanvas = ref(null)
let welcomeTimer = null
let fireRaf = null
let fireStopTimer = null

function startWelcomeTimer() {
  if (welcomeTimer) {
    clearTimeout(welcomeTimer)
    welcomeTimer = null
  }
  welcomeTimer = setTimeout(() => {
    showWelcome.value = false
    welcomeTimer = null
  }, 3000)
}

function stopFireworks() {
  if (fireRaf) {
    cancelAnimationFrame(fireRaf)
    fireRaf = null
  }
  if (fireStopTimer) {
    clearTimeout(fireStopTimer)
    fireStopTimer = null
  }
  const canvas = fireCanvas.value
  if (!canvas) return
  const ctx = canvas.getContext?.('2d')
  if (!ctx) return
  ctx.clearRect(0, 0, canvas.width, canvas.height)
}

function startFireworks() {
  stopFireworks()

  const canvas = fireCanvas.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const dpr = Math.max(1, window.devicePixelRatio || 1)
  const rect = canvas.getBoundingClientRect()
  const width = Math.max(1, Math.floor(rect.width))
  const height = Math.max(1, Math.floor(rect.height))
  canvas.width = Math.floor(width * dpr)
  canvas.height = Math.floor(height * dpr)
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0)

  const rootStyle = getComputedStyle(document.documentElement)
  const primary = (rootStyle.getPropertyValue('--btc-primary') || '').trim() || '#0f172a'
  const ink = (rootStyle.getPropertyValue('--btc-ink') || '').trim() || primary
  const colors = [primary, ink]

  const particles = []
  const gravity = 520
  const drag = 0.985

  const burst = (x, y) => {
    const count = 38
    for (let i = 0; i < count; i += 1) {
      const angle = Math.random() * Math.PI * 2
      const speed = 160 + Math.random() * 180
      particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        life: 0.9 + Math.random() * 0.6,
        age: 0,
        size: 1.2 + Math.random() * 1.6,
        color: colors[i % colors.length]
      })
    }
  }

  // 2 bursts near the left side (as requested: top-left-ish)
  burst(width * 0.18, height * 0.35)
  setTimeout(() => burst(width * 0.28, height * 0.55), 220)

  let last = performance.now()

  const frame = (now) => {
    const dt = Math.min(0.033, (now - last) / 1000)
    last = now

    ctx.clearRect(0, 0, width, height)

    for (let i = particles.length - 1; i >= 0; i -= 1) {
      const p = particles[i]
      p.age += dt
      if (p.age >= p.life) {
        particles.splice(i, 1)
        continue
      }

      p.vx *= drag
      p.vy = p.vy * drag + gravity * dt
      p.x += p.vx * dt
      p.y += p.vy * dt

      const t = 1 - p.age / p.life
      ctx.globalAlpha = Math.max(0, Math.min(1, t))
      ctx.fillStyle = p.color
      ctx.beginPath()
      ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2)
      ctx.fill()
    }
    ctx.globalAlpha = 1

    if (particles.length) {
      fireRaf = requestAnimationFrame(frame)
    } else {
      stopFireworks()
    }
  }

  fireRaf = requestAnimationFrame(frame)

  // Safety stop
  fireStopTimer = setTimeout(() => stopFireworks(), 1400)
}

onMounted(() => {
  try {
    const pending = sessionStorage.getItem('welcomePending')
    if (!pending) return
    sessionStorage.removeItem('welcomePending')
  } catch {
    // ignore
  }

  showWelcome.value = true
  startWelcomeTimer()
  nextTick(() => startFireworks())
})

onBeforeUnmount(() => {
  if (welcomeTimer) clearTimeout(welcomeTimer)
  stopFireworks()
})

const recruiterFeatures = [
  {
    to: '/recruiter/company',
    label: 'Quản lý công ty',
    icon: '🏢',
    description: 'Cập nhật thông tin công ty và hình ảnh logo' 
  },
  {
    to: '/recruiter/jobs',
    label: 'Tin tuyển dụng',
    icon: '📋',
    description: 'Tạo và quản lý các vị trí tuyển dụng'
  },
  {
    to: '/recruiter/applications',
    label: 'Ứng viên nộp hồ sơ',
    icon: '👥',
    description: 'Xem và quản lý hồ sơ ứng viên'
  }
]

const candidateFeatures = [
  {
    to: '/jobs',
    label: 'Tìm kiếm công việc',
    icon: '🔍',
    description: 'Khám phá các vị trí tuyển dụng phù hợp'
  },
  {
    to: '/candidate/cv',
    label: 'Quản lý CV',
    icon: '📄',
    description: 'Tải lên và quản lý hồ sơ CV của bạn'
  },
  {
    to: '/candidate/ai-review',
    label: 'AI Review CV',
    icon: '✨',
    description: 'Nhận đề xuất cải thiện CV từ AI'
  }
]

const links = computed(() => auth.role === 'recruiter' ? recruiterFeatures : candidateFeatures)

const stats = computed(() => {
  if (auth.role === 'recruiter') {
    return [
      { label: 'Công ty', value: '1' },
      { label: 'Tin tuyển dụng', value: '3' },
      { label: 'Ứng viên', value: '12' }
    ]
  } else {
    return [
      { label: 'Công việc tìm thấy', value: '24' },
      { label: 'CV đã tải', value: '1' },
      { label: 'Hồ sơ nộp', value: '5' }
    ]
  }
})

const primaryCta = computed(() => {
  return auth.role === 'recruiter'
    ? { to: '/recruiter/jobs', label: 'Tạo tin tuyển dụng' }
    : { to: '/jobs', label: 'Xem việc làm' }
})

const secondaryCta = computed(() => {
  return auth.role === 'recruiter'
    ? { to: '/recruiter/company', label: 'Quản lý công ty' }
    : { to: '/candidate/cv', label: 'Quản lý CV' }
})
</script>

<template>
  <AppLayout>
    <Transition name="btc-welcome">
      <div
        v-if="showWelcome"
        class="btc-card relative mb-6 flex flex-wrap items-center justify-between gap-3 overflow-hidden"
        role="status"
        :style="{ borderColor: 'var(--btc-border)', background: 'var(--btc-bg-2)' }"
      >
        <canvas ref="fireCanvas" class="btc-fireworks" aria-hidden="true"></canvas>
        <div>
          <p class="text-sm font-semibold" :style="{ color: 'var(--btc-ink)' }">Chào mừng trở lại, {{ auth.user?.name || 'bạn' }}.</p>
          <p class="text-xs" :style="{ color: 'var(--btc-muted)' }">Chúc bạn một ngày làm việc hiệu quả.</p>
        </div>
        <button class="btc-btn-secondary" type="button" @click="showWelcome = false">Đóng</button>
      </div>
    </Transition>

    <!-- Hero -->
    <div class="btc-card mb-8 overflow-hidden">
      <div class="grid gap-6 md:grid-cols-2 md:items-center">
        <div>
          <h1 class="btc-page-title">
            Chào mừng trở lại, {{ auth.user?.name || 'bạn' }}
          </h1>
          <p class="btc-page-subtitle">
            {{ auth.role === 'recruiter'
              ? 'Quản lý công ty, đăng tin tuyển dụng và theo dõi ứng viên trên một nơi.'
              : 'Tìm việc phù hợp, quản lý CV và nhận góp ý AI để tối ưu hồ sơ.'
            }}
          </p>

          <div class="flex flex-wrap gap-3">
            <RouterLink :to="primaryCta.to" class="btc-btn-primary">
              {{ primaryCta.label }}
            </RouterLink>
            <RouterLink :to="secondaryCta.to" class="btc-btn-secondary">
              {{ secondaryCta.label }}
            </RouterLink>
          </div>
        </div>

        <div class="md:justify-self-end">
          <img
            :src="heroImage"
            alt="BreakThroughCV"
            class="w-full max-w-xl rounded-2xl border"
            :style="{ borderColor: 'var(--btc-border)' }"
            loading="lazy"
          />
        </div>
      </div>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 sm:grid-cols-3 mb-8">
      <div
        v-for="stat in stats"
        :key="stat.label"
        class="btc-card"
      >
        <p class="text-xs font-semibold uppercase tracking-wider" :style="{ color: 'var(--btc-muted)' }">{{ stat.label }}</p>
        <p class="mt-1 text-3xl font-extrabold" :style="{ color: 'var(--btc-ink)' }">{{ stat.value }}</p>
      </div>
    </div>

    <!-- Features -->
    <div class="mb-6">
      <h2 class="text-xl font-bold mb-5">Tính năng chính</h2>
      <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        <RouterLink
          v-for="item in links"
          :key="item.to"
          :to="item.to"
          class="group btc-card block overflow-hidden transition-transform hover:-translate-y-1"
        >
          <div class="flex items-start justify-between mb-3">
            <div class="text-4xl">{{ item.icon }}</div>
            <div class="text-sm font-semibold" :style="{ color: 'var(--btc-muted)' }">→</div>
          </div>
          <h3 class="text-lg font-semibold mb-2" :style="{ color: 'var(--btc-ink)' }">
            {{ item.label }}
          </h3>
          <p class="text-sm" :style="{ color: 'var(--btc-muted)' }">{{ item.description }}</p>
        </RouterLink>
      </div>
    </div>

    <!-- Quick Action -->
    <div class="btc-card">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h3 class="text-lg font-semibold mb-1">Bắt đầu ngay</h3>
          <p class="text-sm" :style="{ color: 'var(--btc-muted)' }">
            {{ auth.role === 'recruiter' ? 'Tạo tin tuyển dụng để tiếp cận ứng viên.' : 'Chọn job, review CV bằng AI và apply nhanh.' }}
          </p>
        </div>
        <RouterLink :to="primaryCta.to" class="btc-btn-primary whitespace-nowrap">
          {{ primaryCta.label }}
        </RouterLink>
      </div>
    </div>
  </AppLayout>
</template>

<style scoped>
.btc-welcome-enter-active,
.btc-welcome-leave-active {
  transition: opacity 220ms ease, transform 220ms ease;
}

.btc-welcome-enter-from,
.btc-welcome-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

.btc-fireworks {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  opacity: 0.9;
}
</style>
