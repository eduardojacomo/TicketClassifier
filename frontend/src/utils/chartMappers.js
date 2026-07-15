export const paletaCores = ['#14b8a6','#3b82f6','#a78bfa','#f472b6','#f59e0b','#10b981','#60a5fa','#f87171','#34d399','#94a3b8']
export const coresPrioridade = { Low:'#94a3b8', Medium:'#3b82f6', High:'#eab308', Critical:'#ef4444' }

export function mapearDicionarioParaBarras(dic, coresFixas = null) {
  const entradas = Object.entries(dic || {}).filter(([, v]) => v > 0)
  const totalLote = entradas.reduce((s, [, v]) => s + v, 0) || 1

  return entradas
    .sort((a, b) => b[1] - a[1])
    .map(([label, valor], index) => {
      const percentualReal = Math.round((valor / totalLote) * 100)
      return {
        label,
        valor,
        pct: percentualReal,
        largura: percentualReal,
        cor: coresFixas?.[label] ?? paletaCores[index % paletaCores.length],
      }
    })
}

export function formatarData(iso) {
  return iso ? new Date(iso).toLocaleString('en-US') : ''
}