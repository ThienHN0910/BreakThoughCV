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
    <h2 class="text-2xl font-bold mb-4">AI Review CV theo JD</h2>

    <div class="bg-white border rounded-lg p-4 space-y-3">
      <select v-model="selectedJobId" class="border rounded p-2 w-full">
        <option value="">Chọn job để review</option>
        <option v-for="job in jobs" :key="job.id" :value="job.id">{{ job.title }}</option>
      </select>
      <textarea v-model="cvText" class="border rounded p-2 w-full" rows="8" placeholder="Dán nội dung CV tại đây"></textarea>
      <div class="flex gap-2">
        <button class="bg-teal-700 text-white rounded p-2" :disabled="!cvText" @click="suggestJobs">Gợi ý top 3 job</button>
        <button class="bg-slate-900 text-white rounded p-2" :disabled="!cvText || !selectedJobId || loading" @click="reviewCv">
          Review CV
        </button>
      </div>
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    </div>

    <div v-if="suggestions.length" class="mt-4 bg-white border rounded-lg p-4">
      <h3 class="font-semibold mb-2">AI Job Suggestions</h3>
      <ul class="list-disc pl-5 text-sm space-y-1">
        <li v-for="s in suggestions" :key="`${s.jobId}-${s.reason}`">{{ s.jobId }} - {{ s.reason }}</li>
      </ul>
    </div>

    <div v-if="review" class="mt-4 grid lg:grid-cols-[220px_1fr] gap-4">
      <div class="bg-white border rounded-lg p-4 flex flex-col items-center gap-4">
        <CircleScore :score="review.score" />
        <div>
          <h3 class="font-semibold mb-2">Missing Keywords</h3>
          <ul class="list-disc pl-5 text-sm">
            <li v-for="k in review.missingKeywords" :key="k">{{ k }}</li>
          </ul>
        </div>
      </div>

      <div class="bg-white border rounded-lg p-4">
        <h3 class="font-semibold mb-3">Side-by-Side AI Suggestion</h3>
        <div class="space-y-4">
          <div v-for="(s, idx) in review.tailoredSuggestions" :key="idx" class="border rounded p-3">
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
