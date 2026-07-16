<script setup>
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps({
  title: { type: String, default: '' },
  data: { type: Object, default: () => ({}) },
  colors: { type: Object, default: null },
})

const colorPalette = ['#6366f1', '#8b5cf6', '#a78bfa', '#c084fc', '#818cf8', '#7c3aed', '#6d28d9', '#5b21b6', '#4f46e5', '#4338ca']

const priorityChartColors = {
  'Low': '#10b981',
  'Medium': '#3b82f6',
  'High': '#f59e0b',
  'Critical': '#ef4444',
}

const entries = computed(() => {
  return Object.entries(props.data || {}).filter(([, v]) => v > 0).sort((a, b) => b[1] - a[1])
})

const total = computed(() => {
  return Object.values(props.data || {}).reduce((s, v) => s + v, 0)
})

const chartData = computed(() => {
  const labels = entries.value.map(([k]) => k)
  const values = entries.value.map(([, v]) => v)
  const resolvedColors = props.colors || {}
  const colors = labels.map((l, i) => resolvedColors[l] ?? priorityChartColors[l] ?? colorPalette[i % colorPalette.length])

  return {
    labels,
    datasets: [{
      data: values,
      backgroundColor: colors,
      borderColor: '#ffffff',
      borderWidth: 3,
      hoverOffset: 6,
      hoverBorderWidth: 0,
    }],
  }
})

const legendItems = computed(() => {
  const resolvedColors = props.colors || {}
  return entries.value.map(([k, v], i) => ({
    label: k,
    value: v,
    pct: total.value ? Math.round((v / total.value) * 100) : 0,
    color: resolvedColors[k] ?? priorityChartColors[k] ?? colorPalette[i % colorPalette.length],
  }))
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '72%',
  plugins: {
    legend: { display: false },
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
  animation: {
    animateRotate: true,
    animateScale: false,
    duration: 800,
    easing: 'easeOutQuart',
  },
}
</script>

<template>
  <div class="bg-white rounded-2xl border border-slate-200/80 p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-widest">{{ title }}</h3>
      <span class="text-[10px] text-slate-400 font-medium bg-slate-50 px-2 py-0.5 rounded-full">{{ total }} total</span>
    </div>

    <div class="flex items-center gap-5">
      <!-- Chart -->
      <div class="relative w-36 h-36 shrink-0">
        <Doughnut :data="chartData" :options="chartOptions" />
        <div class="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
          <span class="text-2xl font-bold text-slate-900 leading-none">{{ total }}</span>
          <span class="text-[9px] text-slate-400 font-semibold uppercase tracking-wider mt-0.5">Total</span>
        </div>
      </div>

      <!-- Custom Legend -->
      <div class="flex-1 space-y-2 min-w-0">
        <div v-for="item in legendItems" :key="item.label" class="flex items-center gap-2.5">
          <span class="w-2.5 h-2.5 rounded-full shrink-0" :style="{ backgroundColor: item.color }"></span>
          <span class="text-xs text-slate-600 truncate flex-1">{{ item.label }}</span>
          <span class="text-xs font-bold text-slate-800 tabular-nums">{{ item.value }}</span>
          <span class="text-[10px] text-slate-400 tabular-nums w-8 text-right">{{ item.pct }}%</span>
        </div>
      </div>
    </div>
  </div>
</template>
