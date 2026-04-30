<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { isAuthenticated } from '../services/auth'

defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  (event: 'close'): void
}>()

const sections = computed(() => [
  { label: 'Teorija', to: '/theory', description: 'Mācību materiāli un skaidrojumi' },
  { label: 'Uzdevumi', to: '/exercises', description: 'Programmēšanas uzdevumi' },
  ...(isAuthenticated.value
    ? [{ label: 'Iesniegumi', to: '/submissions', description: 'Tavi uzdevumu iesniegumi' }]
    : []),
  { label: 'Reitings', to: '/ratings', description: 'Lietotāju tops pēc reitinga' },
])
</script>

<template>
  <aside class="app-sidebar card border-primary-subtle" :class="{ 'is-open': open }">
    <div class="card-body p-3 p-lg-4 sidebar-body">
      <div class="sidebar-header">
        <div>
          <p class="sidebar-label text-uppercase mb-2">Sadaļas</p>
          <h2 class="sidebar-title mb-0">Mācību navigācija</h2>
        </div>
      </div>

      <nav class="sidebar-nav d-grid gap-2">
        <RouterLink
          v-for="section in sections"
          :key="section.to"
          :to="section.to"
          class="sidebar-link"
          active-class="active"
          @click="emit('close')"
        >
          <span class="sidebar-link-label">{{ section.label }}</span>
          <small class="sidebar-link-description">{{ section.description }}</small>
        </RouterLink>
      </nav>
    </div>
  </aside>
</template>
