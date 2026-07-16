export const colorPalette = ['#14b8a6','#3b82f6','#a78bfa','#f472b6','#f59e0b','#10b981','#60a5fa','#f87171','#34d399','#94a3b8']
export const priorityColors = { Low:'#94a3b8', Medium:'#3b82f6', High:'#eab308', Critical:'#ef4444' }

export function mapDictToBars(dict, fixedColors = null) {
  const entries = Object.entries(dict || {}).filter(([, v]) => v > 0)
  const batchTotal = entries.reduce((s, [, v]) => s + v, 0) || 1

  return entries
    .sort((a, b) => b[1] - a[1])
    .map(([label, value], index) => {
      const realPercent = Math.round((value / batchTotal) * 100)
      return {
        label,
        value,
        pct: realPercent,
        width: realPercent,
        color: fixedColors?.[label] ?? colorPalette[index % colorPalette.length],
      }
    })
}

export function formatDate(iso) {
  return iso ? new Date(iso).toLocaleString('en-US') : ''
}
