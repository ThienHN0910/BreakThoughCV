<script setup>
import { onMounted, ref } from 'vue'
import AppLayout from '../../layouts/AppLayout.vue'
import api from '../../services/api'

const reviews = ref([])
const stats = ref({ totalReviews: 0, averageRating: 0 })
const loading = ref(false)
const error = ref('')

function formatDate(value) {
  try {
    return new Date(value).toLocaleString('vi-VN')
  } catch {
    return value
  }
}

function roleLabel(role) {
  if (role === 'candidate') return 'Ứng viên'
  if (role === 'recruiter') return 'Nhà tuyển dụng'
  if (role === 'admin') return 'Admin'
  return 'Người dùng'
}

async function loadReviews() {
  try {
    loading.value = true
    error.value = ''
    const [{ data: reviewsData }, { data: statsData }] = await Promise.all([
      api.get('/admin/website-reviews'),
      api.get('/website-reviews/stats')
    ])
    reviews.value = reviewsData || []
    stats.value = {
      totalReviews: statsData.totalReviews || 0,
      averageRating: statsData.averageRating || 0
    }
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được danh sách đánh giá'
    reviews.value = []
    stats.value = { totalReviews: 0, averageRating: 0 }
  } finally {
    loading.value = false
  }
}

onMounted(loadReviews)
</script>

<template>
  <AppLayout>
    <section class="btc-card">
      <div class="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 class="text-2xl font-bold">Đánh giá website</h2>
          <p class="mt-1 text-sm text-slate-500">Xem phản hồi của ứng viên và nhà tuyển dụng về trải nghiệm sử dụng hệ thống.</p>
        </div>
        <div class="flex flex-wrap gap-2 text-sm">
          <span class="rounded-xl bg-slate-100 px-3 py-2 font-semibold text-slate-700">Tổng đánh giá: {{ stats.totalReviews }}</span>
          <span class="rounded-xl bg-amber-100 px-3 py-2 font-semibold text-amber-700">Trung bình: {{ stats.averageRating }} / 5</span>
        </div>
      </div>

      <p v-if="error" class="mt-4 text-sm text-rose-600">{{ error }}</p>
      <p v-if="loading" class="mt-4 text-sm text-slate-500">Đang tải...</p>

      <div class="mt-5 overflow-x-auto rounded-xl border border-slate-200">
        <table class="min-w-full text-left text-sm">
          <thead class="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3">Người đánh giá</th>
              <th class="px-4 py-3">Vai trò</th>
              <th class="px-4 py-3">Điểm</th>
              <th class="px-4 py-3">Nhận xét</th>
              <th class="px-4 py-3">Ngày gửi</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in reviews" :key="item.id" class="border-t border-slate-100 align-top">
              <td class="px-4 py-3">
                <p class="font-semibold text-slate-800">{{ item.userName }}</p>
                <p class="text-xs text-slate-500">{{ item.userEmail }}</p>
              </td>
              <td class="px-4 py-3 text-slate-600">{{ roleLabel(item.userRole) }}</td>
              <td class="px-4 py-3">
                <div class="whitespace-nowrap text-amber-500">
                  <span v-for="value in 5" :key="value">{{ value <= item.rating ? '★' : '☆' }}</span>
                </div>
                <p class="mt-1 text-xs text-slate-500">{{ item.rating }} / 5</p>
              </td>
              <td class="max-w-xl px-4 py-3 text-slate-700">
                <p v-if="item.comment">{{ item.comment }}</p>
                <p v-else class="text-slate-400">Không có nhận xét.</p>
              </td>
              <td class="px-4 py-3 text-slate-600">{{ formatDate(item.createdAt) }}</td>
            </tr>
            <tr v-if="!loading && reviews.length === 0">
              <td colspan="5" class="px-4 py-8 text-center text-slate-500">Chưa có đánh giá nào.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </AppLayout>
</template>
