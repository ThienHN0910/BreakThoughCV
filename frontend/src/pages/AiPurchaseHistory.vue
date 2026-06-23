<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useNotificationsStore } from '../stores/notifications'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const notifications = useNotificationsStore()

const loading = ref(false)
const error = ref('')
const items = ref([])

const plansLoading = ref(false)
const paymentLoading = ref(false)
const paymentVerifying = ref(false)
const paymentError = ref('')
const paymentNotice = ref('')

const aiPlans = ref([])
const selectedAiPlan = ref('MONTH')

const aiAccessEnabled = computed(() => Boolean(auth.user?.aiAccessEnabled))
const aiAccessExpiresAt = computed(() => auth.user?.aiAccessExpiresAt)

function planLabel(plan) {
  const p = String(plan || '').toUpperCase()
  if (p === 'WEEK') return '1 tuần'
  if (p === 'YEAR') return '1 năm'
  return '1 tháng'
}

async function loadAiPlans() {
  try {
    plansLoading.value = true
    const { data } = await api.get('/payments/payos/ai-access/plans')
    aiPlans.value = Array.isArray(data) ? data : []
    if (!aiPlans.value.some(p => p.key === selectedAiPlan.value)) {
      selectedAiPlan.value = aiPlans.value[0]?.key || 'MONTH'
    }
  } catch {
    aiPlans.value = []
  } finally {
    plansLoading.value = false
  }
}

async function startAiPayment() {
  if (paymentLoading.value) return

  paymentError.value = ''
  paymentNotice.value = ''

  try {
    paymentLoading.value = true
    const { data } = await api.post('/payments/payos/ai-access/create', { plan: selectedAiPlan.value })

    if (data?.alreadyPaid || data?.aiAccessEnabled) {
      await auth.refreshMe()
      paymentNotice.value = 'Tài khoản đã có quyền sử dụng AI.'

      notifications.add({
        type: 'info',
        title: 'Gói AI đang hoạt động',
        message: 'Tài khoản của bạn đã có quyền sử dụng AI Review.',
        href: '/candidate/ai-review'
      })
      return
    }

    if (!data?.checkoutUrl) {
      paymentError.value = 'Không tạo được link thanh toán.'
      return
    }

    window.location.href = data.checkoutUrl
  } catch (e) {
    const data = e?.response?.data
    const baseMsg = data?.message || 'Không tạo được link thanh toán'
    const missing = Array.isArray(data?.missing) ? data.missing : []
    const hint = data?.hint

    paymentError.value = [
      baseMsg,
      missing.length ? `Thiếu cấu hình: ${missing.join(', ')}` : '',
      hint ? `Gợi ý: ${hint}` : ''
    ].filter(Boolean).join(' | ')
  } finally {
    paymentLoading.value = false
  }
}

async function verifyAiPaymentFromReturnUrl() {
  const orderCode = route.query.orderCode
  if (!orderCode) return

  const cancel = String(route.query.cancel || '').toLowerCase() === 'true'
  const status = String(route.query.status || '').toUpperCase()

  try {
    await router.replace({ path: route.path, query: {} })
  } catch {
  }

  if (cancel || status === 'CANCELLED') {
    paymentNotice.value = 'Bạn đã hủy thanh toán.'
    return
  }

  if (status !== 'PAID') {
    paymentNotice.value = 'Thanh toán đang được xử lý. Nếu đã thanh toán, hãy thử tải lại trang.'
    return
  }

  try {
    paymentVerifying.value = true
    paymentError.value = ''
    const { data } = await api.get('/payments/payos/ai-access/verify', { params: { orderCode } })
    await auth.refreshMe()
    await loadHistory()
    if (data?.aiAccessEnabled) {
      paymentNotice.value = 'Thanh toán thành công. Bạn có thể sử dụng AI ngay.'

      notifications.add({
        type: 'success',
        title: 'Mua gói AI thành công',
        message: 'Bạn có thể sử dụng AI Review ngay bây giờ.',
        href: '/candidate/ai-review'
      })
    } else {
      paymentNotice.value = 'Chưa xác nhận được thanh toán. Hãy thử lại sau.'
    }
  } catch (e) {
    paymentError.value = e?.response?.data?.message || 'Không xác minh được thanh toán'
  } finally {
    paymentVerifying.value = false
  }
}

function formatDate(value) {
  if (!value) return ''
  try {
    return new Date(value).toLocaleString()
  } catch {
    return String(value)
  }
}

async function loadHistory() {
  try {
    loading.value = true
    error.value = ''
    const { data } = await api.get('/payments/payos/ai-access/history')
    items.value = Array.isArray(data) ? data : []
  } catch (e) {
    error.value = e?.response?.data?.message || 'Không tải được lịch sử gói AI'
    items.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  auth.refreshMe().catch(() => {})
  loadAiPlans()
  loadHistory()
  verifyAiPaymentFromReturnUrl()
})
</script>

