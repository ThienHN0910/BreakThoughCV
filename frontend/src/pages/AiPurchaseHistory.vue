<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppLayout from '../layouts/AppLayout.vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

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

    <div class="btc-card max-w-4xl mb-6">
      <h3 class="text-lg font-semibold mb-2">Trạng thái AI</h3>
      <p v-if="aiAccessEnabled" class="text-sm text-teal-700">
        Bạn đang có quyền sử dụng AI.
        <span v-if="aiAccessExpiresAt">Hạn dùng đến: {{ formatDate(aiAccessExpiresAt) }}</span>
        <span v-else>Hạn dùng: không giới hạn</span>
      </p>
      <p v-else class="text-sm text-slate-600">Bạn chưa có quyền AI. Hãy mua gói để sử dụng AI Review.</p>

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
          class="flex items-center justify-between gap-3 rounded-lg border border-slate-200 bg-white px-3 py-2"
        >
          <div class="flex items-center gap-3">
            <input
              type="radio"
              name="aiPlan"
              class="accent-slate-900"
              :value="p.key"
              v-model="selectedAiPlan"
              :disabled="plansLoading || paymentLoading || paymentVerifying"
            />
            <span class="text-sm font-semibold text-slate-800">{{ p.label }}</span>
          </div>
          <span class="text-sm text-slate-600">{{ (p.amount || 0).toLocaleString() }} VND</span>
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
      <p v-if="error" class="text-sm text-rose-600 mb-3">{{ error }}</p>
      <p v-if="loading" class="text-sm">Đang tải...</p>

      <div v-else>
        <p v-if="!items.length" class="text-sm text-slate-600">Chưa có giao dịch nào.</p>

        <div v-else class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="text-left text-slate-500 border-b border-slate-200">
                <th class="py-2 pr-4">Mã đơn</th>
                <th class="py-2 pr-4">Gói</th>
                <th class="py-2 pr-4">Số tiền</th>
                <th class="py-2 pr-4">Trạng thái</th>
                <th class="py-2 pr-4">Tạo lúc</th>
                <th class="py-2 pr-4">Thanh toán lúc</th>
                <th class="py-2 pr-4">Hạn dùng</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="it in items" :key="it.orderCode" class="border-b border-slate-100">
                <td class="py-2 pr-4 font-medium">{{ it.orderCode }}</td>
                <td class="py-2 pr-4">{{ planLabel(it.plan) }}</td>
                <td class="py-2 pr-4">{{ it.amount?.toLocaleString?.() || it.amount }} VND</td>
                <td class="py-2 pr-4">
                  <span
                    class="inline-flex rounded-full px-2 py-0.5 text-xs font-semibold"
                    :class="it.status === 'PAID'
                      ? 'bg-teal-50 text-teal-700'
                      : it.status === 'CANCELLED'
                        ? 'bg-rose-50 text-rose-700'
                        : 'bg-slate-100 text-slate-700'"
                  >
                    {{ it.status }}
                  </span>
                </td>
                <td class="py-2 pr-4">{{ formatDate(it.createdAt) }}</td>
                <td class="py-2 pr-4">{{ formatDate(it.paidAt) }}</td>
                <td class="py-2 pr-4">{{ formatDate(it.accessToAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
