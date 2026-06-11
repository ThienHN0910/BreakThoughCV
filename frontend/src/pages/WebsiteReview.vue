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

      <div class="btc-card space-y-4">
        <div>
          <p class="mb-2 text-sm font-semibold text-slate-700">Mức độ hài lòng</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="value in 5"
              :key="value"
              type="button"
              class="h-11 w-11 rounded-xl border text-xl font-bold transition"
              :class="value <= rating ? 'border-amber-300 bg-amber-100 text-amber-700' : 'border-slate-200 bg-white text-slate-300'"
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
            class="btc-input min-h-32"
            rows="5"
            placeholder="Bạn thích điều gì? Có điểm nào cần cải thiện?"
          ></textarea>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <button class="btc-btn-primary" type="button" :disabled="saving" @click="submitReview">
            {{ saving ? 'Đang gửi...' : 'Gửi đánh giá' }}
          </button>
          <p v-if="success" class="text-sm text-green-700">{{ success }}</p>
          <p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
        </div>
      </div>

      <div class="mt-6">
        <h3 class="mb-3 text-lg font-semibold">Đánh giá của bạn</h3>
        <div v-if="loading" class="btc-card text-sm text-slate-600">Đang tải...</div>
        <div v-else-if="!reviews.length" class="btc-card text-sm text-slate-600">Bạn chưa gửi đánh giá nào.</div>
        <div v-else class="space-y-3">
          <div v-for="item in reviews" :key="item.id" class="btc-card">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div class="text-amber-500">
                <span v-for="value in 5" :key="value">{{ value <= item.rating ? '★' : '☆' }}</span>
              </div>
              <span class="text-xs text-slate-500">{{ formatDate(item.createdAt) }}</span>
            </div>
            <p v-if="item.comment" class="mt-3 text-sm text-slate-700">{{ item.comment }}</p>
            <p v-else class="mt-3 text-sm text-slate-500">Không có nhận xét.</p>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
