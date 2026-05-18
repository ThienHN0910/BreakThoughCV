<script setup>
import { ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import CircleScore from '../components/CircleScore.vue'
import api from '../services/api'

const cvText = ref('')
const jobs = ref([])
const selectedJobId = ref('')
const review = ref(null)
const loading = ref(false)
const error = ref('')
const suggestions = ref([])

async function loadJobs() {
  const { data } = await api.get('/jobs')
  jobs.value = data.data || []
}

async function suggestJobs() {
  try {
    const { data } = await api.post('/ai/suggest-jobs', { cvText: cvText.value })
    suggestions.value = data.suggestions || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể gợi ý công việc'
  }
}

async function reviewCv() {
  try {
    loading.value = true
    const { data } = await api.post('/ai/review-cv', { jobId: selectedJobId.value, cvText: cvText.value })
    review.value = data
    error.value = ''
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không thể review CV'
  } finally {
    loading.value = false
  }
}

loadJobs()
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">AI Review CV theo JD</h2>
    <p class="btc-page-subtitle">Dán nội dung CV để nhận gợi ý chỉnh sửa và điểm phù hợp với vị trí ứng tuyển.</p>

    <div class="btc-card space-y-3">
      <select v-model="selectedJobId" class="btc-input">
        <option value="">Chọn job để review</option>
        <option v-for="job in jobs" :key="job.id" :value="job.id">{{ job.title }}</option>
      </select>
      <textarea v-model="cvText" class="btc-input" rows="8" placeholder="Dán nội dung CV tại đây"></textarea>
      <div class="flex gap-2">
        <button class="btc-btn-secondary" :disabled="!cvText" @click="suggestJobs">Gợi ý top 3 job</button>
        <button class="btc-btn-primary" :disabled="!cvText || !selectedJobId || loading" @click="reviewCv">
          Review CV
        </button>
      </div>
      <p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
    </div>

    <div v-if="suggestions.length" class="btc-card mt-4">
      <h3 class="font-semibold mb-2">AI Job Suggestions</h3>
      <ul class="list-disc pl-5 text-sm space-y-1">
        <li v-for="s in suggestions" :key="`${s.jobId}-${s.reason}`">{{ s.jobId }} - {{ s.reason }}</li>
      </ul>
    </div>

    <div v-if="review" class="mt-4 grid lg:grid-cols-[220px_1fr] gap-4">
      <div class="btc-card flex flex-col items-center gap-4">
        <CircleScore :score="review.score" />
        <div>
          <h3 class="font-semibold mb-2">Missing Keywords</h3>
          <ul class="list-disc pl-5 text-sm">
            <li v-for="k in review.missingKeywords" :key="k">{{ k }}</li>
          </ul>
        </div>
      </div>

      <div class="btc-card">
        <h3 class="font-semibold mb-3">Side-by-Side AI Suggestion</h3>
        <div class="space-y-4">
          <div v-for="(s, idx) in review.tailoredSuggestions" :key="idx" class="rounded-xl border border-slate-200 p-3">
            <p class="font-medium mb-2">{{ s.section }}</p>
            <div class="grid md:grid-cols-2 gap-3 text-sm">
              <div>
                <p class="font-medium text-slate-500 mb-1">Original</p>
                <p class="bg-slate-100 rounded p-2 whitespace-pre-wrap">{{ s.originalText }}</p>
              </div>
              <div>
                <p class="font-medium text-teal-700 mb-1">Suggested</p>
                <p class="bg-teal-50 rounded p-2 whitespace-pre-wrap">{{ s.suggestedText }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
