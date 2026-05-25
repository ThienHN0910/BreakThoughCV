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
  if (type === 'success') return 'text-green-700'
  if (type === 'warning') return 'text-rose-700'
  return 'text-slate-700'
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
      <div class="flex items-start justify-between gap-4">
        <div>
          <h1 class="btc-page-title">Thông báo</h1>
          <p class="btc-page-subtitle">Theo dõi các sự kiện gần đây: đăng nhập, nộp CV, mua gói AI, cập nhật trạng thái hồ sơ.</p>
        </div>

        <button v-if="items.length" class="btc-btn-secondary" type="button" @click="clearAll">Xóa tất cả</button>
      </div>

      <div v-if="!items.length" class="btc-card mt-6">
        <p class="text-slate-600">Chưa có thông báo nào.</p>
      </div>

      <div v-else class="mt-6 grid gap-3">
        <div v-for="n in items" :key="n.id" class="btc-card">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-2">
              <span class="text-xs font-semibold" :class="typeClass(n.type)">{{ typeLabel(n.type) }}</span>
              <h3 class="text-base font-semibold">{{ n.title }}</h3>
            </div>
            <span class="text-xs text-slate-500">{{ formatDate(n.createdAt) }}</span>
          </div>

          <p v-if="n.message" class="mt-2 text-sm text-slate-700">{{ n.message }}</p>
          <RouterLink v-if="n.href" class="mt-2 inline-block text-sm font-medium text-blue-700" :to="n.href">Xem chi tiết</RouterLink>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
