<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const links = computed(() =>
  auth.role === 'recruiter'
    ? [
        { to: '/recruiter/company', label: 'Quản lý công ty' },
        { to: '/recruiter/jobs', label: 'Quản lý tin tuyển dụng' },
        { to: '/recruiter/applications', label: 'Quản lý ứng viên' }
      ]
    : [
        { to: '/jobs', label: 'Tìm kiếm công việc' },
        { to: '/candidate/cv', label: 'Quản lý CV' },
        { to: '/candidate/ai-review', label: 'AI Review CV' }
      ]
)
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Xin chào, {{ auth.user?.name }}</h2>
    <p class="btc-page-subtitle">Truy cập nhanh các tính năng phù hợp với vai trò hiện tại của bạn.</p>

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <RouterLink
        v-for="item in links"
        :key="item.to"
        :to="item.to"
        class="btc-card block transition hover:-translate-y-0.5 hover:border-blue-300"
      >
        <p class="text-xs font-semibold uppercase tracking-wider text-blue-700">Feature</p>
        <p class="mt-1 text-lg font-semibold">{{ item.label }}</p>
      </RouterLink>
    </div>
  </AppLayout>
</template>