<template>
  <AppLayout>
    <h2 class="btc-page-title">Gói AI đã mua</h2>
    <p class="btc-page-subtitle">Lịch sử thanh toán để sử dụng AI Review.</p>

    <div class="btc-card max-w-4xl mb-6 bg-gradient-to-br from-indigo-50 to-white border-indigo-100">
      <h3 class="text-lg font-bold text-slate-800 mb-2 flex items-center gap-2">
        <svg class="w-5 h-5 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>
        Trạng thái AI
      </h3>
      <div v-if="aiAccessEnabled" class="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-emerald-100 border border-emerald-200 text-sm font-semibold text-emerald-800 mb-4">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
        Đang kích hoạt
        <span class="text-emerald-600 ml-1 font-medium text-xs">
          (<span v-if="aiAccessExpiresAt">Hạn dùng: {{ formatDate(aiAccessExpiresAt) }}</span>
          <span v-else>Không giới hạn</span>)
        </span>
      </div>
      <p v-else class="text-sm font-medium text-slate-600 mb-4">Bạn chưa có quyền AI. Hãy mua gói để sử dụng tính năng AI Review mạnh mẽ.</p>

      <div class="mt-4 flex flex-wrap gap-3">
        <button
          v-if="aiAccessEnabled"
          class="btc-btn-primary"
          type="button"
          @click="router.push('/candidate/ai-review')"
        >
          Sử dụng AI Review
        </button>
      </div>
    </div>

    <div v-if="!aiAccessEnabled" class="btc-card max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-2">Mua gói AI</h3>
      <p class="text-sm text-slate-600">Chọn gói cước và thanh toán qua PayOS.</p>

      <div class="mt-4 space-y-2">
        <label
          v-for="p in aiPlans"
          :key="p.key"
          class="flex items-center justify-between gap-3 rounded-xl border-2 px-4 py-3 cursor-pointer transition-all"
          :class="selectedAiPlan === p.key ? 'border-indigo-500 bg-indigo-50/50 shadow-sm' : 'border-slate-100 bg-white hover:border-slate-200'"
        >
          <div class="flex items-center gap-3">
            <input
              type="radio"
              name="aiPlan"
              class="w-4 h-4 text-indigo-600 border-slate-300 focus:ring-indigo-500 focus:ring-2 accent-indigo-600"
              :value="p.key"
              v-model="selectedAiPlan"
              :disabled="plansLoading || paymentLoading || paymentVerifying"
            />
            <span class="text-base font-bold text-slate-800">{{ p.label }}</span>
          </div>
          <span class="text-lg font-bold text-indigo-600">{{ (p.amount || 0).toLocaleString() }} <span class="text-xs text-slate-500 font-semibold uppercase">VND</span></span>
        </label>

        <p v-if="!aiPlans.length" class="text-sm text-slate-600">Không tải được danh sách gói. Hãy thử tải lại trang.</p>
      </div>

      <div class="mt-4 flex flex-wrap gap-3">
        <button
          class="btc-btn-primary"
          :disabled="!aiPlans.length || paymentLoading || paymentVerifying"
          @click="startAiPayment"
        >
          {{ paymentLoading ? 'Đang tạo thanh toán...' : 'Thanh toán qua PayOS' }}
        </button>
      </div>

      <p v-if="paymentVerifying" class="text-sm mt-3">Đang xác minh thanh toán...</p>
      <p v-if="paymentNotice" class="text-sm text-teal-700 mt-3">{{ paymentNotice }}</p>
      <p v-if="paymentError" class="text-sm text-rose-600 mt-3">{{ paymentError }}</p>
    </div>

    <div class="btc-card max-w-4xl">
      <div class="flex items-center justify-between border-b border-slate-100 pb-4 mb-4">
        <h3 class="text-lg font-bold text-slate-800">Lịch sử giao dịch</h3>
      </div>
      <p v-if="error" class="text-sm font-medium text-rose-600 mb-3">{{ error }}</p>
      <div v-if="loading" class="flex items-center gap-2 text-sm text-slate-500 font-medium">
        <svg class="animate-spin h-4 w-4 text-indigo-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
        Đang tải...
      </div>

      <div v-else>
        <div v-if="!items.length" class="rounded-2xl border border-slate-200 bg-slate-50 p-8 text-center text-slate-500 font-medium">
          Chưa có giao dịch nào.
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-slate-200">
          <table class="min-w-full text-sm text-left whitespace-nowrap">
            <thead class="bg-slate-50 text-slate-500 font-semibold uppercase text-xs">
              <tr>
                <th class="px-4 py-3 border-b border-slate-200">Mã đơn</th>
                <th class="px-4 py-3 border-b border-slate-200">Gói</th>
                <th class="px-4 py-3 border-b border-slate-200">Số tiền</th>
                <th class="px-4 py-3 border-b border-slate-200">Trạng thái</th>
                <th class="px-4 py-3 border-b border-slate-200">Ngày tạo</th>
                <th class="px-4 py-3 border-b border-slate-200">Ngày thanh toán</th>
                <th class="px-4 py-3 border-b border-slate-200">Hạn dùng</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr v-for="it in items" :key="it.orderCode" class="hover:bg-slate-50/80 transition-colors">
                <td class="px-4 py-3 font-semibold text-slate-800">#{{ it.orderCode }}</td>
                <td class="px-4 py-3 text-slate-600 font-medium">{{ planLabel(it.plan) }}</td>
                <td class="px-4 py-3 text-slate-800 font-bold">{{ it.amount?.toLocaleString?.() || it.amount }} <span class="text-[10px] text-slate-500">VND</span></td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold border"
                    :class="it.status === 'PAID'
                      ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                      : it.status === 'CANCELLED'
                        ? 'bg-rose-50 text-rose-700 border-rose-200'
                        : 'bg-slate-50 text-slate-700 border-slate-200'"
                  >
                    {{ it.status }}
                  </span>
                </td>
                <td class="px-4 py-3 text-slate-500 text-xs">{{ formatDate(it.createdAt) }}</td>
                <td class="px-4 py-3 text-slate-500 text-xs">{{ formatDate(it.paidAt) || '-' }}</td>
                <td class="px-4 py-3 text-slate-500 text-xs font-medium">{{ formatDate(it.accessToAt) || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
