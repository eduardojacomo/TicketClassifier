<script setup>
import { ref, computed, watch } from 'vue'
import { priorityColors } from '../utils/chartMappers'
import BarChart from './BarChart.vue'
import DoughnutChart from './DoughnutChart.vue'
import TicketDetailModal from './TicketDetailModal.vue'
import TicketEditModal from './TicketEditModal.vue'
import SimilarTicketsModal from './SimilarTicketsModal.vue'
import ExportModal from './ExportModal.vue'

const props = defineProps({
  batch: { type: Object, required: true },
  reprocessando: { type: Boolean, default: false },
  reprocessandoTudo: { type: Boolean, default: false }
})

const emit = defineEmits(['back', 'reprocess', 'reprocess-all', 'download', 'update-ticket', 'get-similars'])

const CONF_MIN = 0.7
const pageSize = 10

const fCategory = ref('')
const fPriority = ref('')
const fDepartment = ref('')
const fSentiment = ref('')
const fSearch = ref('')
const fLowConfOnly = ref(false)
const fModifiedOnly = ref(false)
const page = ref(1)

const ticketDetail = ref(null)
const ticketEditing = ref(null)
const savingEdit = ref(false)

const showExport = ref(false)

const ticketSimilar = ref(null)
const similarList = ref([])
const loadingSimilars = ref(false)

const stats = computed(() => props.batch?.statistics)
const extractOptions = (dic) => Object.entries(dic || {}).filter(([, v]) => v > 0).map(([k]) => k)

const sentimentColors = { positive: '#10b981', negative: '#ef4444', neutral: '#94a3b8' }

const totalModified = computed(() => (props.batch?.tickets ?? []).filter(t => t.recordModified).length)

const kpis = computed(() => {
  const s = stats.value; if (!s) return []
  const sortedCategories = Object.entries(s.byCategory || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
  const topCat = sortedCategories[0]

  return [
    { label: 'Total Processed', val: s.total, icon: 'fa-list', bg: 'bg-slate-100', iconColor: 'text-slate-600' },
    { label: 'Critical', val: s.byPriority?.['Critical'] ?? 0, icon: 'fa-circle-exclamation', bg: 'bg-rose-50', iconColor: 'text-rose-600' },
    { label: 'Failures', val: s.failures, icon: 'fa-rotate', bg: 'bg-amber-50', iconColor: 'text-amber-600' },
    { label: 'Avg Confidence', val: `${Math.round((s.averageConfidence || 0) * 100)}%`, icon: 'fa-bullseye', bg: 'bg-indigo-50', iconColor: 'text-indigo-600' },
    { label: 'Modified', val: totalModified.value, icon: 'fa-pen-fancy', bg: 'bg-violet-50', iconColor: 'text-violet-600' },
  ]
})

const filteredTickets = computed(() => {
  const ts = props.batch?.tickets ?? []
  const searchTerm = fSearch.value.trim().toLowerCase()

  return ts.filter(t =>
    (!fCategory.value || t.category === fCategory.value) &&
    (!fPriority.value || t.priority === fPriority.value) &&
    (!fDepartment.value || t.department === fDepartment.value) &&
    (!fSentiment.value || t.sentiment === fSentiment.value) &&
    (!fLowConfOnly.value || t.confidence < CONF_MIN) &&
    (!fModifiedOnly.value || t.recordModified) &&
    (!searchTerm || `${t.subject} ${t.description} ${t.summary}`.toLowerCase().includes(searchTerm))
  )
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredTickets.value.length / pageSize)))
const pageTickets = computed(() => {
  const start = (page.value - 1) * pageSize
  return filteredTickets.value.slice(start, start + pageSize)
})

watch([fCategory, fPriority, fDepartment, fSentiment, fSearch, fLowConfOnly, fModifiedOnly], () => { page.value = 1 })

