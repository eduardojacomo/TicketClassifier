<script setup>
import { ref } from 'vue'

const props = defineProps({
  ticket: { type: Object, required: true },
  similars: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false }
})
const emit = defineEmits(['close', 'remove-association', 'view-ticket'])

const removed = ref(new Set())

function remove(id) {
  removed.value.add(id)
  emit('remove-association', props.ticket.id, id)
}

function getSentimentStyle(s) {
  switch (s) {
    case 'positive': return 'bg-emerald-100 text-emerald-800'
    case 'negative': return 'bg-rose-100 text-rose-800'
    default: return 'bg-slate-100 text-slate-600'
  }
}

function getPriorityStyle(p) {
  switch (p) {
    case 'Critical': return 'bg-rose-100 text-rose-800'
    case 'High': return 'bg-amber-100 text-amber-800'
    case 'Medium': return 'bg-blue-100 text-blue-800'
    case 'Low': return 'bg-emerald-100 text-emerald-800'
    default: return 'bg-slate-100 text-slate-700'
  }
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
      <div class="sticky top-0 bg-white border-b border-slate-200 p-5 flex items-center justify-between rounded-t-2xl z-10">
        <div>
          <span class="text-[10px] uppercase font-bold text-violet-600 tracking-widest block">Similar Issues</span>
          <h3 class="text-lg font-bold text-slate-900">
            {{ ticket.externalId || ticket.id?.substring(0, 8) }}
            <span class="text-sm font-normal text-slate-500 ml-2">{{ ticket.category }} · {{ ticket.department }}</span>
          </h3>
        </div>
        <button @click="emit('close')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
          <i class="fa-solid fa-xmark text-lg"></i>
        </button>
      </div>

      <div class="p-6 space-y-4">
        <!-- Reference ticket -->
        <div class="bg-indigo-50 rounded-xl border border-indigo-100 p-4">
          <span class="text-[10px] font-bold text-indigo-500 uppercase tracking-wider block mb-2">Reference ticket</span>
          <p class="text-sm text-slate-900 font-medium">{{ ticket.subject }}</p>
          <div class="flex flex-wrap gap-1.5 mt-2">
            <span v-for="tag in (ticket.tags || [])" :key="tag" class="bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded text-[10px] font-medium">{{ tag }}</span>
          </div>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="text-center py-8">
          <i class="fa-solid fa-spinner animate-spin text-2xl text-slate-300"></i>
          <p class="text-sm text-slate-400 mt-2">Searching for similar tickets...</p>
        </div>

        <!-- Similars list -->
        <div v-else-if="similars.length > 0" class="space-y-2">
          <p class="text-xs text-slate-500 font-medium">
            <i class="fa-solid fa-link text-violet-500 mr-1"></i>
            {{ similars.length }} ticket(s) with matching tags, category, and department
          </p>

          <div v-for="s in similars" :key="s.id"
            :class="removed.has(s.id) ? 'opacity-40 pointer-events-none' : ''"
            class="bg-white rounded-xl border border-slate-200 p-4 flex items-start gap-4 hover:bg-slate-50 transition">

            <div class="grow min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="font-mono text-[10px] font-bold text-slate-400">{{ s.externalId }}</span>
                <span :class="getPriorityStyle(s.priority)" class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase">{{ s.priority }}</span>
                <span :class="getSentimentStyle(s.sentiment)" class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase">{{ s.sentiment }}</span>
                <span v-if="s.recordModified" class="text-[10px] text-amber-600" title="Manually modified record">
                  <i class="fa-solid fa-pen-fancy"></i>
                </span>
              </div>
              <p class="text-sm text-slate-900 font-medium truncate">{{ s.subject }}</p>
              <p class="text-xs text-slate-500 truncate mt-0.5">{{ s.summary }}</p>
              <div class="flex flex-wrap gap-1 mt-2">
                <span v-for="tag in (s.tags || [])" :key="tag"
                  :class="(ticket.tags || []).includes(tag) ? 'bg-violet-100 text-violet-700 border-violet-200' : 'bg-slate-100 text-slate-500 border-slate-200'"
                  class="px-2 py-0.5 rounded text-[10px] font-medium border">
                  {{ tag }}
                </span>
              </div>
            </div>

            <div class="flex flex-col gap-1.5 shrink-0">
              <button @click="emit('view-ticket', s)" class="p-1.5 rounded-lg hover:bg-indigo-50 text-slate-400 hover:text-indigo-600 transition cursor-pointer" title="View details">
                <i class="fa-solid fa-eye text-sm"></i>
              </button>
              <button @click="remove(s.id)" class="p-1.5 rounded-lg hover:bg-rose-50 text-slate-400 hover:text-rose-600 transition cursor-pointer" title="Remove association">
                <i class="fa-solid fa-link-slash text-sm"></i>
              </button>
            </div>
          </div>
        </div>

        <!-- No similars -->
        <div v-else class="text-center py-8">
          <i class="fa-solid fa-circle-nodes text-3xl text-slate-300 block mb-2"></i>
          <p class="text-sm text-slate-400">No similar tickets found in this batch.</p>
          <p class="text-xs text-slate-400 mt-1">Similarity is based on tags + category + department.</p>
        </div>
      </div>
    </div>
  </div>
</template>
