<script setup>
import { ref } from 'vue'
import { useTicketProgress } from './composables/useTicketProgress'
import { useTicketClassifier } from './composables/useTicketClassifier'
import { formatarData } from './utils/chartMappers'
import BatchDashboard from './components/BatchDashboard.vue'
import DuplicataModal from './components/DuplicataModal.vue'

const progressHook = useTicketProgress()
const classifier = useTicketClassifier(progressHook)

const arquivoLocal = ref(null)
const dragOver = ref(false)

const duplicataInfo = ref(null)

function tratarSelecaoArquivo(e) {
  arquivoLocal.value = e.target.files?.[0] ?? null
  classifier.erro.value = ''
}

function handleDrop(e) {
  dragOver.value = false
  const file = e.dataTransfer.files?.[0]
  if (file && file.name.endsWith('.csv')) {
    arquivoLocal.value = file
    classifier.erro.value = ''
  }
}

async function dispararUpload() {
  if (!arquivoLocal.value) return

  const { duplicado, batch } = await classifier.verificarDuplicata(arquivoLocal.value.name)
  if (duplicado && batch) {
    duplicataInfo.value = batch
    return
  }

  enviarComOpcao(false)
}

function enviarComOpcao(sobrescrever) {
  duplicataInfo.value = null
  if (!arquivoLocal.value) return
  const jobId = crypto.randomUUID()
  classifier.enviarArquivo(arquivoLocal.value, jobId, sobrescrever).then(() => {
    arquivoLocal.value = null
  })
}
</script>

