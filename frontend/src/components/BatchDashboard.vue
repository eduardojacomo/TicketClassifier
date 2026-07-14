<script setup>
import { ref, computed, watch } from 'vue'
import { coresPrioridade } from '../utils/chartMappers'
import BarChart from './BarChart.vue'
import DoughnutChart from './DoughnutChart.vue'

const props = defineProps({
  batch: { type: Object, required: true },
  reprocessando: { type: Boolean, default: false }
})

const emit = defineEmits(['voltar', 'reprocessar', 'baixar'])

const CONF_MIN = 0.7
const tamanhoPagina = 10

const fCategoria = ref('')
const fPrioridade = ref('')
const fDepartamento = ref('')
const fBusca = ref('')
const fSoBaixaConfianca = ref(false)
const pagina = ref(1)

const stats = computed(() => props.batch?.estatisticas)
const extrairOpcoes = (dic) => Object.entries(dic || {}).filter(([, v]) => v > 0).map(([k]) => k)

const kpis = computed(() => {
  const s = stats.value; if (!s) return []
  const categoriasOrdenadas = Object.entries(s.porCategoria || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
  const topCat = categoriasOrdenadas[0]

  return [
    { rot: 'Total Processado', val: s.total, ico: 'fa-list', bg: 'bg-slate-100', iconColor: 'text-slate-600' },
    { rot: 'Criticos', val: s.porPrioridade?.['Crítica'] ?? 0, ico: 'fa-circle-exclamation', bg: 'bg-rose-50', iconColor: 'text-rose-600' },
    { rot: 'Falhas', val: s.falhas, ico: 'fa-rotate', bg: 'bg-amber-50', iconColor: 'text-amber-600' },
    { rot: 'Confianca Media', val: `${Math.round((s.confiancaMedia || 0) * 100)}%`, ico: 'fa-bullseye', bg: 'bg-indigo-50', iconColor: 'text-indigo-600' },
    { rot: topCat ? `Top: ${topCat[0]}` : 'Categoria', val: topCat ? topCat[1] : 0, ico: 'fa-trophy', bg: 'bg-violet-50', iconColor: 'text-violet-600' },
  ]
})

const ticketsFiltrados = computed(() => {
  const ts = props.batch?.tickets ?? []
  const buscaTermo = fBusca.value.trim().toLowerCase()

  return ts.filter(t =>
    (!fCategoria.value || t.categoria === fCategoria.value) &&
    (!fPrioridade.value || t.prioridade === fPrioridade.value) &&
    (!fDepartamento.value || t.departamento === fDepartamento.value) &&
    (!fSoBaixaConfianca.value || t.confianca < CONF_MIN) &&
    (!buscaTermo || `${t.assunto} ${t.descricao} ${t.resumo}`.toLowerCase().includes(buscaTermo))
  )
})

const totalPaginas = computed(() => Math.max(1, Math.ceil(ticketsFiltrados.value.length / tamanhoPagina)))
const ticketsPagina = computed(() => {
  const inicio = (pagina.value - 1) * tamanhoPagina
  return ticketsFiltrados.value.slice(inicio, inicio + tamanhoPagina)
})

watch([fCategoria, fPrioridade, fDepartamento, fBusca, fSoBaixaConfianca], () => { pagina.value = 1 })

function getPriorityStyle(p) {
  switch (p) {
    case 'Crítica': return 'bg-rose-100 text-rose-800 border-rose-200'
    case 'Alta': return 'bg-amber-100 text-amber-800 border-amber-200'
    case 'Média': return 'bg-blue-100 text-blue-800 border-blue-200'
    case 'Baixa': return 'bg-emerald-100 text-emerald-800 border-emerald-200'
    default: return 'bg-slate-100 text-slate-700 border-slate-200'
  }
}

function getConfStyle(c) {
  if (c >= 0.8) return 'text-emerald-700 bg-emerald-50'
  if (c >= 0.6) return 'text-blue-700 bg-blue-50'
  if (c >= 0.4) return 'text-amber-700 bg-amber-50'
  return 'text-rose-700 bg-rose-50'
}
</script>

<template>
  <div class="space-y-6">

    <!-- Top Bar -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div class="flex items-center gap-3">
        <button @click="emit('voltar')" class="bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 p-2.5 rounded-xl transition cursor-pointer">
          <i class="fa-solid fa-arrow-left"></i>
        </button>
        <div>
          <h2 class="text-2xl font-bold tracking-tight text-slate-900">{{ batch.nomeArquivo }}</h2>
          <p class="text-sm text-slate-500">Resultados da classificacao por IA</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <button v-if="stats?.falhas > 0" :disabled="reprocessando" @click="emit('reprocessar')"
                class="bg-amber-500 hover:bg-amber-400 disabled:opacity-50 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
          <i class="fa-solid fa-rotate" :class="{ 'animate-spin': reprocessando }"></i>
          {{ reprocessando ? 'Reprocessando...' : `Reprocessar ${stats.falhas} falha(s)` }}
        </button>
        <button @click="emit('baixar')" class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
          <i class="fa-solid fa-file-export"></i>
          Exportar CSV
        </button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
      <div v-for="k in kpis" :key="k.rot" class="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm flex items-center space-x-4">
        <div :class="[k.bg, k.iconColor]" class="w-12 h-12 rounded-xl flex items-center justify-center text-xl shrink-0">
          <i class="fa-solid" :class="k.ico"></i>
        </div>
        <div class="min-w-0">
          <span class="block text-[10px] text-slate-400 font-semibold uppercase tracking-wider truncate">{{ k.rot }}</span>
          <span class="text-2xl font-bold text-slate-950">{{ k.val }}</span>
        </div>
      </div>
    </div>

    <!-- Charts -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <BarChart titulo="POR CATEGORIA" :dados="stats?.porCategoria" />
      <BarChart titulo="POR DEPARTAMENTO" :dados="stats?.porDepartamento" />
      <DoughnutChart titulo="POR PRIORIDADE" :dados="stats?.porPrioridade" :cores="coresPrioridade" />
    </div>

    <!-- Data Table -->
    <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h3 class="text-lg font-bold text-slate-900">Resultados Individuais</h3>
          <p class="text-xs text-slate-500">Navegacao e filtragem por tickets classificados pela IA.</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-2">
        <div class="relative max-w-xs">
          <i class="fa-solid fa-search absolute left-3 top-2.5 text-slate-400 text-xs"></i>
          <input v-model="fBusca" type="text" placeholder="Buscar..." class="pl-9 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 w-full" />
        </div>
        <select v-model="fCategoria" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Categoria: todas</option>
          <option v-for="o in extrairOpcoes(stats?.porCategoria)" :key="o">{{ o }}</option>
        </select>
        <select v-model="fPrioridade" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Prioridade: todas</option>
          <option v-for="o in extrairOpcoes(stats?.porPrioridade)" :key="o">{{ o }}</option>
        </select>
        <select v-model="fDepartamento" class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer">
          <option value="">Departamento: todos</option>
          <option v-for="o in extrairOpcoes(stats?.porDepartamento)" :key="o">{{ o }}</option>
        </select>
        <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer bg-slate-50 border border-slate-200 px-3 py-2 rounded-xl hover:bg-slate-100 transition">
          <input type="checkbox" v-model="fSoBaixaConfianca" class="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
          So &lt; 70%
        </label>
      </div>

      <!-- Table -->
      <div class="overflow-x-auto rounded-xl border border-slate-100">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-100 text-xs font-bold text-slate-400 uppercase tracking-wider">
              <th class="p-4">ID</th>
              <th class="p-4">Assunto</th>
              <th class="p-4">Categoria</th>
              <th class="p-4">Prioridade</th>
              <th class="p-4">Depto</th>
              <th class="p-4">Resumo</th>
              <th class="p-4 text-center">Confianca</th>
            </tr>
          </thead>
          <tbody class="text-sm divide-y divide-slate-100">
            <tr v-for="t in ticketsPagina" :key="t.id" class="hover:bg-slate-50/50 transition duration-150">
              <td class="p-4 font-mono font-bold text-slate-400 text-xs">{{ t.externalId }}</td>
              <td class="p-4 text-slate-900 font-medium max-w-[180px] truncate">{{ t.assunto }}</td>
              <td class="p-4">
                <span class="bg-indigo-50 text-indigo-700 border border-indigo-100 px-2.5 py-1 rounded-full text-xs font-semibold inline-flex items-center gap-1">
                  {{ t.categoria }}
                </span>
              </td>
              <td class="p-4">
                <span :class="getPriorityStyle(t.prioridade)" class="px-2.5 py-1 rounded-full text-xs font-bold uppercase border inline-flex items-center gap-1">
                  <i class="fa-solid fa-circle text-[5px]"></i>
                  {{ t.prioridade }}
                </span>
              </td>
              <td class="p-4 text-slate-600">{{ t.departamento }}</td>
              <td class="p-4 text-xs text-slate-500 max-w-[250px] truncate" :title="t.justificativa">{{ t.resumo }}</td>
              <td class="p-4 text-center">
                <span :class="getConfStyle(t.confianca)" class="px-2.5 py-1 rounded-full text-xs font-bold inline-flex items-center gap-1">
                  <i v-if="t.confianca < CONF_MIN" class="fa-solid fa-triangle-exclamation text-[10px]"></i>
                  {{ Math.round(t.confianca * 100) }}%
                </span>
              </td>
            </tr>
            <tr v-if="!ticketsFiltrados.length">
              <td colspan="7" class="p-8 text-center text-slate-400">
                <i class="fa-solid fa-filter-circle-xmark text-2xl text-slate-300 block mb-2"></i>
                Nenhum ticket encontrado para o filtro aplicado.
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="flex items-center justify-between pt-4 border-t border-slate-100 text-xs">
        <span class="text-slate-400">
          Exibindo <b class="text-slate-700">{{ ticketsFiltrados.length ? (pagina - 1) * tamanhoPagina + 1 : 0 }}-{{ Math.min(pagina * tamanhoPagina, ticketsFiltrados.length) }}</b> de <b class="text-slate-700">{{ ticketsFiltrados.length }}</b>
        </span>
        <div class="flex items-center space-x-2">
          <button :disabled="pagina <= 1" @click="pagina--" class="p-2 border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 transition cursor-pointer">
            <i class="fa-solid fa-chevron-left"></i>
          </button>
          <span class="font-bold text-slate-800">{{ pagina }} / {{ totalPaginas }}</span>
          <button :disabled="pagina >= totalPaginas" @click="pagina++" class="p-2 border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 transition cursor-pointer">
            <i class="fa-solid fa-chevron-right"></i>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
