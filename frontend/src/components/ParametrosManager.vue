<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import api from '../api'

const types = ref([])
const selectedType = ref('')
const parameters = ref([])
const loading = ref(false)
const error = ref('')

const form = ref({ tipo: '', termo: '', alvo: '', ativo: true })
const editingId = ref(null)
const showForm = ref(false)
const saving = ref(false)

const typeLabels = {
  categoria: 'Category',
  prioridade: 'Priority',
  departamento: 'Department',
  pergunta: 'Question',
  reclamacao: 'Complaint',
  sentimento_positivo: 'Positive Sentiment',
  sentimento_negativo: 'Negative Sentiment',
  tag: 'Tag',
}

const typeColors = {
  categoria: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  prioridade: 'bg-amber-50 text-amber-700 border-amber-200',
  departamento: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  pergunta: 'bg-sky-50 text-sky-700 border-sky-200',
  reclamacao: 'bg-rose-50 text-rose-700 border-rose-200',
  sentimento_positivo: 'bg-lime-50 text-lime-700 border-lime-200',
  sentimento_negativo: 'bg-orange-50 text-orange-700 border-orange-200',
  tag: 'bg-violet-50 text-violet-700 border-violet-200',
}

const typeIcons = {
  categoria: 'fa-folder',
  prioridade: 'fa-flag',
  departamento: 'fa-building',
  pergunta: 'fa-circle-question',
  reclamacao: 'fa-face-angry',
  sentimento_positivo: 'fa-face-smile',
  sentimento_negativo: 'fa-face-frown',
  tag: 'fa-tag',
}

const filteredParameters = computed(() => {
  if (!selectedType.value) return parameters.value
  return parameters.value.filter(p => p.tipo === selectedType.value)
})

const countByType = computed(() => {
  const c = {}
  for (const p of parameters.value) {
    c[p.tipo] = (c[p.tipo] || 0) + 1
  }
  return c
})

async function loadTypes() {
  try {
    const { data } = await api.get('/parameters/types')
    types.value = data
  } catch { /* static types on backend */ }
}

async function loadParameters() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/parameters')
    parameters.value = data
  } catch (e) {
    error.value = 'Failed to load parameters: ' + (e.response?.data ?? e.message)
  } finally {
    loading.value = false
  }
}

function openNew(tipo) {
  editingId.value = null
  form.value = { tipo: tipo || selectedType.value || 'categoria', termo: '', alvo: '', ativo: true }
  showForm.value = true
}

function openEdit(p) {
  editingId.value = p.id
  form.value = { tipo: p.tipo, termo: p.termo, alvo: p.alvo || '', ativo: p.ativo }
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingId.value = null
}

async function save() {
  if (!form.value.termo.trim()) return
  saving.value = true
  error.value = ''
  try {
    const payload = {
      tipo: form.value.tipo,
      termo: form.value.termo.trim(),
      alvo: form.value.alvo.trim() || null,
      ativo: form.value.ativo,
    }
    if (editingId.value) {
      await api.put(`/parametros/${editingId.value}`, payload)
    } else {
      await api.post('/parameters', payload)
    }
    closeForm()
    await loadParameters()
  } catch (e) {
    error.value = 'Failed to save: ' + (e.response?.data ?? e.message)
  } finally {
    saving.value = false
  }
}

async function toggleActive(p) {
  try {
    await api.put(`/parametros/${p.id}`, { ...p, ativo: !p.ativo })
    p.ativo = !p.ativo
  } catch (e) {
    error.value = 'Failed to update: ' + (e.response?.data ?? e.message)
  }
}

const confirmingDelete = ref(null)

async function remove(id) {
  error.value = ''
  try {
    await api.delete(`/parametros/${id}`)
    confirmingDelete.value = null
    await loadParameters()
  } catch (e) {
    error.value = 'Failed to delete: ' + (e.response?.data ?? e.message)
  }
}

