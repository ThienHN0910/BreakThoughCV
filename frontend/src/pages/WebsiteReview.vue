<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'

const rating = ref(5)
const comment = ref('')
const reviews = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

function formatDate(value) {
  try {
    return new Date(value).toLocaleString('vi-VN')
  } catch {
    return value
  }
}

async function loadReviews() {
  try {
    loading.value = true
    error.value = ''
    const { data } = await api.get('/website-reviews/my')
    reviews.value = data || []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được đánh giá'
  } finally {
    loading.value = false
  }
}

async function submitReview() {
  try {
    saving.value = true
    error.value = ''
    success.value = ''
    await api.post('/website-reviews', {
      rating: rating.value,
      comment: comment.value
    })
    comment.value = ''
    rating.value = 5
    success.value = 'Cảm ơn bạn đã đánh giá website.'
    await loadReviews()
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không gửi được đánh giá'
  } finally {
    saving.value = false
  }
}

onMounted(loadReviews)
</script>

<template>
  <AppLayout>
    <div class="max-w-4xl">
      <h2 class="btc-page-title">Đánh giá website</h2>
      <p class="btc-page-subtitle">Chia sẻ trải nghiệm của bạn để BreakThroughCV cải thiện tốt hơn.</p>

      <div class="btc-card space-y-5">
        <div>
          <p class="mb-3 text-sm font-semibold text-slate-700">Mức độ hài lòng</p>
          <div class="flex flex-wrap gap-2.5">
            <button
              v-for="value in 5"
              :key="value"
              type="button"
              class="h-12 w-12 rounded-xl border-2 text-2xl font-bold transition-all hover:-translate-y-0.5"
              :class="value <= rating ? 'border-amber-400 bg-amber-50 text-amber-500 shadow-sm' : 'border-slate-100 bg-slate-50 text-slate-300 hover:border-slate-200'"
              @click="rating = value"
            >
              ★
            </button>
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-semibold text-slate-700" for="website-review-comment">Nhận xét</label>
          <textarea
            id="website-review-comment"
            v-model="comment"
            class="btc-input min-h-32 text-base"
            rows="5"
            placeholder="Bạn thích điều gì? Có điểm nào cần cải thiện?"
          ></textarea>
        </div>

        <div class="flex flex-wrap items-center gap-4 pt-2 border-t border-slate-100">
          <button class="btc-btn-primary px-6" type="button" :disabled="saving" @click="submitReview">
            {{ saving ? 'Đang gửi...' : 'Gửi đánh giá' }}
          </button>
          <div v-if="success" class="inline-flex items-center gap-1.5 text-sm font-medium text-emerald-600 px-3 py-1.5 bg-emerald-50 border border-emerald-100 rounded-lg">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            {{ success }}
          </div>
          <p v-if="error" class="text-sm font-medium text-rose-600">{{ error }}</p>
        </div>
      </div>

      <div class="mt-8">
        <h3 class="mb-4 text-xl font-bold text-slate-800">Lịch sử đánh giá của bạn</h3>
        <div v-if="loading" class="btc-card text-sm text-slate-500 font-medium flex items-center gap-2">
          <svg class="animate-spin h-4 w-4 text-indigo-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
          Đang tải...
        </div>
        <div v-else-if="!reviews.length" class="rounded-2xl border border-slate-200 bg-slate-50 p-8 text-center text-slate-500 font-medium">
          Bạn chưa gửi đánh giá nào.
        </div>
        <div v-else class="grid gap-4 sm:grid-cols-2">
          <div v-for="item in reviews" :key="item.id" class="btc-card hover:border-indigo-200 transition-colors">
            <div class="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3 mb-3">
              <div class="text-amber-400 text-lg tracking-widest">
                <span v-for="value in 5" :key="value">{{ value <= item.rating ? '★' : '☆' }}</span>
              </div>
              <span class="text-xs font-medium text-slate-400 bg-slate-50 px-2 py-1 rounded-md">{{ formatDate(item.createdAt) }}</span>
            </div>
            <p v-if="item.comment" class="text-sm text-slate-700 leading-relaxed">{{ item.comment }}</p>
            <p v-else class="text-sm italic text-slate-400">Không có nhận xét.</p>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
