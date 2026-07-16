import { ref, computed } from 'vue'
import api from '../api'

export function useTicketProgress() {
  const processing = ref(false)
  const prog = ref({ total: 0, processed: 0, ok: 0, failures: 0, totalBatches: 0, batchesCompleted: 0 })
  const logs = ref([])

  let progTimer = null
  let progStart = 0
  let lastBatches = 0

  const pctComplete = computed(() => (prog.value.total ? Math.round((prog.value.processed / prog.value.total) * 100) : 0))

  const estimatedTimeRemaining = computed(() => {
    const { processed, total } = prog.value
    if (processed <= 0 || processed >= total) return ''
    const seconds = Math.ceil((total - processed) * ((Date.now() - progStart) / 1000 / processed))
    return seconds > 0 ? `~${seconds}s remaining` : ''
  })

  function addLog(msg) {
    logs.value.push(`[${new Date().toLocaleTimeString('en-US')}] ${msg}`)
  }

  function startPolling(jobId) {
    processing.value = true
    prog.value = { total: 0, processed: 0, ok: 0, failures: 0, totalBatches: 0, batchesCompleted: 0 }
    logs.value = []
    lastBatches = 0
    progStart = Date.now()
    addLog('Reading CSV file...')

    progTimer = setInterval(async () => {
      try {
        const { data } = await api.get(`/tickets/progress/${jobId}`)
        if (!data.total) return

        if (prog.value.total === 0) {
          addLog(`File mapped: ${data.total} tickets in ${data.totalBatches} batch(es).`)
        }

        prog.value = data

        if (data.batchesCompleted > lastBatches) {
          for (let l = lastBatches + 1; l <= data.batchesCompleted; l++) {
            addLog(`Batch ${l}/${data.totalBatches} classified — ${data.ok} ok, ${data.failures} accumulated failure(s).`)
          }
          lastBatches = data.batchesCompleted
        }
      } catch { /* Fail silent no polling */ }
    }, 500)
  }

  function stopPolling() {
    clearInterval(progTimer)
    processing.value = false
  }

  return {
    processing,
    prog,
    logs,
    pctComplete,
    estimatedTimeRemaining,
    startPolling,
    stopPolling,
  }
}