onMounted(async () => {
  await loadTypes()
  await loadParameters()
})
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="bg-linear-to-r from-slate-900 via-indigo-950 to-slate-950 rounded-2xl p-6 sm:p-8 text-white shadow-xl">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div class="space-y-2">
          <h2 class="text-2xl font-bold tracking-tight">Classification Rules</h2>
          <p class="text-slate-300 text-sm leading-relaxed max-w-xl">
            Manage the parameters that guide the AI in ticket classification.
            Categories, priorities, departments, questions, complaints, sentiments, and tags.
          </p>
        </div>
        <button @click="openNew()" class="bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-2.5 rounded-xl font-semibold text-sm transition shadow-lg flex items-center gap-2 cursor-pointer">
          <i class="fa-solid fa-plus"></i>
          New Parameter
        </button>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="bg-rose-50 border border-rose-200 text-rose-700 px-5 py-4 rounded-xl flex items-center gap-3">
      <i class="fa-solid fa-triangle-exclamation text-rose-500"></i>
      <span class="text-sm font-medium">{{ error }}</span>
      <button @click="error = ''" class="ml-auto text-rose-400 hover:text-rose-600 cursor-pointer"><i class="fa-solid fa-xmark"></i></button>
    </div>

    <!-- Type Filter Chips -->
    <div class="flex flex-wrap gap-2">
      <button
        @click="selectedType = ''"
        :class="!selectedType ? 'bg-slate-900 text-white' : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'"
        class="px-4 py-2 rounded-lg text-xs font-semibold transition cursor-pointer flex items-center gap-2"
      >
        <i class="fa-solid fa-layer-group"></i>
        All
        <span class="bg-white/20 text-[10px] px-1.5 py-0.5 rounded-full" :class="!selectedType ? 'bg-white/20' : 'bg-slate-100'">{{ parameters.length }}</span>
      </button>
      <button
        v-for="t in Object.keys(typeLabels)" :key="t"
        @click="selectedType = selectedType === t ? '' : t"
        :class="selectedType === t ? 'bg-slate-900 text-white' : `border ${typeColors[t] || 'bg-white text-slate-600 border-slate-200'}`"
        class="px-3 py-2 rounded-lg text-xs font-semibold transition cursor-pointer flex items-center gap-1.5"
      >
        <i :class="`fa-solid ${typeIcons[t] || 'fa-circle'}`"></i>
        {{ typeLabels[t] }}
        <span v-if="countByType[t]" class="text-[10px] px-1.5 py-0.5 rounded-full" :class="selectedType === t ? 'bg-white/20' : 'bg-black/5'">{{ countByType[t] }}</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="text-center py-12">
      <div class="w-8 h-8 border-3 border-slate-200 border-t-indigo-500 rounded-full animate-spin mx-auto mb-3"></div>
      <p class="text-sm text-slate-400">Loading parameters...</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="filteredParameters.length === 0" class="bg-white rounded-2xl border border-slate-200 p-12 text-center shadow-sm">
      <i class="fa-solid fa-sliders text-4xl text-slate-300 mb-3 block"></i>
      <p class="text-slate-500 text-sm">
        {{ selectedType ? `No parameters of type "${typeLabels[selectedType]}" registered.` : 'No parameters registered.' }}
      </p>
      <button @click="openNew(selectedType)" class="mt-4 bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2 rounded-lg text-sm font-medium cursor-pointer inline-flex items-center gap-2 transition">
        <i class="fa-solid fa-plus"></i>
        Add
      </button>
    </div>

    <!-- Parameters Table -->
    <div v-else class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-100 text-xs font-bold text-slate-400 uppercase tracking-wider">
              <th class="p-4">Type</th>
              <th class="p-4">Term</th>
              <th class="p-4">Target</th>
              <th class="p-4 text-center">Status</th>
              <th class="p-4 text-center">Actions</th>
            </tr>
          </thead>
          <tbody class="text-sm divide-y divide-slate-100">
            <tr v-for="p in filteredParameters" :key="p.id" class="hover:bg-slate-50/50 transition duration-150" :class="{ 'opacity-50': !p.ativo }">
              <td class="p-4">
                <span :class="`border text-xs font-semibold px-2.5 py-1 rounded-full inline-flex items-center gap-1.5 ${typeColors[p.tipo] || 'bg-slate-50 text-slate-600 border-slate-200'}`">
                  <i :class="`fa-solid ${typeIcons[p.tipo] || 'fa-circle'} text-[10px]`"></i>
                  {{ typeLabels[p.tipo] || p.tipo }}
                </span>
              </td>
              <td class="p-4 text-slate-900 font-medium">{{ p.termo }}</td>
              <td class="p-4 text-slate-500">{{ p.alvo || '—' }}</td>
              <td class="p-4 text-center">
                <button @click="toggleActive(p)" class="cursor-pointer" :title="p.ativo ? 'Deactivate' : 'Activate'">
                  <span v-if="p.ativo" class="bg-emerald-50 text-emerald-700 border border-emerald-200 text-xs font-semibold px-2.5 py-1 rounded-full inline-flex items-center gap-1">
                    <i class="fa-solid fa-circle-check text-[10px]"></i> Active
                  </span>
                  <span v-else class="bg-slate-100 text-slate-500 border border-slate-200 text-xs font-semibold px-2.5 py-1 rounded-full inline-flex items-center gap-1">
                    <i class="fa-solid fa-circle-xmark text-[10px]"></i> Inactive
                  </span>
                </button>
              </td>
              <td class="p-4 text-center">
                <div class="flex items-center justify-center gap-1">
                  <button @click="openEdit(p)" class="bg-slate-100 hover:bg-slate-200 text-slate-600 p-2 rounded-lg transition cursor-pointer" title="Edit">
                    <i class="fa-solid fa-pen-to-square text-xs"></i>
                  </button>
                  <button v-if="confirmingDelete !== p.id" @click="confirmingDelete = p.id" class="bg-slate-100 hover:bg-rose-100 text-slate-600 hover:text-rose-600 p-2 rounded-lg transition cursor-pointer" title="Delete">
                    <i class="fa-solid fa-trash text-xs"></i>
                  </button>
                  <button v-else @click="remove(p.id)" class="bg-rose-500 hover:bg-rose-600 text-white p-2 rounded-lg transition cursor-pointer text-xs font-semibold px-3">
                    Confirm
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="closeForm">
      <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-md w-full">
        <div class="border-b border-slate-200 p-5">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-indigo-100 rounded-xl flex items-center justify-center">
              <i class="fa-solid fa-sliders text-indigo-600 text-lg"></i>
            </div>
            <div>
              <span class="text-[10px] uppercase font-bold text-indigo-600 tracking-widest block">{{ editingId ? 'Edit' : 'New' }}</span>
              <h3 class="text-lg font-bold text-slate-900">Classification Parameter</h3>
            </div>
          </div>
        </div>

        <form @submit.prevent="save" class="p-6 space-y-4">
          <!-- Type -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">Type</label>
            <select v-model="form.tipo" class="w-full border border-slate-200 rounded-xl px-4 py-2.5 text-sm text-slate-800 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500">
              <option v-for="t in Object.keys(typeLabels)" :key="t" :value="t">{{ typeLabels[t] }}</option>
            </select>
          </div>

          <!-- Term -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">Term</label>
            <input v-model="form.termo" type="text" placeholder="E.g.: Bug, Finance, login_failed..."
              class="w-full border border-slate-200 rounded-xl px-4 py-2.5 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500" required>
          </div>

          <!-- Target -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">Target <span class="text-slate-400 font-normal">(optional)</span></label>
            <input v-model="form.alvo" type="text" placeholder="E.g.: Target department, action..."
              class="w-full border border-slate-200 rounded-xl px-4 py-2.5 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500">
          </div>

          <!-- Active -->
          <div class="flex items-center gap-3">
            <button type="button" @click="form.ativo = !form.ativo"
              :class="form.ativo ? 'bg-indigo-600' : 'bg-slate-300'"
              class="relative w-10 h-5.5 rounded-full transition cursor-pointer">
              <span :class="form.ativo ? 'translate-x-5' : 'translate-x-0.5'" class="absolute top-0.5 w-4.5 h-4.5 bg-white rounded-full shadow transition-transform"></span>
            </button>
            <span class="text-sm text-slate-700 font-medium">{{ form.ativo ? 'Active' : 'Inactive' }}</span>
          </div>

          <!-- Actions -->
          <div class="flex gap-2 pt-2">
            <button type="submit" :disabled="saving || !form.termo.trim()"
              class="grow bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-4 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center justify-center gap-2">
              <i v-if="saving" class="fa-solid fa-spinner animate-spin"></i>
              <i v-else class="fa-solid fa-check"></i>
              {{ editingId ? 'Save' : 'Create' }}
            </button>
            <button type="button" @click="closeForm"
              class="border border-slate-200 hover:bg-slate-50 text-slate-600 px-5 py-2.5 rounded-xl text-sm font-medium transition cursor-pointer">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>

  </div>
</template>
