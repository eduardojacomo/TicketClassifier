<script setup>
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps({
  titulo: { type: String, default: '' },
  dados: { type: Object, default: () => ({}) },
  cores: { type: Object, default: null },
})

const paletaCores = ['#4f46e5','#0ea5e9','#8b5cf6','#ec4899','#f59e0b','#10b981','#6366f1','#f43f5e','#14b8a6','#64748b']

const coresPrioridadeChart = {
  'Baixa': '#10b981',
  'Média': '#3b82f6',
  'Alta': '#f59e0b',
  'Crítica': '#ef4444',
}

const chartData = computed(() => {
  const entradas = Object.entries(props.dados || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
  const labels = entradas.map(([k]) => k)
  const values = entradas.map(([, v]) => v)
  const resolvedCores = props.cores || {}
  const colors = labels.map((l, i) => resolvedCores[l] ?? coresPrioridadeChart[l] ?? paletaCores[i % paletaCores.length])

  return {
    labels,
    datasets: [{
      data: values,
      backgroundColor: colors,
      borderColor: '#ffffff',
      borderWidth: 3,
      hoverOffset: 8,
    }],
  }
})

const total = computed(() => {
  return Object.values(props.dados || {}).reduce((s, v) => s + v, 0)
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        color: '#334155',
        font: { size: 12, weight: '500' },
        padding: 16,
        usePointStyle: true,
        pointStyleWidth: 10,
      },
    },
    tooltip: {
      backgroundColor: '#0f172a',
      titleColor: '#e2e8f0',
      bodyColor: '#e2e8f0',
      borderColor: '#334155',
      borderWidth: 1,
      cornerRadius: 8,
      padding: 10,
      callbacks: {
        label: (ctx) => {
          const total = ctx.dataset.data.reduce((s, v) => s + v, 0)
          const pct = total ? Math.round((ctx.raw / total) * 100) : 0
          return ` ${ctx.label}: ${ctx.raw} (${pct}%)`
        },
      },
    },
  },
}
</script>

<template>
  <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-4">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-bold text-slate-900 uppercase tracking-wider">{{ titulo }}</h3>
      <span class="text-[10px] text-slate-400 font-medium">Distribuicao</span>
    </div>
    <div class="h-64 flex items-center justify-center relative">
      <Doughnut :data="chartData" :options="chartOptions" />
      <div class="absolute text-center pointer-events-none" style="top: 35%; left: 50%; transform: translate(-50%, -50%);">
        <span class="block text-2xl font-extrabold text-slate-900">{{ total }}</span>
        <span class="block text-[10px] text-slate-400 font-semibold uppercase">Total</span>
      </div>
    </div>
  </div>
</template>
