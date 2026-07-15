import { ref } from 'vue'
import api from '../api'

export function useTicketClassifier(progressHook) {
  const view = ref('upload') // 'upload' | 'lotes' | 'detalhe'
  const lotes = ref([])
  const batchAtual = ref(null)
  const erro = ref('')
  const reprocessando = ref(false)
  const reprocessandoTudo = ref(false)

  async function carregarLotes() {
    try {
      const response = await api.get('/tickets/batches')
      lotes.value = response.data ?? []
      view.value = 'lotes'
    } catch {
      erro.value = 'Failed to load batches.'
    }
  }

  async function abrirDetalhe(id) {
    try {
      const response = await api.get(`/tickets/batches/${id}`)
      batchAtual.value = response.data
      view.value = 'detalhe'
    } catch {
      erro.value = 'Failed to load batch details.'
    }
  }

  async function verificarDuplicata(nomeArquivo) {
    try {
      const { data } = await api.get('/tickets/verificar-duplicata', { params: { nomeArquivo } })
      return data
    } catch {
      return { duplicado: false }
    }
  }

  async function enviarArquivo(arquivo, jobId, sobrescrever = false) {
    if (!arquivo) return
    erro.value = ''
    progressHook.iniciarPolling(jobId)

    try {
      const form = new FormData()
      form.append('file', arquivo)
      form.append('jobId', jobId)
      form.append('sobrescrever', sobrescrever)

      const { data } = await api.post('/tickets/upload', form)
      await abrirDetalhe(data.batchId)
    } catch (e) {
      erro.value = e?.response?.data?.erro || e?.message || 'Failed to process CSV.'
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
      erro.value = e?.response?.data?.erro || 'Failed to reprocess.'
    } finally {
      reprocessando.value = false
    }
  }

  async function reprocessarTudo() {
    if (!batchAtual.value) return
    reprocessandoTudo.value = true
    erro.value = ''
    const jobId = self.crypto?.randomUUID?.() ?? ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c => (c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (c / 4)))).toString(16))
    progressHook.iniciarPolling(jobId)
    try {
      const { data } = await api.post(
        `/tickets/batches/${batchAtual.value.batchId}/reprocessar-tudo`,
        null,
        { params: { jobId } }
      )
      batchAtual.value = data
    } catch (e) {
      erro.value = e?.response?.data?.erro || 'Failed to reprocess.'
    } finally {
      reprocessandoTudo.value = false
      progressHook.pararPolling()
    }
  }

  function baixarExportacao(colunas) {
    if (batchAtual.value) {
      let url = `${api.defaults.baseURL}/tickets/batches/${batchAtual.value.batchId}/export`
      if (colunas?.length) url += `?colunas=${colunas.join(',')}`
      window.open(url, '_blank')
    }
  }

  async function atualizarTicket(ticketId, dto) {
    try {
      const { data } = await api.patch(`/tickets/${ticketId}`, dto)
      if (batchAtual.value?.tickets) {
        const idx = batchAtual.value.tickets.findIndex(t => t.id === ticketId)
        if (idx >= 0) batchAtual.value.tickets[idx] = data
      }
      return data
    } catch (e) {
      erro.value = e?.response?.data?.erro || 'Failed to update ticket.'
      return null
    }
  }

  async function obterSimilares(ticketId) {
    try {
      const { data } = await api.get(`/tickets/${ticketId}/similares`)
      return data
    } catch (e) {
      erro.value = e?.response?.data?.erro || 'Failed to search similar tickets.'
      return []
    }
  }

  return {
    view,
    lotes,
    batchAtual,
    erro,
    reprocessando,
    reprocessandoTudo,
    carregarLotes,
    abrirDetalhe,
    verificarDuplicata,
    enviarArquivo,
    reprocessarLote,
    reprocessarTudo,
    baixarExportacao,
    atualizarTicket,
    obterSimilares,
  }
}
