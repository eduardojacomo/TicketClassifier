<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  ticket: { type: Object, required: true },
  salvando: { type: Boolean, default: false }
})
const emit = defineEmits(['fechar', 'salvar'])

const categorias = ['Dúvida', 'Bug', 'Reclamação', 'Login', 'Pagamento', 'Financeiro', 'Performance', 'Integração', 'Cadastro', 'Comercial', 'Sugestão', 'Elogio', 'Outro']
const prioridades = ['Baixa', 'Média', 'Alta', 'Crítica']
const departamentos = ['Suporte', 'Financeiro', 'Comercial', 'Produto', 'Desenvolvimento']
const sentimentos = ['positivo', 'negativo', 'neutro']

const form = ref({
  categoria: '',
  prioridade: '',
  departamento: '',
  sentimento: '',
  tagsText: ''
})

watch(() => props.ticket, (t) => {
  if (t) {
    form.value = {
      categoria: t.categoria || '',
      prioridade: t.prioridade || '',
      departamento: t.departamento || '',
      sentimento: t.sentimento || 'neutro',
      tagsText: (t.tags || []).join(', ')
    }
  }
}, { immediate: true })

function salvar() {
  const tags = form.value.tagsText
    .split(',')
    .map(t => t.trim().toLowerCase())
    .filter(Boolean)

  emit('salvar', {
    categoria: form.value.categoria,
    prioridade: form.value.prioridade,
    departamento: form.value.departamento,
    sentimento: form.value.sentimento,
    tags
  })
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('fechar')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-lg w-full">
      <div class="border-b border-slate-200 p-5 flex items-center justify-between">
        <div>
          <span class="text-[10px] uppercase font-bold text-amber-600 tracking-widest block">Reclassificar</span>
          <h3 class="text-lg font-bold text-slate-900">{{ ticket.externalId || ticket.id?.substring(0, 8) }}</h3>
        </div>
        <button @click="emit('fechar')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
          <i class="fa-solid fa-xmark text-lg"></i>
        </button>
      </div>

      <form @submit.prevent="salvar" class="p-6 space-y-4">
        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Categoria</label>
          <select v-model="form.categoria" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="c in categorias" :key="c" :value="c">{{ c }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Prioridade</label>
          <select v-model="form.prioridade" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="p in prioridades" :key="p" :value="p">{{ p }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Departamento</label>
          <select v-model="form.departamento" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="d in departamentos" :key="d" :value="d">{{ d }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Sentimento</label>
          <div class="flex gap-2">
            <label v-for="s in sentimentos" :key="s"
              :class="form.sentimento === s ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'"
              class="flex-1 text-center px-3 py-2.5 rounded-xl text-sm font-medium border cursor-pointer transition">
              <input type="radio" v-model="form.sentimento" :value="s" class="hidden" />
              <i class="fa-solid mr-1" :class="s === 'positivo' ? 'fa-face-smile' : s === 'negativo' ? 'fa-face-frown' : 'fa-face-meh'"></i>
              {{ s.charAt(0).toUpperCase() + s.slice(1) }}
            </label>
          </div>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Tags</label>
          <input v-model="form.tagsText" type="text" placeholder="login, acesso, erro (separadas por virgula)"
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500" />
          <p class="text-[10px] text-slate-400 mt-1">Separe as tags por virgula</p>
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button type="button" @click="emit('fechar')" class="px-4 py-2.5 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50 transition cursor-pointer">
            Cancelar
          </button>
          <button type="submit" :disabled="salvando"
            class="bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center gap-2">
            <i v-if="salvando" class="fa-solid fa-spinner animate-spin"></i>
            <i v-else class="fa-solid fa-check"></i>
            {{ salvando ? 'Salvando...' : 'Salvar' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
