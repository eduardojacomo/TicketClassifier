<script setup>
defineProps({
  ticket: { type: Object, required: true }
})
const emit = defineEmits(['close', 'edit'])

function getSentimentStyle(s) {
  switch (s) {
    case 'positive': return 'bg-emerald-100 text-emerald-800 border-emerald-200'
    case 'negative': return 'bg-rose-100 text-rose-800 border-rose-200'
    default: return 'bg-slate-100 text-slate-700 border-slate-200'
  }
}

function getSentimentIcon(s) {
  switch (s) {
    case 'positive': return 'fa-face-smile'
    case 'negative': return 'fa-face-frown'
    default: return 'fa-face-meh'
  }
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
      <div class="sticky top-0 bg-white border-b border-slate-200 p-5 flex items-center justify-between rounded-t-2xl z-10">
        <div>
          <span class="text-[10px] uppercase font-bold text-indigo-600 tracking-widest block">Ticket Details</span>
          <h3 class="text-lg font-bold text-slate-900">{{ ticket.externalId || ticket.id?.substring(0, 8) }}</h3>
        </div>
        <div class="flex items-center gap-2">
          <button @click="emit('edit', ticket)" class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2 rounded-xl text-sm font-medium transition cursor-pointer flex items-center gap-2">
            <i class="fa-solid fa-pen-to-square"></i> Edit
          </button>
          <button @click="emit('close')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
            <i class="fa-solid fa-xmark text-lg"></i>
          </button>
        </div>
      </div>

      <div class="p-6 space-y-6">
        <!-- Original Data -->
        <div>
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-file-lines text-slate-500"></i> Original Data
          </h4>
          <div class="bg-slate-50 rounded-xl border border-slate-100 p-4 space-y-3">
            <div>
              <span class="text-[10px] font-bold text-slate-400 uppercase">Subject</span>
              <p class="text-sm text-slate-900 font-medium">{{ ticket.subject || '—' }}</p>
            </div>
            <div>
              <span class="text-[10px] font-bold text-slate-400 uppercase">Description</span>
              <p class="text-sm text-slate-700 whitespace-pre-wrap">{{ ticket.description || '—' }}</p>
            </div>
          </div>
        </div>

        <!-- AI Classification -->
        <div>
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-robot text-indigo-500"></i> AI Classification
          </h4>
          <div class="grid grid-cols-2 gap-3">
            <div class="bg-white rounded-xl border border-slate-200 p-4">
              <span class="text-[10px] font-bold text-slate-400 uppercase">Category</span>
              <p class="text-sm font-semibold text-indigo-700 mt-1">{{ ticket.category }}</p>
            </div>
            <div class="bg-white rounded-xl border border-slate-200 p-4">
              <span class="text-[10px] font-bold text-slate-400 uppercase">Priority</span>
              <p class="text-sm font-semibold mt-1">{{ ticket.priority }}</p>
            </div>
            <div class="bg-white rounded-xl border border-slate-200 p-4">
              <span class="text-[10px] font-bold text-slate-400 uppercase">Department</span>
              <p class="text-sm font-semibold text-slate-700 mt-1">{{ ticket.department }}</p>
            </div>
            <div class="bg-white rounded-xl border border-slate-200 p-4">
              <span class="text-[10px] font-bold text-slate-400 uppercase">Confidence</span>
              <p class="text-sm font-semibold mt-1">{{ Math.round((ticket.confidence || 0) * 100) }}%</p>
            </div>
          </div>
        </div>

        <!-- Sentiment -->
        <div>
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-heart-pulse text-rose-500"></i> Sentiment
          </h4>
          <span :class="getSentimentStyle(ticket.sentiment)" class="px-3 py-1.5 rounded-full text-xs font-bold uppercase border inline-flex items-center gap-1.5">
            <i class="fa-solid" :class="getSentimentIcon(ticket.sentiment)"></i>
            {{ ticket.sentiment || 'neutral' }}
          </span>
        </div>

        <!-- Tags -->
        <div>
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-tags text-amber-500"></i> Tags
          </h4>
          <div class="flex flex-wrap gap-2">
            <span v-for="tag in (ticket.tags || [])" :key="tag" class="bg-indigo-50 text-indigo-700 border border-indigo-100 px-2.5 py-1 rounded-full text-xs font-medium">
              {{ tag }}
            </span>
            <span v-if="!ticket.tags?.length" class="text-xs text-slate-400">No tags</span>
          </div>
        </div>

        <!-- Modified Record -->
        <div v-if="ticket.recordModified">
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-pen-fancy text-amber-500"></i> Modified Record
          </h4>
          <div class="bg-amber-50 rounded-xl border border-amber-200 p-3 text-sm text-amber-800 flex items-center gap-2">
            <i class="fa-solid fa-circle-info"></i>
            This record was manually modified
            <span v-if="ticket.modifiedDate" class="text-xs text-amber-600 ml-auto">
              {{ new Date(ticket.modifiedDate).toLocaleString('en-US') }}
            </span>
          </div>
        </div>

        <!-- Summary & Justification -->
        <div>
          <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
            <i class="fa-solid fa-align-left text-slate-500"></i> Summary & Justification
          </h4>
          <div class="bg-slate-50 rounded-xl border border-slate-100 p-4 space-y-3">
            <div>
              <span class="text-[10px] font-bold text-slate-400 uppercase">Summary</span>
              <p class="text-sm text-slate-700">{{ ticket.summary || '—' }}</p>
            </div>
            <div>
              <span class="text-[10px] font-bold text-slate-400 uppercase">Justification</span>
              <p class="text-sm text-slate-700">{{ ticket.justification || '—' }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
