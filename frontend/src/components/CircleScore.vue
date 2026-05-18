<script setup>
import { computed } from 'vue'

const props = defineProps({
  score: { type: Number, default: 0 }
})

const normalized = computed(() => Math.min(100, Math.max(0, props.score)))
const circumference = 2 * Math.PI * 54
const dashOffset = computed(() => circumference - (normalized.value / 100) * circumference)
</script>

<template>
  <div class="relative w-36 h-36">
    <svg class="w-full h-full -rotate-90" viewBox="0 0 120 120">
      <circle cx="60" cy="60" r="54" stroke="#e2e8f0" stroke-width="10" fill="none" />
      <circle
        cx="60"
        cy="60"
        r="54"
        stroke="#0f766e"
        stroke-width="10"
        fill="none"
        :stroke-dasharray="circumference"
        :stroke-dashoffset="dashOffset"
        stroke-linecap="round"
      />
    </svg>
    <div class="absolute inset-0 flex items-center justify-center text-2xl font-bold text-teal-700">{{ normalized }}</div>
  </div>
</template>
