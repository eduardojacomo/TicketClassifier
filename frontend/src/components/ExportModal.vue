<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  batchId: { type: String, required: true }
})
const emit = defineEmits(['fechar', 'exportar'])

const colunas = [
  { key: 'id', label: 'ID', grupo: 'original' },
  { key: 'assunto', label: 'Assunto', grupo: 'original' },
  { key: 'descricao', label: 'Descrição', grupo: 'original' },
  { key: 'categoria', label: 'Categoria', grupo: 'classificacao' },
  { key: 'prioridade', label: 'Prioridade', grupo: 'classificacao' },
  { key: 'departamento', label: 'Departamento', grupo: 'classificacao' },
  { key: 'sentimento', label: 'Sentimento', grupo: 'classificacao' },
  { key: 'tags', label: 'Tags', grupo: 'classificacao' },
  { key: 'resumo', label: 'Resumo', grupo: 'classificacao' },
  { key: 'confianca', label: 'Confiança', grupo: 'classificacao' },
  { key: 'justificativa', label: 'Justificativa', grupo: 'classificacao' },
  { key: 'modificado', label: 'Modificado', grupo: 'auditoria' },
  { key: 'dataModificacao', label: 'Data Modificação', grupo: 'auditoria' },
]

const selecionadas = ref(new Set(colunas.map(c => c.key)))

const grupos = [
  { id: 'original', label: 'Dados Originais', ico: 'fa-file-lines', cor: 'text-slate-600' },
  { id: 'classificacao', label: 'Classificação IA', ico: 'fa-robot', cor: 'text-indigo-600' },
  { id: 'auditoria', label: 'Auditoria', ico: 'fa-clock-rotate-left', cor: 'text-amber-600' },
]

const totalSelecionadas = computed(() => selecionadas.value.size)

function toggle(key) {
  if (selecionadas.value.has(key)) selecionadas.value.delete(key)
  else selecionadas.value.add(key)
  selecionadas.value = new Set(selecionadas.value)
}

function toggleGrupo(grupoId) {
  const cols = colunas.filter(c => c.grupo === grupoId)
  const todasMarcadas = cols.every(c => selecionadas.value.has(c.key))
  cols.forEach(c => {
    if (todasMarcadas) selecionadas.value.delete(c.key)
    else selecionadas.value.add(c.key)
  })
  selecionadas.value = new Set(selecionadas.value)
}

function selecionarTodas() {
  selecionadas.value = new Set(colunas.map(c => c.key))
}

function limparTodas() {
  selecionadas.value = new Set()
}

function exportar() {
  const cols = colunas.filter(c => selecionadas.value.has(c.key)).map(c => c.key)
  emit('exportar', cols)
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('fechar')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-md w-full">
      <div class="border-b border-slate-200 p-5 flex items-center justify-between">
        <div>
          <span class="text-[10px] uppercase font-bold text-emerald-600 tracking-widest block">Exportar CSV</span>
          <h3 class="text-lg font-bold text-slate-900">Configurar colunas</h3>
        </div>
        <button @click="emit('fechar')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
          <i class="fa-solid fa-xmark text-lg"></i>
        </button>
      </div>

      <div class="p-6 space-y-5">
        <!-- Quick actions -->
        <div class="flex items-center justify-between">
          <span class="text-xs text-slate-500">
            <b class="text-slate-700">{{ totalSelecionadas }}</b> de {{ colunas.length }} colunas
          </span>
          <div class="flex gap-2">
            <button @click="selecionarTodas" class="text-xs text-indigo-600 hover:text-indigo-800 font-medium cursor-pointer">Todas</button>
            <span class="text-slate-300">|</span>
            <button @click="limparTodas" class="text-xs text-slate-500 hover:text-slate-700 font-medium cursor-pointer">Nenhuma</button>
          </div>
        </div>

        <!-- Column groups -->
        <div v-for="g in grupos" :key="g.id" class="space-y-2">
          <button @click="toggleGrupo(g.id)" class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider cursor-pointer hover:opacity-80 transition w-full">
            <i class="fa-solid" :class="[g.ico, g.cor]"></i>
            <span class="text-slate-500">{{ g.label }}</span>
            <span class="ml-auto text-[10px] text-slate-400 font-normal">
              {{ colunas.filter(c => c.grupo === g.id && selecionadas.has(c.key)).length }}/{{ colunas.filter(c => c.grupo === g.id).length }}
            </span>
          </button>
          <div class="grid grid-cols-2 gap-1.5">
            <label v-for="col in colunas.filter(c => c.grupo === g.id)" :key="col.key"
              :class="selecionadas.has(col.key) ? 'bg-indigo-50 border-indigo-200 text-indigo-800' : 'bg-slate-50 border-slate-200 text-slate-500'"
              class="flex items-center gap-2 px-3 py-2 rounded-lg border text-sm cursor-pointer transition hover:shadow-sm">
              <input type="checkbox" :checked="selecionadas.has(col.key)" @change="toggle(col.key)"
                class="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
              {{ col.label }}
            </label>
          </div>
        </div>
      </div>

      <div class="border-t border-slate-200 p-5 flex justify-end gap-3">
        <button @click="emit('fechar')" class="px-4 py-2.5 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50 transition cursor-pointer">
          Cancelar
        </button>
        <button @click="exportar" :disabled="totalSelecionadas === 0"
          class="bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center gap-2">
          <i class="fa-solid fa-file-export"></i>
          Exportar {{ totalSelecionadas }} coluna(s)
        </button>
      </div>
    </div>
  </div>
</template>
