import { ref, computed } from 'vue'
import api from '../api'

export function useTicketProgress() {
  const processando = ref(false)
  const prog = ref({ total: 0, processados: 0, ok: 0, falhas: 0, totalLotes: 0, lotesConcluidos: 0 })
  const logs = ref([])
  
  let progTimer = null
  let progInicio = 0
  let ultLotes = 0

  const pctConcluido = computed(() => (prog.value.total ? Math.round((prog.value.processados / prog.value.total) * 100) : 0))
  
  const tempoRestanteEstima = computed(() => {
    const { processados, total } = prog.value
    if (processados <= 0 || processados >= total) return ''
    const segundos = Math.ceil((total - processados) * ((Date.now() - progInicio) / 1000 / processados))
    return segundos > 0 ? `~${segundos}s remaining` : ''
  })

  function adicionarLog(msg) {
    logs.value.push(`[${new Date().toLocaleTimeString('en-US')}] ${msg}`)
  }

  function iniciarPolling(jobId) {
    processando.value = true
    prog.value = { total: 0, processados: 0, ok: 0, falhas: 0, totalLotes: 0, lotesConcluidos: 0 }
    logs.value = []
    ultLotes = 0
    progInicio = Date.now()
    adicionarLog('Reading CSV file...')

    progTimer = setInterval(async () => {
      try {
        const { data } = await api.get(`/tickets/progress/${jobId}`)
        if (!data.total) return

        if (prog.value.total === 0) {
          adicionarLog(`File mapped: ${data.total} tickets in ${data.totalLotes} batch(es).`)
        }
        
        prog.value = data
        
        if (data.lotesConcluidos > ultLotes) {
          for (let l = ultLotes + 1; l <= data.lotesConcluidos; l++) {
            adicionarLog(`Batch ${l}/${data.totalLotes} classified — ${data.ok} ok, ${data.falhas} accumulated failure(s).`)
          }
          ultLotes = data.lotesConcluidos
        }
      } catch { /* Fail silent no polling */ }
    }, 500)
  }

  function pararPolling() {
    clearInterval(progTimer)
    processando.value = false
  }

  return {
    processando,
    prog,
    logs,
    pctConcluido,
    tempoRestanteEstima,
    iniciarPolling,
    pararPolling,
  }
}