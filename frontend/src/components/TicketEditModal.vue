<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  ticket: { type: Object, required: true },
  saving: { type: Boolean, default: false }
})
const emit = defineEmits(['close', 'save'])

const categories = ['Question', 'Bug', 'Complaint', 'Login', 'Payment', 'Financial', 'Performance', 'Integration', 'Registration', 'Sales', 'Suggestion', 'Praise', 'Other']
const priorities = ['Low', 'Medium', 'High', 'Critical']
const departments = ['Support', 'Financial', 'Sales', 'Product', 'Development']
const sentiments = ['positive', 'negative', 'neutral']

const form = ref({
  category: '',
  priority: '',
  department: '',
  sentiment: '',
  tagsText: ''
})

watch(() => props.ticket, (t) => {
  if (t) {
    form.value = {
      category: t.category || '',
      priority: t.priority || '',
      department: t.department || '',
      sentiment: t.sentiment || 'neutral',
      tagsText: (t.tags || []).join(', ')
    }
  }
}, { immediate: true })

function save() {
  const tags = form.value.tagsText
    .split(',')
    .map(t => t.trim().toLowerCase())
    .filter(Boolean)

  emit('save', {
    category: form.value.category,
    priority: form.value.priority,
    department: form.value.department,
    sentiment: form.value.sentiment,
    tags
  })
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4" @click.self="emit('close')">
    <div class="bg-white rounded-2xl border border-slate-200 shadow-2xl max-w-lg w-full">
      <div class="border-b border-slate-200 p-5 flex items-center justify-between">
        <div>
          <span class="text-[10px] uppercase font-bold text-amber-600 tracking-widest block">Reclassify</span>
          <h3 class="text-lg font-bold text-slate-900">{{ ticket.externalId || ticket.id?.substring(0, 8) }}</h3>
        </div>
        <button @click="emit('close')" class="text-slate-400 hover:text-slate-600 p-2 cursor-pointer">
          <i class="fa-solid fa-xmark text-lg"></i>
        </button>
      </div>

      <form @submit.prevent="save" class="p-6 space-y-4">
        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Category</label>
          <select v-model="form.category" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Priority</label>
          <select v-model="form.priority" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="p in priorities" :key="p" :value="p">{{ p }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Department</label>
          <select v-model="form.department" class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
          </select>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Sentiment</label>
          <div class="flex gap-2">
            <label v-for="s in sentiments" :key="s"
              :class="form.sentiment === s ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'"
              class="flex-1 text-center px-3 py-2.5 rounded-xl text-sm font-medium border cursor-pointer transition">
              <input type="radio" v-model="form.sentiment" :value="s" class="hidden" />
              <i class="fa-solid mr-1" :class="s === 'positive' ? 'fa-face-smile' : s === 'negative' ? 'fa-face-frown' : 'fa-face-meh'"></i>
              {{ s.charAt(0).toUpperCase() + s.slice(1) }}
            </label>
          </div>
        </div>

        <div>
          <label class="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1.5">Tags</label>
          <input v-model="form.tagsText" type="text" placeholder="login, access, error (separated by comma)"
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500" />
          <p class="text-[10px] text-slate-400 mt-1">Separate tags by comma</p>
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button type="button" @click="emit('close')" class="px-4 py-2.5 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50 transition cursor-pointer">
            Cancel
          </button>
          <button type="submit" :disabled="saving"
            class="bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl text-sm font-semibold transition cursor-pointer flex items-center gap-2">
            <i v-if="saving" class="fa-solid fa-spinner animate-spin"></i>
            <i v-else class="fa-solid fa-check"></i>
            {{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
