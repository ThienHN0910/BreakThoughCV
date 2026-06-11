<script setup>
import { onMounted, ref, watch } from 'vue'
import AppLayout from '../../layouts/AppLayout.vue'
import api from '../../services/api'

const users = ref([])
const total = ref(0)
const stats = ref({
  totalUsers: 0,
  candidateCount: 0,
  recruiterCount: 0,
  adminCount: 0,
  noneRoleCount: 0
})
const page = ref(1)
const pageSize = ref(20)
const search = ref('')
const roleFilter = ref('')
const loading = ref(false)
const error = ref('')
const updatingId = ref('')

const roleOptions = [
  { value: 'none', label: 'Chưa chọn' },
  { value: 'candidate', label: 'Ứng viên' },
  { value: 'recruiter', label: 'Nhà tuyển dụng' },
  { value: 'admin', label: 'Quản trị viên' }
]

let searchTimer = null

async function loadUsers() {
  try {
    loading.value = true
    error.value = ''
    const { data } = await api.get('/admin/users', {
      params: {
        page: page.value,
        pageSize: pageSize.value,
        search: search.value.trim() || undefined,
        role: roleFilter.value || undefined
      }
    })
    users.value = data.items
    total.value = data.total
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được danh sách người dùng'
    users.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

async function loadStats() {
  try {
    const { data } = await api.get('/admin/users/stats')
    stats.value = {
      totalUsers: data.totalUsers || 0,
      candidateCount: data.candidateCount || 0,
      recruiterCount: data.recruiterCount || 0,
      adminCount: data.adminCount || 0,
      noneRoleCount: data.noneRoleCount || 0
    }
  } catch {
    stats.value = {
      totalUsers: 0,
      candidateCount: 0,
      recruiterCount: 0,
      adminCount: 0,
      noneRoleCount: 0
    }
  }
}

async function updateRole(user, newRole) {
  if (user.role === newRole) return
  try {
    updatingId.value = user.id
    error.value = ''
    await api.put(`/admin/users/${user.id}/role`, { role: newRole })
    user.role = newRole
    await loadStats()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không cập nhật được vai trò'
    await loadUsers()
  } finally {
    updatingId.value = ''
  }
}

async function toggleStatus(user) {
  try {
    updatingId.value = user.id
    error.value = ''
    const { data } = await api.put(`/admin/users/${user.id}/status`, { isActive: !user.isActive })
    Object.assign(user, data)
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không cập nhật được trạng thái'
  } finally {
    updatingId.value = ''
  }
}

function formatDate(value, mode = 'date') {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'

  if (mode === 'datetime') {
    return date.toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

function totalPages() {
  return Math.max(1, Math.ceil(total.value / pageSize.value))
}

function goPage(next) {
  const max = totalPages()
  page.value = Math.min(Math.max(1, next), max)
  loadUsers()
}

watch([roleFilter], () => {
  page.value = 1
  loadUsers()
})

watch(search, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    page.value = 1
    loadUsers()
  }, 350)
})

onMounted(loadUsers)
onMounted(loadStats)
</script>

<template>
  <AppLayout>
    <section class="btc-card">
      <div class="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 class="text-2xl font-bold">Quản lý người dùng</h2>
          <p class="mt-1 text-sm text-slate-500">Theo dõi tài khoản, vai trò, trạng thái và dữ liệu hoạt động.</p>
        </div>
       
      </div>

      <div class="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <div class="rounded-xl border border-slate-200 bg-slate-50 p-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-slate-500">Tổng người dùng</p>
          <p class="mt-1 text-2xl font-bold text-slate-900">{{ stats.totalUsers }}</p>
        </div>
        <div class="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-emerald-700">Ứng viên</p>
          <p class="mt-1 text-2xl font-bold text-emerald-800">{{ stats.candidateCount }}</p>
        </div>
        <div class="rounded-xl border border-blue-200 bg-blue-50 p-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-blue-700">Nhà tuyển dụng</p>
          <p class="mt-1 text-2xl font-bold text-blue-800">{{ stats.recruiterCount }}</p>
        </div>
        <div class="rounded-xl border border-violet-200 bg-violet-50 p-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-violet-700">Admin</p>
          <p class="mt-1 text-2xl font-bold text-violet-800">{{ stats.adminCount }}</p>
        </div>
        <div class="rounded-xl border border-amber-200 bg-amber-50 p-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-amber-700">Chưa chọn role</p>
          <p class="mt-1 text-2xl font-bold text-amber-800">{{ stats.noneRoleCount }}</p>
        </div>
      </div>

      <div class="mt-5 flex flex-col gap-3 md:flex-row">
        <input
          v-model="search"
          type="search"
          placeholder="Tìm theo email hoặc tên..."
          class="w-full rounded-xl border border-slate-200 px-4 py-2 text-sm outline-none focus:border-slate-400 md:max-w-sm"
        />
        <select
          v-model="roleFilter"
          class="rounded-xl border border-slate-200 px-4 py-2 text-sm outline-none focus:border-slate-400"
        >
          <option value="">Tất cả vai trò</option>
          <option v-for="opt in roleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
        </select>
      </div>

      <p v-if="error" class="mt-4 text-sm text-rose-600">{{ error }}</p>
      <p v-if="loading" class="mt-4 text-sm text-slate-500">Đang tải...</p>

      <div class="mt-5 overflow-x-auto rounded-xl border border-slate-200">
        <table class="min-w-full text-left text-sm">
          <thead class="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3">Người dùng</th>
              <th class="px-4 py-3">Vai trò</th>
              <th class="px-4 py-3">Trạng thái</th>
              <th class="px-4 py-3">Đăng nhập cuối</th>
              <th class="px-4 py-3">Upload CV</th>
              <th class="px-4 py-3">AI Review</th>
              <th class="px-4 py-3">AI Access</th>
              <th class="px-4 py-3">Ngày tạo</th>
              <th class="px-4 py-3">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id" class="border-t border-slate-100">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <img
                    v-if="user.avatarUrl"
                    :src="user.avatarUrl"
                    :alt="user.name"
                    class="h-9 w-9 rounded-full object-cover"
                  />
                  <div
                    v-else
                    class="flex h-9 w-9 items-center justify-center rounded-full bg-slate-200 text-xs font-bold text-slate-600"
                  >
                    {{ user.name?.charAt(0) || '?' }}
                  </div>
                  <div>
                    <p class="font-semibold text-slate-800">{{ user.name }}</p>
                    <p class="text-xs text-slate-500">{{ user.email }}</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3">
                <select
                  :value="user.role"
                  :disabled="updatingId === user.id"
                  class="rounded-lg border border-slate-200 px-2 py-1 text-xs outline-none focus:border-slate-400"
                  @change="updateRole(user, $event.target.value)"
                >
                  <option v-for="opt in roleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              </td>
              <td class="px-4 py-3">
                <span
                  class="inline-flex rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="user.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'"
                >
                  {{ user.isActive ? 'Hoạt động' : 'Vô hiệu' }}
                </span>
              </td>
              <td class="px-4 py-3 text-slate-600">{{ formatDate(user.lastLoginAt, 'datetime') }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex rounded-lg bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-700">
                  {{ user.cvUploadCount || 0 }} lần
                </span>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex rounded-lg bg-blue-100 px-2 py-1 text-xs font-semibold text-blue-700">
                  {{ user.aiReviewCount || 0 }} lần
                </span>
              </td>
              <td class="px-4 py-3">
                <span
                  class="inline-flex rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="user.aiAccessEnabled ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-600'"
                >
                  {{ user.aiAccessEnabled ? 'Có' : 'Không' }}
                </span>
              </td>
              <td class="px-4 py-3 text-slate-600">{{ formatDate(user.createdAt) }}</td>
              <td class="px-4 py-3">
                <button
                  class="rounded-lg px-3 py-1 text-xs font-semibold transition"
                  :class="user.isActive ? 'bg-rose-50 text-rose-700 hover:bg-rose-100' : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'"
                  :disabled="updatingId === user.id"
                  @click="toggleStatus(user)"
                >
                  {{ user.isActive ? 'Vô hiệu hóa' : 'Kích hoạt' }}
                </button>
              </td>
            </tr>
            <tr v-if="!loading && users.length === 0">
              <td colspan="9" class="px-4 py-8 text-center text-slate-500">Không có người dùng phù hợp.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="totalPages() > 1" class="mt-4 flex items-center justify-center gap-2">
        <button class="btc-btn-secondary px-3 py-1 text-xs" :disabled="page <= 1" @click="goPage(page - 1)">Trước</button>
        <span class="text-sm text-slate-600">Trang {{ page }} / {{ totalPages() }}</span>
        <button class="btc-btn-secondary px-3 py-1 text-xs" :disabled="page >= totalPages()" @click="goPage(page + 1)">Sau</button>
      </div>
    </section>
  </AppLayout>
</template>
