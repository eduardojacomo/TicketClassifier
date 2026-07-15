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

const paletaCores = [
  '#6366f1', '#8b5cf6', '#a78bfa', '#c084fc',
  '#818cf8', '#7c3aed', '#6d28d9', '#5b21b6',
  '#4f46e5', '#4338ca',
]

const chartData = computed(() => {
  const entradas = Object.entries(props.dados || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
  const labels = entradas.map(([k]) => k)
  const values = entradas.map(([, v]) => v)
  const total = values.reduce((s, v) => s + v, 0)
  const colors = labels.map((l, i) => props.cores?.[l] ?? paletaCores[i % paletaCores.length])

  return {
    labels,
    datasets: [{
      data: values,
      backgroundColor: colors,
      borderColor: 'transparent',
      borderWidth: 0,
      borderRadius: 4,
      borderSkipped: false,
      maxBarThickness: 28,
      barPercentage: 0.7,
      categoryPercentage: 0.8,
    }],
    _total: total,
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  layout: {
    padding: { right: 40 },
  },
  plugins: {
    legend: { display: false },
    title: { display: false },
    tooltip: {
      enabled: true,
      backgroundColor: 'rgba(15, 23, 42, 0.95)',
      titleColor: '#f8fafc',
      bodyColor: '#cbd5e1',
      titleFont: { size: 13, weight: '600' },
      bodyFont: { size: 12 },
      borderColor: 'rgba(99, 102, 241, 0.3)',
      borderWidth: 1,
      cornerRadius: 10,
      padding: { x: 14, y: 10 },
      displayColors: true,
      boxWidth: 8,
      boxHeight: 8,
      boxPadding: 4,
      usePointStyle: true,
      callbacks: {
        label: (ctx) => {
          const total = ctx.dataset.data.reduce((s, v) => s + v, 0)
          const pct = total ? Math.round((ctx.raw / total) * 100) : 0
          return `  ${ctx.raw} tickets  ·  ${pct}%`
        },
      },
    },
  },
  scales: {
    x: {
      display: false,
      grid: { display: false },
      border: { display: false },
    },
    y: {
      grid: { display: false },
      ticks: {
        color: '#475569',
        font: { size: 12, weight: '500', family: "'Inter', system-ui, sans-serif" },
        padding: 8,
      },
      border: { display: false },
    },
  },
  animation: {
    duration: 800,
    easing: 'easeOutQuart',
  },
}))

const totalItems = computed(() => {
  return Object.values(props.dados || {}).reduce((s, v) => s + v, 0)
})

const topEntries = computed(() => {
  return Object.entries(props.dados || {})
    .filter(([, v]) => v > 0)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 3)
    .map(([k, v]) => ({ label: k, value: v, pct: totalItems.value ? Math.round((v / totalItems.value) * 100) : 0 }))
})
</script>

<template>
  <div class="bg-white rounded-2xl border border-slate-200/80 p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
    <div class="flex items-center justify-between mb-1">
      <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-widest">{{ titulo }}</h3>
      <span class="text-[10px] text-slate-400 font-medium bg-slate-50 px-2 py-0.5 rounded-full">{{ totalItems }} total</span>
    </div>

    <div class="flex items-baseline gap-2 mb-4">
      <span class="text-2xl font-bold text-slate-900">{{ topEntries[0]?.label || '—' }}</span>
      <span v-if="topEntries[0]" class="text-sm text-slate-400">{{ topEntries[0]?.pct }}%</span>
    </div>

    <div class="h-48">
      <Bar :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>
