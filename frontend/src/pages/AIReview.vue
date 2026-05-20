<script setup>
import { ref, onMounted } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import PdfViewer from '../components/PdfViewer.vue'
import CircleScore from '../components/CircleScore.vue'
import api from '../services/api'

const cvUrl = ref('')
const jobs = ref([])
const selectedJobId = ref('')
const selectedJob = ref(null)
const review = ref(null)
const loading = ref(false)
const error = ref('')
const suggestions = ref([])

const loadMyCv = async () => {
  try {
    const { data } = await api.get('/cv/my')
    cvUrl.value = data.cvUrl || ''
  } catch (e) {
    console.error('Failed to load CV:', e)
  }
}

const loadJobs = async () => {
  try {
    const { data } = await api.get('/jobs?page=1&pageSize=100')
    jobs.value = data.data || []
  } catch (e) {
    console.error('Failed to load jobs:', e)
  }
}

const onJobSelected = async () => {
  const job = jobs.value.find(j => j._id === selectedJobId.value)
  selectedJob.value = job || null
}

const suggestJobs = async () => {
  if (!cvUrl.value) {
    error.value = 'Please upload your CV first'
    return
  }

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/suggest-jobs', { cvUrl: cvUrl.value })
    suggestions.value = data.suggestions || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Unable to suggest jobs'
  } finally {
    loading.value = false
  }
}

const reviewCv = async () => {
  if (!selectedJobId.value) {
    error.value = 'Please select a job'
    return
  }

  if (!cvUrl.value) {
    error.value = 'Please upload your CV first'
    return
  }

  try {
    loading.value = true
    error.value = ''
    const { data } = await api.post('/ai/review-cv', { jobId: selectedJobId.value, cvUrl: cvUrl.value })
    review.value = data
  } catch (e) {
    error.value = e?.response?.data?.message || 'Unable to review CV'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadMyCv()
  loadJobs()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">AI Review CV theo JD</h2>
    <p class="btc-page-subtitle">Upload CV để nhận gợi ý chỉnh sửa và điểm phù hợp với vị trí ứng tuyển.</p>

    <!-- Job Selection Panel -->
    <div class="btc-card max-w-2xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Choose Job Position</h3>
      <select v-model="selectedJobId" @change="onJobSelected" class="btc-input w-full mb-4">
        <option value="">Select a job to review against</option>
        <option v-for="job in jobs" :key="job._id" :value="job._id">
          {{ job.title }} - {{ job.companyId }}
        </option>
      </select>

      <!-- Selected Job Info -->
      <div v-if="selectedJob" class="bg-blue-50 border border-blue-200 rounded-lg p-4 mt-4">
        <h4 class="font-semibold text-blue-900 mb-2">{{ selectedJob.title }}</h4>
        <p class="text-sm text-blue-800 mb-3">{{ selectedJob.description }}</p>
        <div class="space-y-2 text-sm">
          <p><strong>Experience:</strong> {{ selectedJob.minExperienceYears }}+ years</p>
          <p v-if="selectedJob.mustHaveSkills?.length">
            <strong>Required Skills:</strong> {{ selectedJob.mustHaveSkills.join(', ') }}
          </p>
        </div>
      </div>
    </div>

    <!-- CV Preview -->
    <div class="btc-card max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-4">Your CV</h3>
      <div v-if="!cvUrl" class="bg-amber-50 border border-amber-200 text-amber-800 p-4 rounded-lg">
        <p>Please upload your CV first in <router-link to="/candidate/cv" class="font-semibold underline">CV Management</router-link></p>
      </div>
      <PdfViewer v-else :pdfUrl="cvUrl" />
    </div>

    <!-- Action Buttons -->
    <div class="btc-card max-w-2xl mb-6">
      <div class="flex gap-3 flex-wrap">
        <button
          class="btc-btn-secondary"
          :disabled="!cvUrl || loading"
          @click="suggestJobs"
        >
          {{ loading ? 'Analyzing...' : 'Get Job Suggestions' }}
        </button>
        <button
          class="btc-btn-primary"
          :disabled="!cvUrl || !selectedJobId || loading"
          @click="reviewCv"
        >
          {{ loading ? 'Reviewing...' : 'Review CV for This Job' }}
        </button>
      </div>
      <p v-if="error" class="text-sm text-rose-600 mt-3">{{ error }}</p>
    </div>

    <!-- Job Suggestions -->
    <div v-if="suggestions.length" class="btc-card max-w-4xl mb-6">
      <h3 class="font-semibold mb-4">AI Job Recommendations</h3>
      <div class="space-y-2">
        <div v-for="(s, idx) in suggestions" :key="idx" class="bg-green-50 border border-green-200 p-3 rounded-lg">
          <p class="text-sm text-green-900">{{ idx + 1 }}. {{ s.jobId }} - {{ s.reason }}</p>
        </div>
      </div>
    </div>

    <!-- Review Results -->
    <div v-if="review" class="mt-6 space-y-6">
      <!-- Score Card -->
      <div class="btc-card max-w-2xl">
        <h3 class="text-lg font-semibold mb-4">Review Score</h3>
        <div class="flex items-center gap-8">
          <CircleScore :score="review.score" />
          <div class="flex-1">
            <h4 class="font-semibold mb-3">Missing Keywords</h4>
            <div class="space-y-1">
              <p v-for="k in review.missingKeywords" :key="k" class="text-sm bg-red-50 text-red-700 px-3 py-1 rounded">
                • {{ k }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Suggestions -->
      <div class="btc-card max-w-4xl">
        <h3 class="font-semibold mb-4">AI Suggestions</h3>
        <div class="space-y-4">
          <div v-for="(s, idx) in review.tailoredSuggestions" :key="idx" class="border border-slate-200 rounded-lg overflow-hidden">
            <div class="bg-slate-100 px-4 py-2 font-medium">{{ s.section }}</div>
            <div class="grid md:grid-cols-2 gap-0">
              <div class="p-4 border-r border-slate-200">
                <p class="text-xs font-semibold text-slate-600 mb-2">Original</p>
                <p class="text-sm whitespace-pre-wrap bg-slate-50 p-3 rounded">{{ s.originalText }}</p>
              </div>
              <div class="p-4">
                <p class="text-xs font-semibold text-teal-700 mb-2">Suggested</p>
                <p class="text-sm whitespace-pre-wrap bg-teal-50 p-3 rounded">{{ s.suggestedText }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>

<style scoped>
.max-w-2xl {
  max-width: 42rem;
}

.max-w-4xl {
  max-width: 56rem;
}

.mb-6 {
  margin-bottom: 1.5rem;
}

.mb-4 {
  margin-bottom: 1rem;
}

.p-4 {
  padding: 1rem;
}

.px-3 {
  padding-left: 0.75rem;
  padding-right: 0.75rem;
}

.py-1 {
  padding-top: 0.25rem;
  padding-bottom: 0.25rem;
}

.gap-3 {
  gap: 0.75rem;
}

.gap-8 {
  gap: 2rem;
}

.flex {
  display: flex;
}

.flex-wrap {
  flex-wrap: wrap;
}

.items-center {
  align-items: center;
}

.flex-1 {
  flex: 1;
}

.space-y-1 > * + * {
  margin-top: 0.25rem;
}

.space-y-2 > * + * {
  margin-top: 0.5rem;
}

.space-y-4 > * + * {
  margin-top: 1rem;
}

.rounded {
  border-radius: 0.25rem;
}

.rounded-lg {
  border-radius: 0.5rem;
}

.w-full {
  width: 100%;
}
</style>
