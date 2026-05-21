<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import TagInput from '../components/TagInput.vue'
import api from '../services/api'

const jobs = ref([])
const companyId = ref('')
const categories = ref([])
const editId = ref('')
const error = ref('')
const keywordSuggestions = ref([])
const keywordLoading = ref(false)
const keywordError = ref('')
const activeSuggestionTarget = ref('mustHaveSkills')
let keywordTimer = null

const form = ref({
  title: '',
  categoryId: '',
  description: '',
  responsibilities: [],
  mustHaveSkills: [],
  niceToHaveSkills: [],
  minExperienceYears: 0
})

const selectedCategoryName = computed(() => {
  return categories.value.find((category) => category.id === form.value.categoryId)?.name || ''
})

const suggestionTargets = {
  responsibilities: 'Responsibilities',
  mustHaveSkills: 'Must-have skills',
  niceToHaveSkills: 'Nice-to-have skills'
}

const activeSuggestionLabel = computed(() => suggestionTargets[activeSuggestionTarget.value])

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
  keywordSuggestions.value = []
  keywordError.value = ''
  form.value = {
    title: '',
    categoryId: '',
    description: '',
    responsibilities: [],
    mustHaveSkills: [],
    niceToHaveSkills: [],
    minExperienceYears: 0
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
  scheduleKeywordSuggestions()
}

function addKeyword(target, keyword) {
  const normalized = keyword.trim()
  if (!normalized || form.value[target].some((item) => item.toLowerCase() === normalized.toLowerCase())) return
  form.value[target] = [...form.value[target], normalized]
}

function selectSuggestionTarget(target) {
  activeSuggestionTarget.value = target
  scheduleKeywordSuggestions()
}

async function suggestKeywords() {
  const title = form.value.title.trim()
  const description = form.value.description.trim()
  const categoryName = selectedCategoryName.value

  if (title.length < 3 && description.length < 12 && !categoryName) {
    keywordSuggestions.value = []
    keywordError.value = ''
    return
  }

  keywordLoading.value = true
  keywordError.value = ''

  try {
    const { data } = await api.post('/ai/suggest-job-keywords', {
      title,
      categoryName,
      description,
      targetField: activeSuggestionTarget.value
    })
    keywordSuggestions.value = data.keywords || []
  } catch (e) {
    keywordSuggestions.value = []
    keywordError.value = e?.response?.data?.message || 'AI chưa gợi ý được từ khóa'
  } finally {
    keywordLoading.value = false
  }
}

function scheduleKeywordSuggestions() {
  window.clearTimeout(keywordTimer)
  keywordTimer = window.setTimeout(suggestKeywords, 700)
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

watch(
  () => [form.value.title, form.value.categoryId, form.value.description, activeSuggestionTarget.value],
  scheduleKeywordSuggestions
)

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

        <div class="rounded-xl border border-sky-100 bg-sky-50/70 p-3">
          <div class="mb-2 flex items-center justify-between gap-3">
            <p class="text-sm font-semibold text-slate-800">AI gợi ý cho: {{ activeSuggestionLabel }}</p>
            <button class="text-xs font-semibold text-blue-700" type="button" @click="suggestKeywords">
              {{ keywordLoading ? 'Đang gợi ý...' : 'Gợi ý lại' }}
            </button>
          </div>
          <p v-if="keywordError" class="mb-2 text-xs text-rose-600">{{ keywordError }}</p>
          <div v-if="keywordSuggestions.length" class="flex flex-wrap gap-2">
            <span
              v-for="keyword in keywordSuggestions"
              :key="keyword"
              class="inline-flex items-center gap-1 rounded-lg border border-sky-200 bg-white px-2 py-1 text-xs font-semibold text-slate-700"
            >
              {{ keyword }}
              <button class="text-blue-700" type="button" title="Thêm vào ô đang chọn" @click="addKeyword(activeSuggestionTarget, keyword)">+</button>
            </span>
          </div>
          <p v-else class="text-xs text-slate-500">
            Click vào ô cần gợi ý rồi nhập tiêu đề, danh mục hoặc mô tả.
          </p>
        </div>

        <div
          :class="activeSuggestionTarget === 'responsibilities' ? 'rounded-xl ring-2 ring-blue-300' : ''"
          @click="selectSuggestionTarget('responsibilities')"
          @focusin="selectSuggestionTarget('responsibilities')"
        >
          <TagInput v-model="form.responsibilities" placeholder="Responsibilities" />
        </div>
        <div
          :class="activeSuggestionTarget === 'mustHaveSkills' ? 'rounded-xl ring-2 ring-blue-300' : ''"
          @click="selectSuggestionTarget('mustHaveSkills')"
          @focusin="selectSuggestionTarget('mustHaveSkills')"
        >
          <TagInput v-model="form.mustHaveSkills" placeholder="Must-have skills" />
        </div>
        <div
          :class="activeSuggestionTarget === 'niceToHaveSkills' ? 'rounded-xl ring-2 ring-blue-300' : ''"
          @click="selectSuggestionTarget('niceToHaveSkills')"
          @focusin="selectSuggestionTarget('niceToHaveSkills')"
        >
          <TagInput v-model="form.niceToHaveSkills" placeholder="Nice-to-have skills" />
        </div>
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
