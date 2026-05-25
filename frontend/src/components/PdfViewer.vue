<template>
  <div class="pdf-viewer-container">
    <!-- Loading Progress -->
    <div v-if="loading" class="pdf-loading">
      <div class="spinner"></div>
      <p>Loading PDF...</p>
    </div>

    <!-- Error Message -->
    <div v-if="error" class="pdf-error">
      <p>{{ error }}</p>
    </div>

    <!-- PDF Viewer -->
    <div v-if="!loading && !error && pdfUrl" class="pdf-main">
      <!-- Controls -->
      <div class="pdf-controls">
        <button @click="previousPage" :disabled="currentPage <= 1" class="btc-btn-secondary">
          ← Previous
        </button>
        <span class="page-info">
          Page {{ currentPage }} of {{ totalPages }}
        </span>
        <button @click="nextPage" :disabled="currentPage >= totalPages" class="btc-btn-secondary">
          Next →
        </button>
        <button v-if="onDownload" @click="onDownload" class="btc-btn-primary ml-4">
          Download PDF
        </button>
      </div>

      <!-- PDF Canvas -->
      <div class="pdf-canvas-wrapper" :style="wrapperStyle">
        <canvas ref="pdfCanvas" class="pdf-canvas"></canvas>
      </div>

      <!-- Page Navigation -->
      <div class="pdf-nav">
        <input
          v-model.number="currentPage"
          type="number"
          :max="totalPages"
          min="1"
          @keyup.enter="renderPage"
          class="btc-input"
          style="width: 100px"
        />
        <span> / {{ totalPages }}</span>
      </div>
    </div>

    <!-- No PDF -->
    <div v-if="!pdfUrl && !loading && !error" class="pdf-empty">
      <p>No PDF available</p>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted, nextTick, computed } from 'vue'
import * as pdfjsLib from 'pdfjs-dist'
import workerSrc from 'pdfjs-dist/build/pdf.worker.min.mjs?url'

// Set up PDF.js worker
pdfjsLib.GlobalWorkerOptions.workerSrc = workerSrc

const props = defineProps({
  pdfUrl: {
    type: String,
    default: null
  },
  maxHeight: {
    type: [Number, String],
    default: 600
  },
  onDownload: {
    type: Function,
    default: null
  }
})

function resolveMaxHeight(value) {
  if (value === null || value === undefined) return 'none'
  if (typeof value === 'number') return `${value}px`
  const trimmed = String(value).trim()
  if (!trimmed) return 'none'
  return trimmed
}

const wrapperStyle = computed(() => {
  const maxHeight = resolveMaxHeight(props.maxHeight)
  const isUnlimited = maxHeight === 'none'
  return {
    maxHeight,
    overflow: isUnlimited ? 'visible' : 'auto'
  }
})

const pdfCanvas = ref(null)
const currentPage = ref(1)
const totalPages = ref(0)
const loading = ref(false)
const error = ref(null)
let pdfDoc = null

const renderPage = async () => {
  if (!pdfDoc) return

  error.value = null

  try {
    // Validate page number
    if (currentPage.value < 1) currentPage.value = 1
    if (currentPage.value > totalPages.value) currentPage.value = totalPages.value

    const page = await pdfDoc.getPage(currentPage.value)
    const viewport = page.getViewport({ scale: 1.5 })

    await nextTick()

    const canvas = pdfCanvas.value
    if (!canvas) {
      throw new Error('PDF canvas is not ready yet')
    }

    const context = canvas.getContext('2d')
    if (!context) {
      throw new Error('Unable to create PDF canvas context')
    }

    canvas.width = viewport.width
    canvas.height = viewport.height

    await page.render({
      canvasContext: context,
      viewport: viewport
    }).promise
  } catch (err) {
    error.value = `Failed to render page: ${err.message}`
    console.error(err)
  }
}

const loadPdf = async () => {
  if (!props.pdfUrl) {
    totalPages.value = 0
    return
  }

  loading.value = true
  error.value = null

  try {
    pdfDoc = await pdfjsLib.getDocument(props.pdfUrl).promise
    totalPages.value = pdfDoc.numPages
    currentPage.value = 1
    loading.value = false
    await renderPage()
  } catch (err) {
    error.value = `Failed to load PDF: ${err.message}`
    console.error(err)
    totalPages.value = 0
  } finally {
    loading.value = false
  }
}

const nextPage = async () => {
  if (currentPage.value < totalPages.value) {
    currentPage.value++
    await renderPage()
  }
}

const previousPage = async () => {
  if (currentPage.value > 1) {
    currentPage.value--
    await renderPage()
  }
}

watch(() => props.pdfUrl, () => {
  loadPdf()
})

onMounted(() => {
  loadPdf()
})
</script>

<style scoped>
.pdf-viewer-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;

  background: var(--btc-bg-muted, #f8f9fa);
  border-radius: 0.5rem;
  border: 1px solid var(--btc-border, #e5e7eb);
}

.pdf-loading,
.pdf-error,
.pdf-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 2rem;
  min-height: 400px;
  width: 100%;
}

.pdf-error,
.pdf-empty {
  background: #fff;
  border-radius: 0.5rem;
  color: #666;
}

.pdf-error {
  color: #d32f2f;
  border: 1px solid #ffcdd2;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid var(--btc-primary, #0b5fff);
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.pdf-main {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  background: #fff;
  padding: 1rem;
  border-radius: 0.5rem;
  border: 1px solid var(--btc-border, #e5e7eb);
}

.pdf-controls {
  display: flex;
  gap: 1rem;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  padding: 0.5rem;
  border-bottom: 1px solid var(--btc-border, #e5e7eb);
}

.page-info {
  font-size: 0.875rem;
  color: #666;
  min-width: 150px;
  text-align: center;
}

.pdf-canvas-wrapper {
  display: block;
}

.pdf-canvas {
  display: block;
  margin: 0 auto;
  max-width: 100%;
  height: auto;
  border: 1px solid var(--btc-border, #e5e7eb);
  border-radius: 0.25rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.pdf-nav {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  border-top: 1px solid var(--btc-border, #e5e7eb);
}

.ml-4 {
  margin-left: 1rem;
}
</style>
