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
    <div class="grid lg:grid-cols-2 gap-8">
      <div class="btc-card space-y-5 h-fit">
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Tiêu đề Job</label>
          <input v-model="form.title" class="btc-input" placeholder="Ví dụ: Frontend Developer..." />
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Danh mục</label>
          <select v-model="form.categoryId" class="btc-input">
            <option value="">Chọn danh mục</option>
            <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Mô tả công việc</label>
          <textarea v-model="form.description" class="btc-input" rows="3" placeholder="Mô tả ngắn gọn..."></textarea>
        </div>

        <div class="rounded-xl border border-indigo-100 bg-indigo-50/50 p-4">
          <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
            <p class="text-sm font-semibold text-indigo-900 inline-flex items-center gap-1.5"><svg class="w-4 h-4 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg> AI gợi ý cho: {{ activeSuggestionLabel }}</p>
            <button class="text-xs font-semibold text-indigo-600 hover:text-indigo-800 transition-colors" type="button" @click="suggestKeywords">
              {{ keywordLoading ? 'Đang gợi ý...' : 'Gợi ý lại' }}
            </button>
          </div>
          <p v-if="keywordError" class="mb-2 text-xs text-rose-600">{{ keywordError }}</p>
          <div v-if="keywordSuggestions.length" class="flex flex-wrap gap-2">
            <span
              v-for="keyword in keywordSuggestions"
              :key="keyword"
              class="inline-flex items-center gap-1 rounded-lg border border-indigo-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-sm transition-all hover:border-indigo-300"
            >
              {{ keyword }}
              <button class="text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded p-0.5" type="button" title="Thêm vào ô đang chọn" @click="addKeyword(activeSuggestionTarget, keyword)"><svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg></button>
            </span>
          </div>
          <p v-else class="text-xs text-slate-500 flex items-center gap-1.5">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Click vào ô kỹ năng bên dưới rồi nhập tiêu đề để nhận gợi ý.
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Responsibilities</label>
          <div
            :class="activeSuggestionTarget === 'responsibilities' ? 'rounded-xl ring-2 ring-indigo-500/20 border-indigo-500' : ''"
            class="transition-all"
            @click="selectSuggestionTarget('responsibilities')"
            @focusin="selectSuggestionTarget('responsibilities')"
          >
            <TagInput v-model="form.responsibilities" placeholder="Nhập và nhấn Enter..." />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Must-have skills</label>
          <div
            :class="activeSuggestionTarget === 'mustHaveSkills' ? 'rounded-xl ring-2 ring-indigo-500/20 border-indigo-500' : ''"
            class="transition-all"
            @click="selectSuggestionTarget('mustHaveSkills')"
            @focusin="selectSuggestionTarget('mustHaveSkills')"
          >
            <TagInput v-model="form.mustHaveSkills" placeholder="Nhập và nhấn Enter..." />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Nice-to-have skills</label>
          <div
            :class="activeSuggestionTarget === 'niceToHaveSkills' ? 'rounded-xl ring-2 ring-indigo-500/20 border-indigo-500' : ''"
            class="transition-all"
            @click="selectSuggestionTarget('niceToHaveSkills')"
            @focusin="selectSuggestionTarget('niceToHaveSkills')"
          >
            <TagInput v-model="form.niceToHaveSkills" placeholder="Nhập và nhấn Enter..." />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1.5">Số năm kinh nghiệm (tối thiểu)</label>
          <input v-model.number="form.minExperienceYears" type="number" min="0" class="btc-input" placeholder="Ví dụ: 2" />
        </div>
        <div class="flex gap-3 pt-2">
          <button class="btc-btn-primary px-6" @click="save">{{ editId ? 'Cập nhật' : 'Tạo job mới' }}</button>
          <button v-if="editId" class="btc-btn-secondary" @click="resetForm">Hủy chỉnh sửa</button>
        </div>
        <p v-if="error" class="text-sm text-rose-600 font-medium">{{ error }}</p>
      </div>

      <div class="space-y-4">
        <h3 class="font-bold text-slate-800 text-lg mb-2">Danh sách Job đã đăng</h3>
        <div v-if="jobs.length === 0" class="rounded-xl border border-slate-200 bg-slate-50 p-8 text-center text-slate-500">
          Chưa có job nào.
        </div>
        <div v-for="job in jobs" :key="job.id" class="btc-card hover:border-indigo-200 transition-colors">
          <div class="flex flex-wrap gap-2 justify-between items-start mb-2">
            <h3 class="font-bold text-slate-800 text-lg">{{ job.title }}</h3>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700 border border-indigo-100">{{ job.minExperienceYears }} năm KN</span>
          </div>
          <p class="text-sm text-slate-600 line-clamp-2">{{ job.description }}</p>
          <div class="mt-4 flex flex-wrap gap-3 pt-4 border-t border-slate-100 justify-end">
            <button class="btc-btn-secondary !py-1.5 !px-4 text-sm" @click="edit(job)">Sửa</button>
            <button class="btc-btn-secondary !py-1.5 !px-4 text-sm !text-rose-600 !border-rose-200 hover:!bg-rose-50" @click="removeJob(job.id)">Xóa</button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
