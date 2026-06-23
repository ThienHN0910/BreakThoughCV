<script setup>
import { computed, onMounted } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import { useNotificationsStore } from '../stores/notifications'

const notifications = useNotificationsStore()

onMounted(() => {
  notifications.ensureLoaded()
  notifications.markAllRead()
})

const items = computed(() => notifications.list)

function clearAll() {
  notifications.clearAll()
}

function formatDate(value) {
  try {
    return new Date(value).toLocaleString()
  } catch {
    return value
  }
}

function typeClass(type) {
  if (type === 'success') return 'bg-emerald-50 text-emerald-700 border-emerald-100'
  if (type === 'warning') return 'bg-amber-50 text-amber-700 border-amber-100'
  return 'bg-blue-50 text-blue-700 border-blue-100'
}

function typeLabel(type) {
  if (type === 'success') return 'Thành công'
  if (type === 'warning') return 'Cảnh báo'
  return 'Thông tin'
}
</script>

<template>
  <AppLayout>
    <div class="btc-page">
      <div class="flex items-start justify-between gap-4 border-b border-slate-100 pb-6">
        <div>
          <h1 class="btc-page-title !mb-1">Thông báo</h1>
          <p class="btc-page-subtitle">Theo dõi các sự kiện gần đây: đăng nhập, nộp CV, mua gói AI, cập nhật trạng thái hồ sơ.</p>
        </div>

        <button v-if="items.length" class="btc-btn-secondary !text-rose-600 !border-rose-200 hover:!bg-rose-50" type="button" @click="clearAll">Xóa tất cả</button>
      </div>

      <div v-if="!items.length" class="btc-card mt-8 text-center py-12">
        <svg class="w-12 h-12 text-slate-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path></svg>
        <p class="text-slate-500 font-medium">Chưa có thông báo nào.</p>
      </div>

      <div v-else class="mt-6 grid gap-4 max-w-4xl mx-auto">
        <div v-for="n in items" :key="n.id" class="btc-card relative overflow-hidden">
          <!-- Decorator line based on type -->
          <div class="absolute left-0 top-0 bottom-0 w-1" :class="{'bg-emerald-400': n.type === 'success', 'bg-amber-400': n.type === 'warning', 'bg-blue-400': n.type !== 'success' && n.type !== 'warning'}"></div>
          
          <div class="pl-2 flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-3">
              <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-bold uppercase tracking-wider border" :class="typeClass(n.type)">{{ typeLabel(n.type) }}</span>
              <h3 class="text-base font-bold text-slate-800">{{ n.title }}</h3>
            </div>
            <span class="text-xs font-medium text-slate-400 bg-slate-50 px-2 py-1 rounded-md">{{ formatDate(n.createdAt) }}</span>
          </div>

          <p v-if="n.message" class="mt-3 pl-2 text-sm text-slate-600 leading-relaxed">{{ n.message }}</p>
          <div class="mt-4 pl-2" v-if="n.href">
            <RouterLink class="inline-flex items-center gap-1.5 text-sm font-semibold text-indigo-600 hover:text-indigo-800 transition-colors" :to="n.href">
              Xem chi tiết
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3"></path></svg>
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
