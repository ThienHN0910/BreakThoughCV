<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'

const form = ref({ name: '', description: '', website: '', categoryId: '' })
const logo = ref(null)
const categories = ref([])
const message = ref('')
const error = ref('')

async function loadData() {
  const [{ data: cats }] = await Promise.all([api.get('/categories')])
  categories.value = cats
  try {
    const { data } = await api.get('/companies/my')
    form.value = {
      name: data.name || '',
      description: data.description || '',
      website: data.website || '',
      categoryId: data.categoryId || ''
    }
  } catch {
    // no company yet
  }
}

async function save() {
  const fd = new FormData()
  Object.entries(form.value).forEach(([k, v]) => fd.append(k, v || ''))
  if (logo.value) fd.append('logo', logo.value)

  try {
    await api.post('/companies', fd)
    message.value = 'Lưu thông tin công ty thành công'
    error.value = ''
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể lưu công ty'
  }
}

onMounted(loadData)
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Quản lý công ty</h2>
    <p class="btc-page-subtitle">Tạo hoặc cập nhật hồ sơ công ty để đăng tuyển dụng.</p>

    <div class="btc-card max-w-3xl">
      <div class="grid gap-6">
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Tên công ty</label>
          <input v-model="form.name" class="btc-input" placeholder="Nhập tên công ty..." />
        </div>
        
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Mô tả</label>
          <textarea v-model="form.description" class="btc-input" rows="4" placeholder="Giới thiệu về công ty..."></textarea>
        </div>

        <div class="grid md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1.5">Website</label>
            <input v-model="form.website" class="btc-input" placeholder="https://..." />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1.5">Danh mục</label>
            <select v-model="form.categoryId" class="btc-input">
              <option value="">Chọn danh mục</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Logo công ty</label>
          <div class="flex items-center gap-4">
            <label class="flex-1 border-2 border-dashed border-indigo-200 bg-slate-50/50 hover:bg-slate-50 transition-colors rounded-xl p-4 text-center cursor-pointer">
              <input type="file" accept="image/*" @change="(e) => (logo = e.target.files[0] || null)" class="hidden" />
              <div class="flex flex-col items-center gap-1">
                <svg class="w-6 h-6 text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path></svg>
                <span class="text-sm font-medium text-slate-600">Tải ảnh lên</span>
              </div>
            </label>
            <div v-if="logo" class="flex items-center gap-2 text-sm text-emerald-600 font-medium px-3 py-2 bg-emerald-50 rounded-lg border border-emerald-100">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
              Đã chọn ảnh
            </div>
          </div>
        </div>

        <div class="pt-2 border-t border-slate-100 flex items-center justify-between">
          <div class="flex-1">
            <p v-if="message" class="text-sm font-medium text-emerald-600 inline-flex items-center gap-1.5"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>{{ message }}</p>
            <p v-if="error" class="text-sm font-medium text-rose-600">{{ error }}</p>
          </div>
          <button class="btc-btn-primary px-8" @click="save">Lưu thay đổi</button>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