function getPriorityStyle(p) {
  switch (p) {
    case 'Critical': return 'bg-rose-100 text-rose-800 border-rose-200'
    case 'High': return 'bg-amber-100 text-amber-800 border-amber-200'
    case 'Medium': return 'bg-blue-100 text-blue-800 border-blue-200'
    case 'Low': return 'bg-emerald-100 text-emerald-800 border-emerald-200'
    default: return 'bg-slate-100 text-slate-700 border-slate-200'
  }
}

function getConfStyle(c) {
  if (c >= 0.8) return 'text-emerald-700 bg-emerald-50'
  if (c >= 0.6) return 'text-blue-700 bg-blue-50'
  if (c >= 0.4) return 'text-amber-700 bg-amber-50'
  return 'text-rose-700 bg-rose-50'
}

function getSentimentStyle(s) {
  switch (s) {
    case 'positive': return 'bg-emerald-100 text-emerald-800 border-emerald-200'
    case 'negative': return 'bg-rose-100 text-rose-800 border-rose-200'
    default: return 'bg-slate-100 text-slate-600 border-slate-200'
  }
}

function getSentimentIcon(s) {
  switch (s) {
    case 'positive': return 'fa-face-smile'
    case 'negative': return 'fa-face-frown'
    default: return 'fa-face-meh'
  }
}

function openDetail(t) { ticketDetail.value = t }
function openEdit(t) { ticketDetail.value = null; ticketEditing.value = t }

async function saveEdit(dto) {
  if (!ticketEditing.value) return
  savingEdit.value = true
  emit('update-ticket', ticketEditing.value.id, dto, () => {
    savingEdit.value = false
    ticketEditing.value = null
  })
}

async function openSimilars(t) {
  ticketSimilar.value = t
  loadingSimilars.value = true
  similarList.value = []
  emit('get-similars', t.id, (data) => {
    similarList.value = data
    loadingSimilars.value = false
  })
}

function viewSimilarTicket(t) {
  ticketSimilar.value = null
  ticketDetail.value = t
}

function exportWithColumns(cols) {
  showExport.value = false
  emit('download', cols)
}
</script>

