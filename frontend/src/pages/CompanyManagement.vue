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

    <div class="btc-card grid max-w-3xl gap-3">
      <input v-model="form.name" class="btc-input" placeholder="Tên công ty" />
      <textarea v-model="form.description" class="btc-input" rows="4" placeholder="Mô tả"></textarea>
      <input v-model="form.website" class="btc-input" placeholder="Website" />
      <select v-model="form.categoryId" class="btc-input">
        <option value="">Chọn danh mục</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input class="btc-input" type="file" accept="image/*" @change="(e) => (logo = e.target.files[0] || null)" />
      <button class="btc-btn-primary w-fit" @click="save">Lưu</button>
      <p v-if="message" class="text-sm text-emerald-700">{{ message }}</p>
      <p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
    </div>
  </AppLayout>
</template>
