<script setup>
import { formatarData } from '../utils/chartMappers'

defineProps({
  nomeArquivo: { type: String, required: true },
  batchExistente: { type: Object, required: true }
})
const emit = defineEmits(['sobrescrever', 'novo', 'cancelar'])
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('cancelar')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-md w-full">
      <div class="border-b border-slate-200 p-5">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-amber-100 rounded-xl flex items-center justify-center">
            <i class="fa-solid fa-triangle-exclamation text-amber-600 text-lg"></i>
          </div>
          <div>
            <span class="text-[10px] uppercase font-bold text-amber-600 tracking-widest block">Duplicate file</span>
            <h3 class="text-lg font-bold text-slate-900">Existing record</h3>
          </div>
        </div>
      </div>

      <div class="p-6 space-y-4">
        <p class="text-sm text-slate-600">
          A processed batch already exists with the file <b class="text-slate-900">{{ nomeArquivo }}</b>.
        </p>

        <div class="bg-slate-50 rounded-xl border border-slate-100 p-4 space-y-2">
          <div class="flex items-center justify-between text-sm">
            <span class="text-slate-500">Tickets</span>

            <span class="font-bold text-slate-900">{{ batchExistente.total }}</span>
          </div>
          <div class="flex items-center justify-between text-sm">
            <span class="text-slate-500">Processed on</span>
            <span class="font-medium text-slate-700">{{ formatarData(batchExistente.dataCriacao) }}</span>
          </div>
        </div>

        <p class="text-xs text-slate-500">What would you like to do?</p>
      </div>

      <div class="border-t border-slate-200 p-5 flex flex-col gap-2">
        <button @click="emit('sobrescrever')"
          class="w-full bg-amber-500 hover:bg-amber-400 text-white px-4 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center justify-center gap-2">
          <i class="fa-solid fa-arrows-rotate"></i>
          Overwrite existing record
        </button>
        <button @click="emit('novo')"
          class="w-full bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center justify-center gap-2">
          <i class="fa-solid fa-plus"></i>
          Insert as new record
        </button>
        <button @click="emit('cancelar')"
          class="w-full border border-slate-200 hover:bg-slate-50 text-slate-600 px-4 py-2.5 rounded-xl text-sm font-medium transition cursor-pointer">
          Cancel
        </button>
      </div>
    </div>
  </div>
</template>
