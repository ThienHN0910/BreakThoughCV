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
          <h2 class="text-2xl font-bold text-slate-800">Đánh giá website</h2>
          <p class="mt-1 text-sm text-slate-500">Xem phản hồi của ứng viên và nhà tuyển dụng về trải nghiệm sử dụng hệ thống.</p>
        </div>
        <div class="flex flex-wrap gap-4 text-sm">
          <div class="flex flex-col items-center justify-center rounded-xl bg-slate-50 border border-slate-200 px-5 py-2">
            <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Tổng đánh giá</span>
            <span class="text-xl font-extrabold text-slate-800">{{ stats.totalReviews }}</span>
          </div>
          <div class="flex flex-col items-center justify-center rounded-xl bg-amber-50 border border-amber-200 px-5 py-2">
            <span class="text-xs font-bold uppercase tracking-wider text-amber-600">Trung bình sao</span>
            <span class="text-xl font-extrabold text-amber-700">{{ stats.averageRating }} <span class="text-xs text-amber-500 font-bold">/ 5</span></span>
          </div>
        </div>
      </div>

      <p v-if="error" class="mt-4 text-sm text-rose-600">{{ error }}</p>
      <p v-if="loading" class="mt-4 text-sm text-slate-500">Đang tải...</p>

      <div class="mt-8 overflow-x-auto rounded-xl border border-slate-200">
        <table class="min-w-full text-left text-sm whitespace-nowrap">
          <thead class="bg-slate-50 text-xs font-bold uppercase tracking-wider text-slate-500 border-b border-slate-200">
            <tr>
              <th class="px-5 py-3">Người đánh giá</th>
              <th class="px-5 py-3">Vai trò</th>
              <th class="px-5 py-3 text-center">Điểm</th>
              <th class="px-5 py-3 w-1/2">Nhận xét</th>
              <th class="px-5 py-3 text-right">Ngày gửi</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 bg-white">
            <tr v-for="item in reviews" :key="item.id" class="hover:bg-slate-50/80 transition-colors align-top">
              <td class="px-5 py-4">
                <p class="font-bold text-slate-800">{{ item.userName }}</p>
                <p class="text-xs text-slate-500 font-medium">{{ item.userEmail }}</p>
              </td>
              <td class="px-5 py-4">
                <span class="inline-flex items-center rounded-md bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700 border border-slate-200">
                  {{ roleLabel(item.userRole) }}
                </span>
              </td>
              <td class="px-5 py-4 text-center">
                <div class="whitespace-nowrap text-amber-400 text-base tracking-widest">
                  <span v-for="value in 5" :key="value">{{ value <= item.rating ? '★' : '☆' }}</span>
                </div>
                <p class="mt-1 text-[11px] font-bold text-amber-600 bg-amber-50 inline-block px-1.5 py-0.5 rounded">{{ item.rating }} / 5</p>
              </td>
              <td class="px-5 py-4 whitespace-normal min-w-[300px]">
                <p v-if="item.comment" class="text-sm text-slate-700 leading-relaxed">{{ item.comment }}</p>
                <p v-else class="text-sm italic text-slate-400">Không có nhận xét.</p>
              </td>
              <td class="px-5 py-4 text-right text-xs font-medium text-slate-500">{{ formatDate(item.createdAt) }}</td>
            </tr>
            <tr v-if="!loading && reviews.length === 0">
              <td colspan="5" class="px-5 py-8 text-center text-slate-500 font-medium">Chưa có đánh giá nào.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </AppLayout>
</template>
