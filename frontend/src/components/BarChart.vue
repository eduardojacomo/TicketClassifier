<script setup>
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const props = defineProps({
  titulo: { type: String, default: '' },
  dados: { type: Object, default: () => ({}) },
  cores: { type: Object, default: null },
})

const paletaCores = ['#4f46e5','#0ea5e9','#8b5cf6','#ec4899','#f59e0b','#10b981','#6366f1','#f43f5e','#14b8a6','#64748b']

const chartData = computed(() => {
  const entradas = Object.entries(props.dados || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
  const labels = entradas.map(([k]) => k)
  const values = entradas.map(([, v]) => v)
  const colors = labels.map((l, i) => props.cores?.[l] ?? paletaCores[i % paletaCores.length])

  return {
    labels,
    datasets: [{
      data: values,
      backgroundColor: colors.map(c => c + '30'),
      borderColor: colors,
      borderWidth: 2,
      borderRadius: 6,
      maxBarThickness: 36,
    }],
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: {
    legend: { display: false },
    title: { display: false },
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
          return ` ${ctx.raw} (${pct}%)`
        },
      },
    },
  },
  scales: {
    x: {
      grid: { color: '#f1f5f9' },
      ticks: { color: '#94a3b8', font: { size: 11 } },
      border: { display: false },
    },
    y: {
      grid: { display: false },
      ticks: { color: '#334155', font: { size: 12, weight: '500' } },
      border: { display: false },
    },
  },
}
</script>

<template>
  <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm space-y-4">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-bold text-slate-900 uppercase tracking-wider">{{ titulo }}</h3>
      <span class="text-[10px] text-slate-400 font-medium">Volumetria</span>
    </div>
    <div class="h-52">
      <Bar :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>
