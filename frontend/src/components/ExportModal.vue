<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  batchId: { type: String, required: true }
})
const emit = defineEmits(['close', 'export'])

const columns = [
  { key: 'id', label: 'ID', group: 'original' },
  { key: 'subject', label: 'Subject', group: 'original' },
  { key: 'description', label: 'Description', group: 'original' },
  { key: 'category', label: 'Category', group: 'classification' },
  { key: 'priority', label: 'Priority', group: 'classification' },
  { key: 'department', label: 'Department', group: 'classification' },
  { key: 'sentiment', label: 'Sentiment', group: 'classification' },
  { key: 'tags', label: 'Tags', group: 'classification' },
  { key: 'summary', label: 'Summary', group: 'classification' },
  { key: 'confidence', label: 'Confidence', group: 'classification' },
  { key: 'justification', label: 'Justification', group: 'classification' },
  { key: 'modified', label: 'Modified', group: 'audit' },
  { key: 'modifiedDate', label: 'Modification Date', group: 'audit' },
]

const selected = ref(new Set(columns.map(c => c.key)))

const groups = [
  { id: 'original', label: 'Original Data', ico: 'fa-file-lines', color: 'text-slate-600' },
  { id: 'classification', label: 'AI Classification', ico: 'fa-robot', color: 'text-indigo-600' },
  { id: 'audit', label: 'Audit', ico: 'fa-clock-rotate-left', color: 'text-amber-600' },
]

const totalSelected = computed(() => selected.value.size)

function toggle(key) {
  if (selected.value.has(key)) selected.value.delete(key)
  else selected.value.add(key)
  selected.value = new Set(selected.value)
}

function toggleGroup(groupId) {
  const cols = columns.filter(c => c.group === groupId)
  const allChecked = cols.every(c => selected.value.has(c.key))
  cols.forEach(c => {
    if (allChecked) selected.value.delete(c.key)
    else selected.value.add(c.key)
  })
  selected.value = new Set(selected.value)
}

function selectAll() {
  selected.value = new Set(columns.map(c => c.key))
}

function clearAll() {
  selected.value = new Set()
}

function exportColumns() {
  const cols = columns.filter(c => selected.value.has(c.key)).map(c => c.key)
  emit('export', cols)
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-md w-full">
      <div class="border-b border-slate-200 p-5 flex items-center justify-between">
        <div>
          <span class="text-[10px] uppercase font-bold text-emerald-600 tracking-widest block">Export CSV</span>
          <h3 class="text-lg font-bold text-slate-900">Configure columns</h3>
        </div>
        <button @click="emit('close')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
          <i class="fa-solid fa-xmark text-lg"></i>
        </button>
      </div>

      <div class="p-6 space-y-5">
        <!-- Quick actions -->
        <div class="flex items-center justify-between">
          <span class="text-xs text-slate-500">
            <b class="text-slate-700">{{ totalSelected }}</b> of {{ columns.length }} columns
          </span>
          <div class="flex gap-2">
            <button @click="selectAll" class="text-xs text-indigo-600 hover:text-indigo-800 font-medium cursor-pointer">All</button>
            <span class="text-slate-300">|</span>
            <button @click="clearAll" class="text-xs text-slate-500 hover:text-slate-700 font-medium cursor-pointer">None</button>
          </div>
        </div>

        <!-- Column groups -->
        <div v-for="g in groups" :key="g.id" class="space-y-2">
          <button @click="toggleGroup(g.id)" class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider cursor-pointer hover:opacity-80 transition w-full">
            <i class="fa-solid" :class="[g.ico, g.color]"></i>
            <span class="text-slate-500">{{ g.label }}</span>
            <span class="ml-auto text-[10px] text-slate-400 font-normal">
              {{ columns.filter(c => c.group === g.id && selected.has(c.key)).length }}/{{ columns.filter(c => c.group === g.id).length }}
            </span>
          </button>
          <div class="grid grid-cols-2 gap-1.5">
            <label v-for="col in columns.filter(c => c.group === g.id)" :key="col.key"
              :class="selected.has(col.key) ? 'bg-indigo-50 border-indigo-200 text-indigo-800' : 'bg-slate-50 border-slate-200 text-slate-500'"
              class="flex items-center gap-2 px-3 py-2 rounded-lg border text-sm cursor-pointer transition hover:shadow-sm">
              <input type="checkbox" :checked="selected.has(col.key)" @change="toggle(col.key)"
                class="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
              {{ col.label }}
            </label>
          </div>
        </div>
      </div>

      <div class="border-t border-slate-200 p-5 flex justify-end gap-3">
        <button @click="emit('close')" class="px-4 py-2.5 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50 transition cursor-pointer">
          Cancel
        </button>
        <button @click="exportColumns" :disabled="totalSelected === 0"
          class="bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center gap-2">
          <i class="fa-solid fa-file-export"></i>
          Export {{ totalSelected }} column(s)
        </button>
      </div>
    </div>
  </div>
</template>
