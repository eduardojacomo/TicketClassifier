<script setup>
import { ref } from 'vue'
import { useTicketProgress } from './composables/useTicketProgress'
import { useTicketClassifier } from './composables/useTicketClassifier'
import { formatDate } from './utils/chartMappers'
import BatchDashboard from './components/BatchDashboard.vue'
import DuplicateModal from './components/DuplicateModal.vue'
import ParametersManager from './components/ParametersManager.vue'

const progressHook = useTicketProgress()
const classifier = useTicketClassifier(progressHook)

const localFile = ref(null)
const dragOver = ref(false)

const duplicateInfo = ref(null)

function handleFileSelection(e) {
  localFile.value = e.target.files?.[0] ?? null
  classifier.error.value = ''
}

function handleDrop(e) {
  dragOver.value = false
  const file = e.dataTransfer.files?.[0]
  if (file && file.name.endsWith('.csv')) {
    localFile.value = file
    classifier.error.value = ''
  }
}

async function triggerUpload() {
  if (!localFile.value) return

  const { duplicate, batch } = await classifier.checkDuplicate(localFile.value.name)
  if (duplicate && batch) {
    duplicateInfo.value = batch
    return
  }

  sendWithOption(false)
}

function sendWithOption(overwrite) {
  duplicateInfo.value = null
  if (!localFile.value) return
  const jobId = self.crypto?.randomUUID?.() ?? ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c => (c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (c / 4)))).toString(16))
  classifier.uploadFile(localFile.value, jobId, overwrite).then(() => {
    localFile.value = null
  })
}
</script>

