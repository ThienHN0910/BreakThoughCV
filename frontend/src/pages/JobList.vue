<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'

const jobs = ref([])
const categories = ref([])
const loading = ref(false)
const categoryId = ref('')
const keyword = ref('')
const error = ref('')

async function loadCategories() {
  const { data } = await api.get('/categories')
  categories.value = data
}

async function loadJobs() {
  try {
    loading.value = true
    const { data } = await api.get('/jobs', { params: { categoryId: categoryId.value || undefined, keyword: keyword.value || undefined } })
    jobs.value = data.data || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể tải danh sách công việc'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadCategories(), loadJobs()])
})
</script>

<template>
  <AppLayout>
    <h2 class="text-2xl font-bold mb-4">Tìm kiếm việc làm</h2>
    <div class="grid md:grid-cols-3 gap-3 mb-4">
      <select v-model="categoryId" class="border rounded p-2 bg-white">
        <option value="">Tất cả danh mục</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input v-model="keyword" class="border rounded p-2 bg-white" placeholder="Tìm theo title hoặc skill" />
      <button class="bg-slate-900 text-white rounded p-2" @click="loadJobs">Lọc</button>
    </div>

    <p v-if="error" class="text-red-600 mb-3">{{ error }}</p>
    <p v-if="loading">Đang tải...</p>

    <div class="grid gap-3">
      <div v-for="job in jobs" :key="job.id" class="bg-white border rounded-lg p-4">
        <h3 class="font-semibold text-lg">{{ job.title }}</h3>
        <p class="text-sm text-slate-500">{{ job.companyName }} • {{ job.categoryName || 'Chưa phân loại' }}</p>
        <p class="mt-2 text-sm">{{ job.description }}</p>
        <div class="mt-2 flex flex-wrap gap-2 text-xs">
          <span v-for="s in job.mustHaveSkills" :key="s" class="px-2 py-1 bg-teal-100 rounded">{{ s }}</span>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
