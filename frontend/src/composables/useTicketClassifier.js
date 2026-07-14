import { ref } from 'vue'
import api from '../api'

export function useTicketClassifier(progressHook) {
  const view = ref('upload') // 'upload' | 'lotes' | 'detalhe'
  const lotes = ref([])
  const batchAtual = ref(null)
  const erro = ref('')
  const reprocessando = ref(false)

  async function carregarLotes() {
    try {
      const response = await api.get('/tickets/batches')
      lotes.value = response.data ?? []
      view.value = 'lotes'
    } catch {
      erro.value = 'Falha ao carregar lotes.'
    }
  }

  async function abrirDetalhe(id) {
    try {
      const response = await api.get(`/tickets/batches/${id}`)
      batchAtual.value = response.data
      view.value = 'detalhe'
    } catch {
      erro.value = 'Falha ao carregar detalhes do lote.'
    }
  }

  async function enviarArquivo(arquivo, jobId) {
    if (!arquivo) return
    erro.value = ''
    progressHook.iniciarPolling(jobId)

    try {
      const form = new FormData()
      form.append('file', arquivo)
      form.append('jobId', jobId)
      
      const { data } = await api.post('/tickets/upload', form)
      await abrirDetalhe(data.batchId)
    } catch (e) {
      erro.value = e?.response?.data?.erro || e?.message || 'Falha ao processar o CSV.'
    } finally {
      progressHook.pararPolling()
    }
  }

  async function reprocessarLote() {
    if (!batchAtual.value) return
    reprocessando.value = true
    erro.value = ''
    try {
      const { data } = await api.post(`/tickets/batches/${batchAtual.value.batchId}/reprocessar`)
      batchAtual.value = data
    } catch (e) {
      erro.value = e?.response?.data?.erro || 'Falha ao reprocessar.'
    } finally {
      reprocessando.value = false
    }
  }

  function baixarExportacao() {
    if (batchAtual.value) {
      window.open(`${api.defaults.baseURL}/tickets/batches/${batchAtual.value.batchId}/export`, '_blank')
    }
  }

  return {
    view,
    lotes,
    batchAtual,
    erro,
    reprocessando,
    carregarLotes,
    abrirDetalhe,
    enviarArquivo,
    reprocessarLote,
    baixarExportacao,
  }
}