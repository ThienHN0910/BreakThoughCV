<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

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
      { label: 'Công ty', value: '1', color: 'from-blue-500 to-blue-600' },
      { label: 'Tin tuyển dụng', value: '3', color: 'from-purple-500 to-purple-600' },
      { label: 'Ứng viên', value: '12', color: 'from-pink-500 to-pink-600' }
    ]
  } else {
    return [
      { label: 'Công việc tìm thấy', value: '24', color: 'from-green-500 to-green-600' },
      { label: 'CV đã tải', value: '1', color: 'from-orange-500 to-orange-600' },
      { label: 'Hồ sơ nộp', value: '5', color: 'from-red-500 to-red-600' }
    ]
  }
})
</script>

<template>
  <AppLayout>
    <!-- Header -->
    <div class="mb-10">
      <h1 class="btc-page-title text-4xl">👋 Xin chào, {{ auth.user?.name }}</h1>
      <p class="btc-page-subtitle text-lg mt-3">
        {{ auth.role === 'recruiter' ? 'Quản lý công ty và tân tuyển dụng của bạn' : 'Tìm công việc phù hợp và cải thiện CV' }}
      </p>
    </div>

    <!-- Stats Section -->
    <div class="grid gap-4 sm:grid-cols-3 mb-10">
      <div
        v-for="stat in stats"
        :key="stat.label"
        class="btc-card"
      >
        <p class="text-sm font-medium text-gray-600 uppercase tracking-wider mb-2">{{ stat.label }}</p>
        <div class="flex items-baseline gap-2">
          <p class="text-4xl font-bold bg-gradient-to-r" :class="`bg-gradient-to-r ${stat.color} bg-clip-text text-transparent`">
            {{ stat.value }}
          </p>
        </div>
      </div>
    </div>

    <!-- Features Grid -->
    <div class="mb-6">
      <h2 class="text-xl font-bold mb-5">Tính năng chính</h2>
      <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        <RouterLink
          v-for="item in links"
          :key="item.to"
          :to="item.to"
          class="group btc-card block overflow-hidden transition-all duration-300 hover:-translate-y-2 hover:shadow-lg"
        >
          <div class="flex items-start justify-between mb-3">
            <div class="text-4xl">{{ item.icon }}</div>
            <div class="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500/20 to-cyan-500/20 flex items-center justify-center group-hover:from-blue-500/30 group-hover:to-cyan-500/30 transition-colors">
              <svg class="w-5 h-5 text-blue-600 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
              </svg>
            </div>
          </div>
          <h3 class="text-lg font-semibold mb-2 group-hover:text-blue-600 transition-colors">
            {{ item.label }}
          </h3>
          <p class="text-sm text-gray-600">{{ item.description }}</p>
        </RouterLink>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="btc-card bg-gradient-to-r from-blue-50 to-cyan-50 border-blue-200">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h3 class="text-lg font-semibold mb-1">🚀 Bắt đầu ngay</h3>
          <p class="text-sm text-gray-600">
            {{ auth.role === 'recruiter' ? 'Tạo tin tuyển dụng đầu tiên để bắt đầu tuyển dụng' : 'Khám phá các công việc hôm nay' }}
          </p>
        </div>
        <RouterLink
          :to="auth.role === 'recruiter' ? '/recruiter/jobs' : '/jobs'"
          class="btc-btn-primary whitespace-nowrap"
        >
          {{ auth.role === 'recruiter' ? 'Tạo tin mới' : 'Xem công việc' }}
        </RouterLink>
      </div>
    </div>
  </AppLayout>
</template>
