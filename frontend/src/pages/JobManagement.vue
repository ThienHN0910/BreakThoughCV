<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import TagInput from '../components/TagInput.vue'
import api from '../services/api'

const jobs = ref([])
const companyId = ref('')
const categories = ref([])
const editId = ref('')
const error = ref('')

const form = ref({
  title: '',
  categoryId: '',
  description: '',
  responsibilities: [],
  mustHaveSkills: [],
  niceToHaveSkills: [],
  minExperienceYears: 0
})

async function loadCategories() {
  const { data } = await api.get('/categories')
  categories.value = data
}

async function loadJobs() {
  const company = await api.get('/companies/my')
  companyId.value = company.data.id
  const { data } = await api.get(`/jobs/company/${companyId.value}`)
  jobs.value = data
}

function resetForm() {
  editId.value = ''
  form.value = {
    title: '', categoryId: '', description: '', responsibilities: [], mustHaveSkills: [], niceToHaveSkills: [], minExperienceYears: 0
  }
}

function edit(job) {
  editId.value = job.id
  form.value = {
    title: job.title,
    categoryId: job.categoryId || '',
    description: job.description,
    responsibilities: [...job.responsibilities],
    mustHaveSkills: [...job.mustHaveSkills],
    niceToHaveSkills: [...job.niceToHaveSkills],
    minExperienceYears: job.minExperienceYears
  }
}

async function save() {
  try {
    if (editId.value) await api.put(`/jobs/${editId.value}`, form.value)
    else await api.post('/jobs', form.value)
    await loadJobs()
    resetForm()
    error.value = ''
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể lưu job'
  }
}

async function removeJob(id) {
  await api.delete(`/jobs/${id}`)
  await loadJobs()
}

onMounted(async () => {
  await Promise.all([loadCategories(), loadJobs()])
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Đăng tuyển & quản lý Job</h2>
    <p class="btc-page-subtitle">Tạo mới hoặc cập nhật JD cho công ty của bạn.</p>
    <div class="grid lg:grid-cols-2 gap-4">
      <div class="btc-card space-y-3">
        <input v-model="form.title" class="btc-input" placeholder="Tiêu đề" />
        <select v-model="form.categoryId" class="btc-input">
          <option value="">Chọn danh mục</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <textarea v-model="form.description" class="btc-input" rows="3" placeholder="Mô tả"></textarea>
        <TagInput v-model="form.responsibilities" placeholder="Responsibilities" />
        <TagInput v-model="form.mustHaveSkills" placeholder="Must-have skills" />
        <TagInput v-model="form.niceToHaveSkills" placeholder="Nice-to-have skills" />
        <input v-model.number="form.minExperienceYears" type="number" min="0" class="btc-input" placeholder="Số năm kinh nghiệm" />
        <div class="flex gap-2">
          <button class="btc-btn-primary" @click="save">{{ editId ? 'Cập nhật' : 'Tạo job' }}</button>
          <button v-if="editId" class="btc-btn-secondary" @click="resetForm">Hủy</button>
        </div>
        <p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
      </div>

      <div class="space-y-3">
        <div v-for="job in jobs" :key="job.id" class="btc-card">
          <h3 class="font-semibold">{{ job.title }}</h3>
          <p class="text-sm text-slate-600">{{ job.description }}</p>
          <div class="mt-3 flex gap-2">
            <button class="btc-btn-secondary" @click="edit(job)">Sửa</button>
            <button class="btc-btn-secondary border-rose-200 text-rose-700" @click="removeJob(job.id)">Xóa</button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