<template>
  <div class="bg-slate-50 text-slate-800 antialiased min-h-screen flex flex-col font-sans">

    <!-- Duplicate Modal -->
    <DuplicateModal
      v-if="duplicateInfo"
      :file-name="localFile?.name ?? ''"
      :existing-batch="duplicateInfo"
      @overwrite="sendWithOption(true)"
      @create="sendWithOption(false)"
      @cancel="duplicateInfo = null"
    />

    <!-- Processing Modal Overlay -->
    <div v-if="progressHook.processing.value" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-xl w-full overflow-hidden">
        <div class="bg-slate-900 text-white p-5">
          <div class="flex items-center gap-3">
            <div class="w-5 h-5 border-3 border-slate-600 border-t-indigo-400 rounded-full animate-spin"></div>
            <div>
              <span class="text-[10px] uppercase font-bold text-indigo-400 tracking-widest block">Active Pipeline</span>
              <h3 class="text-lg font-bold">Classifying tickets...</h3>
            </div>
            <span class="ml-auto text-sm font-mono font-semibold text-indigo-400 bg-indigo-950 px-3 py-1 rounded-full">
              {{ progressHook.pctComplete.value }}%
            </span>
          </div>
        </div>

        <div class="p-6 space-y-5">
          <!-- Progress Bar -->
          <div class="w-full bg-slate-100 rounded-full h-3 overflow-hidden">
            <div class="bg-linear-to-r from-indigo-500 to-violet-600 h-full rounded-full transition-all duration-300"
                 :style="{ width: progressHook.pctComplete.value + '%' }"></div>
          </div>

          <p class="text-sm text-slate-600">
            <b class="text-slate-900">{{ progressHook.prog.value.processed }}/{{ progressHook.prog.value.total || '...' }}</b> tickets processed
            <span v-if="progressHook.prog.value.totalBatches" class="text-slate-400"> · batch {{ progressHook.prog.value.batchesCompleted }}/{{ progressHook.prog.value.totalBatches }}</span>
            <span v-if="progressHook.estimatedTimeRemaining.value" class="text-slate-400"> · {{ progressHook.estimatedTimeRemaining.value }}</span>
          </p>

          <!-- Mini KPIs -->
          <div class="grid grid-cols-4 gap-3">
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 text-center">
              <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">Total</span>

              <span class="text-xl font-bold text-slate-800">{{ progressHook.prog.value.total }}</span>
            </div>
            <div class="bg-amber-50/50 p-3 rounded-xl border border-amber-100 text-center">
              <span class="block text-[10px] text-amber-500 uppercase font-bold tracking-wider">Pending</span>
              <span class="text-xl font-bold text-amber-700">{{ Math.max(0, progressHook.prog.value.total - progressHook.prog.value.processed) }}</span>
            </div>
            <div class="bg-emerald-50/50 p-3 rounded-xl border border-emerald-100 text-center">
              <span class="block text-[10px] text-emerald-500 uppercase font-bold tracking-wider">OK</span>
              <span class="text-xl font-bold text-emerald-700">{{ progressHook.prog.value.ok }}</span>
            </div>
            <div class="bg-rose-50/50 p-3 rounded-xl border border-rose-100 text-center">
              <span class="block text-[10px] text-rose-500 uppercase font-bold tracking-wider">Failures</span>
              <span class="text-xl font-bold text-rose-700">{{ progressHook.prog.value.failures }}</span>
            </div>
          </div>

          <!-- Console Log -->
          <div class="space-y-2">
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
              <i class="fa-solid fa-terminal text-slate-500"></i> LLM Agent Execution Logs
            </span>
            <div class="bg-slate-900 rounded-xl p-4 font-mono text-xs text-slate-300 h-40 overflow-y-auto space-y-1">
              <div v-for="(l, i) in progressHook.logs.value" :key="i">{{ l }}</div>
              <div class="text-indigo-400 animate-blink">_</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Header -->
    <header class="bg-white border-b border-slate-200 sticky top-0 z-40">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16 items-center">
          <!-- Logo -->
          <div class="flex items-center space-x-3">
            <div class="bg-indigo-600 text-white p-2.5 rounded-xl flex items-center justify-center shadow-lg shadow-indigo-100">
              <i class="fa-solid fa-robot text-lg"></i>
            </div>
            <div>
              <h1 class="text-lg font-bold tracking-tight text-slate-900 flex items-center gap-2">
                HelpDesk AI
                <span class="text-[10px] bg-indigo-50 text-indigo-700 px-2 py-0.5 rounded-full font-medium border border-indigo-100">Classifier</span>
              </h1>
              <p class="text-xs text-slate-500">Intelligent ticket classification</p>
            </div>
          </div>

          <!-- Navigation -->
          <nav class="hidden md:flex space-x-1 bg-slate-100 p-1 rounded-lg">
            <button
              @click="classifier.view.value = 'upload'"
              :class="classifier.view.value === 'upload' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="px-4 py-1.5 rounded-md text-sm font-medium transition duration-150 flex items-center gap-2 cursor-pointer"
            >
              <i class="fa-solid fa-cloud-arrow-up"></i>
              Upload
            </button>
            <button
              @click="classifier.loadBatches"
              :class="(classifier.view.value === 'batches' || classifier.view.value === 'detail') ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="px-4 py-1.5 rounded-md text-sm font-medium transition duration-150 flex items-center gap-2 cursor-pointer"
            >
              <i class="fa-solid fa-history"></i>
              Batches
            </button>
            <button
              @click="classifier.view.value = 'parametros'"
              :class="classifier.view.value === 'parametros' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="px-4 py-1.5 rounded-md text-sm font-medium transition duration-150 flex items-center gap-2 cursor-pointer"
            >
              <i class="fa-solid fa-sliders"></i>
              Rules
            </button>
          </nav>

          <!-- Mobile nav -->
          <div class="flex md:hidden space-x-2">
            <button
              @click="classifier.view.value = 'upload'"
              :class="classifier.view.value === 'upload' ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600'"
              class="p-2.5 rounded-lg text-sm cursor-pointer"
            >
              <i class="fa-solid fa-cloud-arrow-up"></i>
            </button>
            <button
              @click="classifier.loadBatches"
              :class="(classifier.view.value === 'batches' || classifier.view.value === 'detail') ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600'"
              class="p-2.5 rounded-lg text-sm cursor-pointer"
            >
              <i class="fa-solid fa-history"></i>
            </button>
            <button
              @click="classifier.view.value = 'parametros'"
              :class="classifier.view.value === 'parametros' ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600'"
              class="p-2.5 rounded-lg text-sm cursor-pointer"
            >
              <i class="fa-solid fa-sliders"></i>
            </button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="grow max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">

      <!-- Error Toast -->
      <div v-if="classifier.error.value" class="mb-6 bg-rose-50 border border-rose-200 text-rose-700 px-5 py-4 rounded-xl flex items-center gap-3">
        <i class="fa-solid fa-triangle-exclamation text-rose-500"></i>
        <span class="text-sm font-medium">{{ classifier.error.value }}</span>
        <button @click="classifier.error.value = ''" class="ml-auto text-rose-400 hover:text-rose-600 cursor-pointer"><i class="fa-solid fa-xmark"></i></button>
      </div>

      <!-- UPLOAD VIEW -->
      <div v-if="classifier.view.value === 'upload'" class="space-y-6">

        <!-- Hero Banner -->
        <div class="bg-linear-to-r from-slate-900 via-indigo-950 to-slate-950 rounded-2xl p-6 sm:p-8 text-white shadow-xl">
          <div class="space-y-3 max-w-2xl">
            <h2 class="text-2xl font-bold tracking-tight">AI Ticket Classification</h2>
            <p class="text-slate-300 text-sm leading-relaxed">
              Upload your support spreadsheet in CSV format. The system automatically classifies category, priority, and department using artificial intelligence, processing in batches to avoid bottlenecks.
            </p>
          </div>
        </div>

        <!-- Upload Card -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2">
            <div
              @dragover.prevent="dragOver = true"
              @dragleave="dragOver = false"
              @drop.prevent="handleDrop"
              :class="dragOver ? 'border-indigo-500 bg-indigo-50/50' : localFile ? 'border-emerald-400 bg-emerald-50/30' : 'border-slate-300 bg-white hover:border-slate-400'"
              class="border-2 border-dashed rounded-2xl p-12 text-center transition-all duration-150 flex flex-col items-center justify-center min-h-[320px]"
            >
              <input type="file" id="csv-upload" class="hidden" accept=".csv" @change="handleFileSelection">

              <!-- No file selected -->
              <label v-if="!localFile" for="csv-upload" class="cursor-pointer flex flex-col items-center">
                <div class="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-4 hover:scale-105 transition-transform">
                  <i class="fa-solid fa-file-csv text-3xl text-indigo-600"></i>
                </div>
                <h3 class="text-lg font-bold text-slate-900">Drag your CSV file or click to browse</h3>
                <p class="text-xs text-slate-500 mt-2 max-w-xs mx-auto">
                  Recognized columns: subject/assunto, description/descricao/body, id (optional)
                </p>
              </label>

              <!-- File selected -->
              <div v-else class="flex flex-col items-center">
                <div class="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mb-4">
                  <i class="fa-solid fa-file-circle-check text-3xl text-emerald-600"></i>
                </div>
                <h3 class="text-lg font-bold text-slate-900">{{ localFile.name }}</h3>
                <p class="text-xs text-slate-500 mt-1">{{ (localFile.size / 1024).toFixed(1) }} KB</p>

                <div class="flex gap-3 mt-6">
                  <button
                    @click="triggerUpload"
                    :disabled="progressHook.processing.value"
                    class="bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-6 py-3 rounded-xl font-semibold text-sm transition shadow-lg shadow-indigo-100 flex items-center gap-2 cursor-pointer"
                  >
                    <i class="fa-solid fa-wand-magic-sparkles"></i>
                    Classify tickets
                  </button>
                  <label for="csv-upload" class="border border-slate-300 hover:bg-slate-50 text-slate-700 px-5 py-3 rounded-xl font-semibold text-sm transition cursor-pointer flex items-center gap-2">
                    <i class="fa-solid fa-arrows-rotate"></i>
                    Change file
                  </label>
                </div>
              </div>

              <!-- Format hints -->
              <div v-if="!localFile" class="mt-8 pt-6 border-t border-slate-100 w-full max-w-md flex justify-around text-xs text-slate-400">
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> Ticket ID</span>
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> Description</span>
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> Subject</span>
              </div>
            </div>
          </div>

          <!-- Architecture Info Panel -->
          <div class="space-y-6">
            <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-5">
              <h3 class="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                <i class="fa-solid fa-network-wired text-indigo-600"></i>
                How It Works
              </h3>

              <div class="space-y-3.5 text-xs text-slate-600">
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">1</span>
                  <div>
                    <p class="font-semibold text-slate-800">Upload CSV</p>
                    <p class="text-slate-500">Import your support tickets from a spreadsheet.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">2</span>
                  <div>
                    <p class="font-semibold text-slate-800">Batch Processing</p>
                    <p class="text-slate-500">Tickets are organized into batches for efficient processing.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">3</span>
                  <div>
                    <p class="font-semibold text-slate-800">AI Classification</p>
                    <p class="text-slate-500">Each ticket is automatically categorized, prioritized, and routed.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">4</span>
                  <div>
                    <p class="font-semibold text-slate-800">Results Dashboard</p>
                    <p class="text-slate-500">Visualize results with charts, filters, and export options.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- BATCH LIST VIEW -->
      <div v-else-if="classifier.view.value === 'batches'" class="space-y-6">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-2xl font-bold tracking-tight text-slate-900">Processed Batches</h2>
            <p class="text-sm text-slate-500">History of all processed spreadsheets.</p>
          </div>
          <button @click="classifier.view.value = 'upload'" class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
            <i class="fa-solid fa-plus"></i>
            New Upload
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
          <p v-if="!classifier.batches.value.length" class="text-center text-slate-400 py-12">
            <i class="fa-solid fa-inbox text-4xl text-slate-300 block mb-3"></i>
            No batches processed yet. Upload a CSV to get started.
          </p>

          <div v-else class="overflow-x-auto rounded-xl border border-slate-100">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-100 text-xs font-bold text-slate-400 uppercase tracking-wider">
                  <th class="p-4">File</th>
                  <th class="p-4 text-center">Tickets</th>
                  <th class="p-4">Date</th>
                  <th class="p-4 text-center">Actions</th>
                </tr>
              </thead>
              <tbody class="text-sm divide-y divide-slate-100">
                <tr v-for="l in classifier.batches.value" :key="l.batchId" class="hover:bg-slate-50/50 transition duration-150">
                  <td class="p-4 text-slate-900 font-semibold flex items-center gap-2">
                    <i class="fa-solid fa-file-csv text-indigo-600"></i>
                    {{ l.fileName }}
                  </td>
                  <td class="p-4 text-center">
                    <span class="bg-slate-100 text-slate-700 px-2.5 py-1 rounded-full text-xs font-bold">{{ l.total }}</span>
                  </td>
                  <td class="p-4 text-slate-500">{{ formatDate(l.createdDate) }}</td>
                  <td class="p-4 text-center">
                    <button @click="classifier.openDetail(l.batchId)" class="bg-slate-100 hover:bg-slate-200 text-slate-700 px-4 py-2 rounded-lg text-xs font-semibold transition cursor-pointer flex items-center gap-1.5 mx-auto">
                      <i class="fa-solid fa-chart-line text-indigo-600"></i>
                      Open
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- PARAMETROS VIEW -->
      <ParametersManager v-else-if="classifier.view.value === 'parametros'" />

      <!-- BATCH DETAIL VIEW -->
      <BatchDashboard
        v-else-if="classifier.view.value === 'detail' && classifier.currentBatch.value"
        :batch="classifier.currentBatch.value"
        :reprocessando="classifier.reprocessing.value"
        :reprocessando-tudo="classifier.reprocessingAll.value"
        @back="classifier.loadBatches"
        @reprocess="classifier.reprocessBatch"
        @reprocess-all="classifier.reprocessAll"
        @download="(cols) => classifier.downloadExport(cols)"
        @update-ticket="(id, dto, done) => classifier.updateTicket(id, dto).finally(done)"
        @get-similars="(id, cb) => classifier.getSimilars(id).then(cb)"
      />
    </main>

    <!-- Footer -->
    <footer class="bg-white border-t border-slate-200 py-6 mt-auto">
      <div class="max-w-7xl mx-auto px-4 text-center text-xs text-slate-400 space-y-1">
        <p>HelpDesk AI Ticket Classifier</p>
      </div>
    </footer>
  </div>
</template>