<template>
  <div class="bg-slate-50 text-slate-800 antialiased min-h-screen flex flex-col font-sans">

    <!-- Duplicata Modal -->
    <DuplicataModal
      v-if="duplicataInfo"
      :nome-arquivo="arquivoLocal?.name ?? ''"
      :batch-existente="duplicataInfo"
      @sobrescrever="enviarComOpcao(true)"
      @novo="enviarComOpcao(false)"
      @cancelar="duplicataInfo = null"
    />

    <!-- Processing Modal Overlay -->
    <div v-if="progressHook.processando.value" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-xl w-full overflow-hidden">
        <div class="bg-slate-900 text-white p-5">
          <div class="flex items-center gap-3">
            <div class="w-5 h-5 border-3 border-slate-600 border-t-indigo-400 rounded-full animate-spin"></div>
            <div>
              <span class="text-[10px] uppercase font-bold text-indigo-400 tracking-widest block">Pipeline Ativo</span>
              <h3 class="text-lg font-bold">Classificando tickets...</h3>
            </div>
            <span class="ml-auto text-sm font-mono font-semibold text-indigo-400 bg-indigo-950 px-3 py-1 rounded-full">
              {{ progressHook.pctConcluido.value }}%
            </span>
          </div>
        </div>

        <div class="p-6 space-y-5">
          <!-- Progress Bar -->
          <div class="w-full bg-slate-100 rounded-full h-3 overflow-hidden">
            <div class="bg-linear-to-r from-indigo-500 to-violet-600 h-full rounded-full transition-all duration-300"
                 :style="{ width: progressHook.pctConcluido.value + '%' }"></div>
          </div>

          <p class="text-sm text-slate-600">
            <b class="text-slate-900">{{ progressHook.prog.value.processados }}/{{ progressHook.prog.value.total || '...' }}</b> tickets processados
            <span v-if="progressHook.prog.value.totalLotes" class="text-slate-400"> · lote {{ progressHook.prog.value.lotesConcluidos }}/{{ progressHook.prog.value.totalLotes }}</span>
            <span v-if="progressHook.tempoRestanteEstima.value" class="text-slate-400"> · {{ progressHook.tempoRestanteEstima.value }}</span>
          </p>

          <!-- Mini KPIs -->
          <div class="grid grid-cols-4 gap-3">
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 text-center">
              <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">Total</span>
              <span class="text-xl font-bold text-slate-800">{{ progressHook.prog.value.total }}</span>
            </div>
            <div class="bg-amber-50/50 p-3 rounded-xl border border-amber-100 text-center">
              <span class="block text-[10px] text-amber-500 uppercase font-bold tracking-wider">Pendente</span>
              <span class="text-xl font-bold text-amber-700">{{ Math.max(0, progressHook.prog.value.total - progressHook.prog.value.processados) }}</span>
            </div>
            <div class="bg-emerald-50/50 p-3 rounded-xl border border-emerald-100 text-center">
              <span class="block text-[10px] text-emerald-500 uppercase font-bold tracking-wider">OK</span>
              <span class="text-xl font-bold text-emerald-700">{{ progressHook.prog.value.ok }}</span>
            </div>
            <div class="bg-rose-50/50 p-3 rounded-xl border border-rose-100 text-center">
              <span class="block text-[10px] text-rose-500 uppercase font-bold tracking-wider">Falhas</span>
              <span class="text-xl font-bold text-rose-700">{{ progressHook.prog.value.falhas }}</span>
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
              <p class="text-xs text-slate-500">Classificacao inteligente de chamados</p>
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
              @click="classifier.carregarLotes"
              :class="(classifier.view.value === 'lotes' || classifier.view.value === 'detalhe') ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="px-4 py-1.5 rounded-md text-sm font-medium transition duration-150 flex items-center gap-2 cursor-pointer"
            >
              <i class="fa-solid fa-history"></i>
              Lotes
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
              @click="classifier.carregarLotes"
              :class="(classifier.view.value === 'lotes' || classifier.view.value === 'detalhe') ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600'"
              class="p-2.5 rounded-lg text-sm cursor-pointer"
            >
              <i class="fa-solid fa-history"></i>
            </button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="grow max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">

      <!-- Error Toast -->
      <div v-if="classifier.erro.value" class="mb-6 bg-rose-50 border border-rose-200 text-rose-700 px-5 py-4 rounded-xl flex items-center gap-3">
        <i class="fa-solid fa-triangle-exclamation text-rose-500"></i>
        <span class="text-sm font-medium">{{ classifier.erro.value }}</span>
        <button @click="classifier.erro.value = ''" class="ml-auto text-rose-400 hover:text-rose-600 cursor-pointer"><i class="fa-solid fa-xmark"></i></button>
      </div>

      <!-- UPLOAD VIEW -->
      <div v-if="classifier.view.value === 'upload'" class="space-y-6">

        <!-- Hero Banner -->
        <div class="bg-linear-to-r from-slate-900 via-indigo-950 to-slate-950 rounded-2xl p-6 sm:p-8 text-white shadow-xl">
          <div class="space-y-3 max-w-2xl">
            <h2 class="text-2xl font-bold tracking-tight">Classificacao de Tickets por IA</h2>
            <p class="text-slate-300 text-sm leading-relaxed">
              Envie sua planilha de suporte em formato CSV. O sistema classifica automaticamente categoria, prioridade e departamento usando inteligencia artificial, processando em lotes para evitar gargalos.
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
              :class="dragOver ? 'border-indigo-500 bg-indigo-50/50' : arquivoLocal ? 'border-emerald-400 bg-emerald-50/30' : 'border-slate-300 bg-white hover:border-slate-400'"
              class="border-2 border-dashed rounded-2xl p-12 text-center transition-all duration-150 flex flex-col items-center justify-center min-h-[320px]"
            >
              <input type="file" id="csv-upload" class="hidden" accept=".csv" @change="tratarSelecaoArquivo">

              <!-- No file selected -->
              <label v-if="!arquivoLocal" for="csv-upload" class="cursor-pointer flex flex-col items-center">
                <div class="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-4 hover:scale-105 transition-transform">
                  <i class="fa-solid fa-file-csv text-3xl text-indigo-600"></i>
                </div>
                <h3 class="text-lg font-bold text-slate-900">Arraste seu arquivo CSV ou clique para navegar</h3>
                <p class="text-xs text-slate-500 mt-2 max-w-xs mx-auto">
                  Colunas reconhecidas: subject/assunto, description/descricao/body, id (opcional)
                </p>
              </label>

              <!-- File selected -->
              <div v-else class="flex flex-col items-center">
                <div class="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mb-4">
                  <i class="fa-solid fa-file-circle-check text-3xl text-emerald-600"></i>
                </div>
                <h3 class="text-lg font-bold text-slate-900">{{ arquivoLocal.name }}</h3>
                <p class="text-xs text-slate-500 mt-1">{{ (arquivoLocal.size / 1024).toFixed(1) }} KB</p>

                <div class="flex gap-3 mt-6">
                  <button
                    @click="dispararUpload"
                    :disabled="progressHook.processando.value"
                    class="bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-6 py-3 rounded-xl font-semibold text-sm transition shadow-lg shadow-indigo-100 flex items-center gap-2 cursor-pointer"
                  >
                    <i class="fa-solid fa-wand-magic-sparkles"></i>
                    Classificar tickets
                  </button>
                  <label for="csv-upload" class="border border-slate-300 hover:bg-slate-50 text-slate-700 px-5 py-3 rounded-xl font-semibold text-sm transition cursor-pointer flex items-center gap-2">
                    <i class="fa-solid fa-arrows-rotate"></i>
                    Trocar arquivo
                  </label>
                </div>
              </div>

              <!-- Format hints -->
              <div v-if="!arquivoLocal" class="mt-8 pt-6 border-t border-slate-100 w-full max-w-md flex justify-around text-xs text-slate-400">
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> ID do Ticket</span>
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> Descricao</span>
                <span class="flex items-center gap-1.5"><i class="fa-solid fa-check text-emerald-500"></i> Assunto</span>
              </div>
            </div>
          </div>

          <!-- Architecture Info Panel -->
          <div class="space-y-6">
            <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-5">
              <h3 class="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                <i class="fa-solid fa-network-wired text-indigo-600"></i>
                Arquitetura do Sistema
              </h3>

              <div class="space-y-3.5 text-xs text-slate-600">
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">1</span>
                  <div>
                    <p class="font-semibold text-slate-800">Upload CSV (.NET API)</p>
                    <p class="text-slate-500">Arquivo parseado e persistido no PostgreSQL via EF Core.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">2</span>
                  <div>
                    <p class="font-semibold text-slate-800">Processamento em Lote</p>
                    <p class="text-slate-500">Tickets divididos em lotes paralelos com throttling.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">3</span>
                  <div>
                    <p class="font-semibold text-slate-800">Classificacao por LLM</p>
                    <p class="text-slate-500">Gemini, Claude, Llama local ou investigação mock.</p>
                  </div>
                </div>
                <div class="flex items-start gap-2.5">
                  <span class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-[10px] shrink-0 mt-0.5">4</span>
                  <div>
                    <p class="font-semibold text-slate-800">Dashboard de Resultados</p>
                    <p class="text-slate-500">Visualizacao interativa com Chart.js e filtros.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- BATCH LIST VIEW -->
      <div v-else-if="classifier.view.value === 'lotes'" class="space-y-6">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-2xl font-bold tracking-tight text-slate-900">Lotes Processados</h2>
            <p class="text-sm text-slate-500">Historico de todas as planilhas processadas.</p>
          </div>
          <button @click="classifier.view.value = 'upload'" class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2.5 rounded-xl font-medium text-sm transition flex items-center gap-2 shadow-md cursor-pointer">
            <i class="fa-solid fa-plus"></i>
            Novo Upload
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
          <p v-if="!classifier.lotes.value.length" class="text-center text-slate-400 py-12">
            <i class="fa-solid fa-inbox text-4xl text-slate-300 block mb-3"></i>
            Nenhum lote processado ainda. Envie um CSV para comecar.
          </p>

          <div v-else class="overflow-x-auto rounded-xl border border-slate-100">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-100 text-xs font-bold text-slate-400 uppercase tracking-wider">
                  <th class="p-4">Arquivo</th>
                  <th class="p-4 text-center">Tickets</th>
                  <th class="p-4">Data</th>
                  <th class="p-4 text-center">Acoes</th>
                </tr>
              </thead>
              <tbody class="text-sm divide-y divide-slate-100">
                <tr v-for="l in classifier.lotes.value" :key="l.batchId" class="hover:bg-slate-50/50 transition duration-150">
                  <td class="p-4 text-slate-900 font-semibold flex items-center gap-2">
                    <i class="fa-solid fa-file-csv text-indigo-600"></i>
                    {{ l.nomeArquivo }}
                  </td>
                  <td class="p-4 text-center">
                    <span class="bg-slate-100 text-slate-700 px-2.5 py-1 rounded-full text-xs font-bold">{{ l.total }}</span>
                  </td>
                  <td class="p-4 text-slate-500">{{ formatarData(l.dataCriacao) }}</td>
                  <td class="p-4 text-center">
                    <button @click="classifier.abrirDetalhe(l.batchId)" class="bg-slate-100 hover:bg-slate-200 text-slate-700 px-4 py-2 rounded-lg text-xs font-semibold transition cursor-pointer flex items-center gap-1.5 mx-auto">
                      <i class="fa-solid fa-chart-line text-indigo-600"></i>
                      Abrir
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- BATCH DETAIL VIEW -->
      <BatchDashboard
        v-else-if="classifier.view.value === 'detalhe' && classifier.batchAtual.value"
        :batch="classifier.batchAtual.value"
        :reprocessando="classifier.reprocessando.value"
        :reprocessando-tudo="classifier.reprocessandoTudo.value"
        @voltar="classifier.carregarLotes"
        @reprocessar="classifier.reprocessarLote"
        @reprocessar-tudo="classifier.reprocessarTudo"
        @baixar="(cols) => classifier.baixarExportacao(cols)"
        @atualizar-ticket="(id, dto, done) => classifier.atualizarTicket(id, dto).finally(done)"
        @obter-similares="(id, cb) => classifier.obterSimilares(id).then(cb)"
      />
    </main>

    <!-- Footer -->
    <footer class="bg-white border-t border-slate-200 py-6 mt-auto">
      <div class="max-w-7xl mx-auto px-4 text-center text-xs text-slate-400 space-y-1">
        <p>HelpDesk AI Ticket Classifier &mdash; .NET 8, Vue 3, PostgreSQL, Chart.js</p>
      </div>
    </footer>
  </div>
</template>
