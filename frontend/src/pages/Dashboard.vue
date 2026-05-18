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
    <h2 class="text-2xl font-bold mb-4">Xin chào, {{ auth.user?.name }}</h2>
    <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <RouterLink
        v-for="item in links"
        :key="item.to"
        :to="item.to"
        class="block p-4 rounded-lg border bg-white hover:border-slate-400"
      >
        {{ item.label }}
      </RouterLink>
    </div>
  </AppLayout>
</template>
