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
      <defs>
        <linearGradient id="score-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stop-color="#0d9488" />
          <stop offset="100%" stop-color="#10b981" />
        </linearGradient>
      </defs>
      <circle cx="60" cy="60" r="54" stroke="#f1f5f9" stroke-width="10" fill="none" />
      <circle
        cx="60"
        cy="60"
        r="54"
        stroke="url(#score-gradient)"
        stroke-width="10"
        fill="none"
        :stroke-dasharray="circumference"
        :stroke-dashoffset="dashOffset"
        stroke-linecap="round"
        class="transition-all duration-1000 ease-out"
      />
    </svg>
    <div class="absolute inset-0 flex items-center justify-center text-4xl font-extrabold text-slate-800">{{ normalized }}</div>
  </div>
</template>
