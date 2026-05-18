<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  modelValue: { type: Array, default: () => [] },
  placeholder: { type: String, default: 'Nhập tag và nhấn Enter' }
})

const emit = defineEmits(['update:modelValue'])
const input = ref('')
const tags = ref([...props.modelValue])

watch(
  () => props.modelValue,
  (next) => {
    tags.value = [...next]
  }
)

function addTag() {
  const value = input.value.trim()
  if (!value || tags.value.includes(value)) return
  tags.value.push(value)
  emit('update:modelValue', tags.value)
  input.value = ''
}

function removeTag(tag) {
  tags.value = tags.value.filter((x) => x !== tag)
  emit('update:modelValue', tags.value)
}
</script>

<template>
  <div class="rounded border p-2 bg-white">
    <div class="flex flex-wrap gap-2 mb-2">
      <span v-for="tag in tags" :key="tag" class="bg-slate-200 px-2 py-1 rounded text-sm">
        {{ tag }}
        <button class="ml-1" @click="removeTag(tag)">×</button>
      </span>
    </div>
    <input
      v-model="input"
      :placeholder="placeholder"
      class="w-full outline-none"
      @keydown.enter.prevent="addTag"
    />
  </div>
</template>
