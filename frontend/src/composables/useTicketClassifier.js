import { ref } from 'vue'
import api from '../api'

export function useTicketClassifier(progressHook) {
  const view = ref('upload') // 'upload' | 'batches' | 'detail'
  const batches = ref([])
  const currentBatch = ref(null)
  const error = ref('')
  const reprocessing = ref(false)
  const reprocessingAll = ref(false)

  async function loadBatches() {
    try {
      const response = await api.get('/tickets/batches')
      batches.value = response.data ?? []
      view.value = 'batches'
    } catch {
      error.value = 'Failed to load batches.'
    }
  }

  async function openDetail(id) {
    try {
      const response = await api.get(`/tickets/batches/${id}`)
      currentBatch.value = response.data
      view.value = 'detail'
    } catch {
      error.value = 'Failed to load batch details.'
    }
  }

  async function checkDuplicate(fileName) {
    try {
      const { data } = await api.get('/tickets/check-duplicate', { params: { fileName } })
      return data
    } catch {
      return { duplicate: false }
    }
  }

  async function uploadFile(file, jobId, overwrite = false) {
    if (!file) return
    error.value = ''
    progressHook.startPolling(jobId)

    try {
      const form = new FormData()
      form.append('file', file)
      form.append('jobId', jobId)
      form.append('overwrite', overwrite)

      const { data } = await api.post('/tickets/upload', form)
      await openDetail(data.batchId)
    } catch (e) {
      error.value = e?.response?.data?.error || e?.message || 'Failed to process CSV.'
    } finally {
      progressHook.stopPolling()
    }
  }

  async function reprocessBatch() {
    if (!currentBatch.value) return
    reprocessing.value = true
    error.value = ''
    try {
      const { data } = await api.post(`/tickets/batches/${currentBatch.value.batchId}/reprocess`)
      currentBatch.value = data
    } catch (e) {
      error.value = e?.response?.data?.error || 'Failed to reprocess.'
    } finally {
      reprocessing.value = false
    }
  }

  async function reprocessAll() {
    if (!currentBatch.value) return
    reprocessingAll.value = true
    error.value = ''
    const jobId = self.crypto?.randomUUID?.() ?? ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c => (c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (c / 4)))).toString(16))
    progressHook.startPolling(jobId)
    try {
      const { data } = await api.post(
        `/tickets/batches/${currentBatch.value.batchId}/reprocess-all`,
        null,
        { params: { jobId } }
      )
      currentBatch.value = data
    } catch (e) {
      error.value = e?.response?.data?.error || 'Failed to reprocess.'
    } finally {
      reprocessingAll.value = false
      progressHook.stopPolling()
    }
  }

  function downloadExport(columns) {
    if (currentBatch.value) {
      let url = `${api.defaults.baseURL}/tickets/batches/${currentBatch.value.batchId}/export`
      if (columns?.length) url += `?columns=${columns.join(',')}`
      window.open(url, '_blank')
    }
  }

  async function updateTicket(ticketId, dto) {
    try {
      const { data } = await api.patch(`/tickets/${ticketId}`, dto)
      if (currentBatch.value?.tickets) {
        const idx = currentBatch.value.tickets.findIndex(t => t.id === ticketId)
        if (idx >= 0) currentBatch.value.tickets[idx] = data
      }
      return data
    } catch (e) {
      error.value = e?.response?.data?.error || 'Failed to update ticket.'
      return null
    }
  }

  async function getSimilars(ticketId) {
    try {
      const { data } = await api.get(`/tickets/${ticketId}/similar`)
      return data
    } catch (e) {
      error.value = e?.response?.data?.error || 'Failed to search similar tickets.'
      return []
    }
  }

  return {
    view,
    batches,
    currentBatch,
    error,
    reprocessing,
    reprocessingAll,
    loadBatches,
    openDetail,
    checkDuplicate,
    uploadFile,
    reprocessBatch,
    reprocessAll,
    downloadExport,
    updateTicket,
    getSimilars,
  }
}
