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
    <h2 class="text-2xl font-bold mb-4">Quản lý công ty</h2>
    <div class="bg-white border rounded-lg p-4 grid gap-3 max-w-2xl">
      <input v-model="form.name" class="border rounded p-2" placeholder="Tên công ty" />
      <textarea v-model="form.description" class="border rounded p-2" rows="4" placeholder="Mô tả"></textarea>
      <input v-model="form.website" class="border rounded p-2" placeholder="Website" />
      <select v-model="form.categoryId" class="border rounded p-2">
        <option value="">Chọn danh mục</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input type="file" accept="image/*" @change="(e) => (logo = e.target.files[0])" />
      <button class="bg-slate-900 text-white rounded p-2" @click="save">Lưu</button>
      <p v-if="message" class="text-green-600 text-sm">{{ message }}</p>
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    </div>
  </AppLayout>
</template>
