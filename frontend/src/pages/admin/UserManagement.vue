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

      <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
          <p class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-1">Tổng người dùng</p>
          <p class="text-3xl font-extrabold text-slate-900">{{ stats.totalUsers }}</p>
        </div>
        <div class="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 shadow-sm hover:shadow-md transition-shadow">
          <p class="text-xs font-bold uppercase tracking-wider text-emerald-700 mb-1">Ứng viên</p>
          <p class="text-3xl font-extrabold text-emerald-800">{{ stats.candidateCount }}</p>
        </div>
        <div class="rounded-2xl border border-blue-200 bg-blue-50 p-5 shadow-sm hover:shadow-md transition-shadow">
          <p class="text-xs font-bold uppercase tracking-wider text-blue-700 mb-1">Nhà tuyển dụng</p>
          <p class="text-3xl font-extrabold text-blue-800">{{ stats.recruiterCount }}</p>
        </div>
        <div class="rounded-2xl border border-violet-200 bg-violet-50 p-5 shadow-sm hover:shadow-md transition-shadow">
          <p class="text-xs font-bold uppercase tracking-wider text-violet-700 mb-1">Admin</p>
          <p class="text-3xl font-extrabold text-violet-800">{{ stats.adminCount }}</p>
        </div>
        <div class="rounded-2xl border border-amber-200 bg-amber-50 p-5 shadow-sm hover:shadow-md transition-shadow">
          <p class="text-xs font-bold uppercase tracking-wider text-amber-700 mb-1">Chưa chọn role</p>
          <p class="text-3xl font-extrabold text-amber-800">{{ stats.noneRoleCount }}</p>
        </div>
      </div>

      <div class="mt-8 flex flex-col gap-4 md:flex-row items-center">
        <div class="relative w-full md:max-w-sm">
          <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <svg class="h-4 w-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
          </div>
          <input
            v-model="search"
            type="search"
            placeholder="Tìm theo email hoặc tên..."
            class="w-full rounded-xl border border-slate-200 pl-10 pr-4 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 transition-all"
          />
        </div>
        <select
          v-model="roleFilter"
          class="w-full md:w-auto rounded-xl border border-slate-200 px-4 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 transition-all"
        >
          <option value="">Tất cả vai trò</option>
          <option v-for="opt in roleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
        </select>
      </div>

      <p v-if="error" class="mt-4 text-sm text-rose-600">{{ error }}</p>
      <p v-if="loading" class="mt-4 text-sm text-slate-500">Đang tải...</p>

      <div class="mt-6 overflow-x-auto rounded-xl border border-slate-200">
        <table class="min-w-full text-left text-sm whitespace-nowrap">
          <thead class="bg-slate-50 text-xs font-bold uppercase tracking-wider text-slate-500 border-b border-slate-200">
            <tr>
              <th class="px-5 py-3">Người dùng</th>
              <th class="px-5 py-3">Vai trò</th>
              <th class="px-5 py-3">Trạng thái</th>
              <th class="px-5 py-3">Đăng nhập cuối</th>
              <th class="px-5 py-3 text-center">Upload CV</th>
              <th class="px-5 py-3 text-center">AI Review</th>
              <th class="px-5 py-3 text-center">AI Access</th>
              <th class="px-5 py-3">Ngày tạo</th>
              <th class="px-5 py-3 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 bg-white">
            <tr v-for="user in users" :key="user.id" class="hover:bg-slate-50/80 transition-colors">
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
              <td class="px-5 py-4">
                <select
                  :value="user.role"
                  :disabled="updatingId === user.id"
                  class="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium bg-white outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all"
                  @change="updateRole(user, $event.target.value)"
                >
                  <option v-for="opt in roleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              </td>
              <td class="px-5 py-4">
                <span
                  class="inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider border"
                  :class="user.isActive ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-rose-50 text-rose-700 border-rose-200'"
                >
                  {{ user.isActive ? 'Hoạt động' : 'Vô hiệu' }}
                </span>
              </td>
              <td class="px-5 py-4 text-slate-500 font-medium text-xs">{{ formatDate(user.lastLoginAt, 'datetime') }}</td>
              <td class="px-5 py-4 text-center">
                <span class="inline-flex items-center justify-center min-w-[2.5rem] rounded-md bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600 border border-slate-200">
                  {{ user.cvUploadCount || 0 }}
                </span>
              </td>
              <td class="px-5 py-4 text-center">
                <span class="inline-flex items-center justify-center min-w-[2.5rem] rounded-md bg-indigo-50 px-2 py-1 text-xs font-bold text-indigo-700 border border-indigo-100">
                  {{ user.aiReviewCount || 0 }}
                </span>
              </td>
              <td class="px-5 py-4 text-center">
                <span
                  class="inline-flex items-center justify-center rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider border"
                  :class="user.aiAccessEnabled ? 'bg-indigo-50 text-indigo-700 border-indigo-200' : 'bg-slate-50 text-slate-500 border-slate-200'"
                >
                  {{ user.aiAccessEnabled ? 'Có' : 'Không' }}
                </span>
              </td>
              <td class="px-5 py-4 text-slate-500 font-medium text-xs">{{ formatDate(user.createdAt) }}</td>
              <td class="px-5 py-4 text-right">
                <button
                  class="rounded-lg px-3 py-1.5 text-xs font-bold transition-colors border"
                  :class="user.isActive ? 'bg-white border-rose-200 text-rose-600 hover:bg-rose-50' : 'bg-white border-emerald-200 text-emerald-600 hover:bg-emerald-50'"
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