<template>
  <div class="space-y-6">

    <!-- Modals -->
    <TicketDetailModal v-if="ticketDetail" :ticket="ticketDetail" @close="ticketDetail = null" @edit="openEdit" />
    <TicketEditModal v-if="ticketEditing" :ticket="ticketEditing" :saving="savingEdit" @close="ticketEditing = null" @save="saveEdit" />
    <SimilarTicketsModal v-if="ticketSimilar" :ticket="ticketSimilar" :similars="similarList" :loading="loadingSimilars"
      @close="ticketSimilar = null" @view-ticket="viewSimilarTicket" />
    <ExportModal v-if="showExport" :batch-id="batch.batchId" @close="showExport = false" @export="exportWithColumns" />

    <!-- Top Bar -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div class="flex items-center gap-3">
        <button @click="emit('back')" class="bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 p-2.5 rounded-xl transition cursor-pointer">
          <i class="fa-solid fa-arrow-left"></i>
        </button>
        <div>
          <h2 class="text-2xl font-bold tracking-tight text-slate-900">{{ batch.fileName }}</h2>
          <p class="text-sm text-slate-500">AI classification results</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <button v-if="stats?.failures > 0" :disabled="reprocessando" @click="emit('reprocess')"
                class="bg-amber-500 hover:bg-amber-400 disabled:opacity-50 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
          <i class="fa-solid fa-rotate" :class="{ 'animate-spin': reprocessando }"></i>
          {{ reprocessando ? 'Reprocessing...' : `Reprocess ${stats.failures} failure(s)` }}
        </button>
        <button :disabled="reprocessandoTudo" @click="emit('reprocess-all')"
                class="bg-violet-600 hover:bg-violet-500 disabled:opacity-50 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
          <i class="fa-solid fa-arrows-rotate" :class="{ 'animate-spin': reprocessandoTudo }"></i>
          {{ reprocessandoTudo ? 'Reprocessing...' : 'Reprocess all' }}
        </button>
        <button @click="showExport = true" class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
          <i class="fa-solid fa-file-export"></i>
          Export CSV
        </button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
      <div v-for="k in kpis" :key="k.label" class="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm flex items-center space-x-4">
        <div :class="[k.bg, k.iconColor]" class="w-12 h-12 rounded-xl flex items-center justify-center text-xl shrink-0">
          <i class="fa-solid" :class="k.icon"></i>
        </div>
        <div class="min-w-0">
          <span class="block text-[10px] text-slate-400 font-semibold uppercase tracking-wider truncate">{{ k.label }}</span>
          <span class="text-2xl font-bold text-slate-950">{{ k.val }}</span>
        </div>
      </div>
    </div>

    <!-- Charts -->
    <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
      <BarChart title="BY CATEGORY" :data="stats?.byCategory" />
      <BarChart title="BY DEPARTMENT" :data="stats?.byDepartment" />
      <DoughnutChart title="BY PRIORITY" :data="stats?.byPriority" :colors="priorityColors" />
      <DoughnutChart title="BY SENTIMENT" :data="stats?.bySentiment" :colors="sentimentColors" />
    </div>

    <!-- Data Table -->
    <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h3 class="text-lg font-bold text-slate-900">Individual Results</h3>
          <p class="text-xs text-slate-500">Browse and filter AI-classified tickets.</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-2">
        <div class="relative max-w-xs">
          <i class="fa-solid fa-search absolute left-3 top-2.5 text-slate-400 text-xs"></i>
          <input v-model="fSearch" type="text" placeholder="Search..." class="pl-9 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 w-full" />
        </div>
        <select v-model="fCategory" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Category: all</option>
          <option v-for="o in extractOptions(stats?.byCategory)" :key="o">{{ o }}</option>
        </select>
        <select v-model="fPriority" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Priority: all</option>
          <option v-for="o in extractOptions(stats?.byPriority)" :key="o">{{ o }}</option>
        </select>
        <select v-model="fDepartment" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Department: all</option>
          <option v-for="o in extractOptions(stats?.byDepartment)" :key="o">{{ o }}</option>
        </select>
        <select v-model="fSentiment" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Sentiment: all</option>
          <option value="positive">Positive</option>
          <option value="negative">Negative</option>
          <option value="neutral">Neutral</option>
        </select>
        <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer bg-slate-50 border border-slate-200 px-3 py-2 rounded-xl hover:bg-slate-100 transition">
          <input type="checkbox" v-model="fLowConfOnly" class="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
          Only &lt; 70%
        </label>
        <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer bg-slate-50 border border-slate-200 px-3 py-2 rounded-xl hover:bg-slate-100 transition">
          <input type="checkbox" v-model="fModifiedOnly" class="rounded border-slate-300 text-amber-600 focus:ring-amber-500" />
          Modified
        </label>
      </div>

      <!-- Table -->
      <div class="overflow-x-auto rounded-xl border border-slate-100">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-100 text-xs font-bold text-slate-400 uppercase tracking-wider">
              <th class="p-4">ID</th>
              <th class="p-4">Subject</th>
              <th class="p-4">Category</th>
              <th class="p-4">Priority</th>
              <th class="p-4">Dept</th>
              <th class="p-4">Sentiment</th>
              <th class="p-4">Tags</th>
              <th class="p-4 text-center">Conf.</th>
              <th class="p-4 text-center">Sim.</th>
              <th class="p-4 text-center">Actions</th>
            </tr>
          </thead>
          <tbody class="text-sm divide-y divide-slate-100">
            <tr v-for="t in pageTickets" :key="t.id"
              :class="t.recordModified ? 'bg-amber-50/30 hover:bg-amber-50/50' : 'hover:bg-slate-50/50'"
              class="transition duration-150">
              <td class="p-4 font-mono font-bold text-slate-400 text-xs">
                <div class="flex items-center gap-1.5">
                  {{ t.externalId }}
                  <i v-if="t.recordModified" class="fa-solid fa-pen-fancy text-[10px] text-amber-500" title="Manually modified"></i>
                </div>
              </td>
              <td class="p-4 text-slate-900 font-medium max-w-[160px] truncate">{{ t.subject }}</td>
              <td class="p-4">
                <span class="bg-indigo-50 text-indigo-700 border border-indigo-100 px-2.5 py-1 rounded-full text-xs font-semibold inline-flex items-center gap-1">
                  {{ t.category }}
                </span>
              </td>
              <td class="p-4">
                <span :class="getPriorityStyle(t.priority)" class="px-2.5 py-1 rounded-full text-xs font-bold uppercase border inline-flex items-center gap-1">
                  <i class="fa-solid fa-circle text-[5px]"></i>
                  {{ t.priority }}
                </span>
              </td>
              <td class="p-4 text-slate-600 text-xs">{{ t.department }}</td>
              <td class="p-4">
                <span :class="getSentimentStyle(t.sentiment)" class="px-2 py-1 rounded-full text-[10px] font-bold uppercase border inline-flex items-center gap-1">
                  <i class="fa-solid" :class="getSentimentIcon(t.sentiment)"></i>
                  {{ t.sentiment || 'neutral' }}
                </span>
              </td>
              <td class="p-4 max-w-[180px]">
                <div class="flex flex-wrap gap-1">
                  <span v-for="tag in (t.tags || []).slice(0, 3)" :key="tag" class="bg-slate-100 text-slate-600 px-2 py-0.5 rounded text-[10px] font-medium">
                    {{ tag }}
                  </span>
                  <span v-if="(t.tags || []).length > 3" class="text-[10px] text-slate-400">+{{ t.tags.length - 3 }}</span>
                </div>
              </td>
              <td class="p-4 text-center">
                <span :class="getConfStyle(t.confidence)" class="px-2.5 py-1 rounded-full text-xs font-bold inline-flex items-center gap-1">
                  <i v-if="t.confidence < CONF_MIN" class="fa-solid fa-triangle-exclamation text-[10px]"></i>
                  {{ Math.round(t.confidence * 100) }}%
                </span>
              </td>
              <td class="p-4 text-center">
                <button v-if="t.similarCount > 0" @click="openSimilars(t)"
                  class="bg-violet-50 text-violet-700 border border-violet-200 px-2 py-1 rounded-full text-xs font-bold hover:bg-violet-100 transition cursor-pointer inline-flex items-center gap-1">
                  <i class="fa-solid fa-link text-[10px]"></i>
                  {{ t.similarCount }}
                </button>
                <span v-else class="text-slate-300 text-xs">—</span>
              </td>
              <td class="p-4 text-center">
                <div class="flex items-center justify-center gap-1.5">
                  <button @click="openDetail(t)" class="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-indigo-600 transition cursor-pointer" title="View">
                    <i class="fa-solid fa-eye text-sm"></i>
                  </button>
                  <button @click="openEdit(t)" class="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-amber-600 transition cursor-pointer" title="Edit">
                    <i class="fa-solid fa-pen text-sm"></i>
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!filteredTickets.length">
              <td colspan="10" class="p-8 text-center text-slate-400">
                <i class="fa-solid fa-filter-circle-xmark text-2xl text-slate-300 block mb-2"></i>
                No tickets found for the applied filter.
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="flex items-center justify-between pt-4 border-t border-slate-100 text-xs">
        <span class="text-slate-400">
          Showing <b class="text-slate-700">{{ filteredTickets.length ? (page - 1) * pageSize + 1 : 0 }}-{{ Math.min(page * pageSize, filteredTickets.length) }}</b> of <b class="text-slate-700">{{ filteredTickets.length }}</b>
        </span>
        <div class="flex items-center space-x-2">
          <button :disabled="page <= 1" @click="page--" class="p-2 border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 transition cursor-pointer">
            <i class="fa-solid fa-chevron-left"></i>
          </button>
          <span class="font-bold text-slate-800">{{ page }} / {{ totalPages }}</span>
          <button :disabled="page >= totalPages" @click="page++" class="p-2 border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 transition cursor-pointer">
            <i class="fa-solid fa-chevron-right"></i>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
