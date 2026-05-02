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

type SidebarSection = {
  label: string
  to: string
  description: string
  icon: string
}

const sections = computed<SidebarSection[]>(() => [
  {
    label: 'Teorija',
    to: '/theory',
    description: 'Mācību materiāli un skaidrojumi',
    icon: 'book',
  },
  {
    label: 'Uzdevumi',
    to: '/exercises',
    description: 'Programmēšanas uzdevumi',
    icon: 'code',
  },
  ...(isAuthenticated.value
    ? [{
        label: 'Iesniegumi',
        to: '/submissions',
        description: 'Tavi uzdevumu iesniegumi',
        icon: 'inbox',
      }]
    : []),
  ...(isAuthenticated.value
    ? [{
        label: 'Pārstāvniecības',
        to: '/representations',
        description: 'Grupas, dalība un kopējā statistika',
        icon: 'users',
      }]
    : []),
  {
    label: 'Reitings',
    to: '/ratings',
    description: 'Lietotāju un pārstāvniecību tops',
    icon: 'trophy',
  },
])
</script>

<template>
  <aside class="app-sidebar card border-primary-subtle" :class="{ 'is-open': open }">
    <div class="card-body p-3 p-lg-4 sidebar-body">
      <div class="sidebar-header">
        <h2 class="sidebar-title mb-0">Sadaļas</h2>
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
          <span class="sidebar-link__icon" aria-hidden="true">
            <!-- Teorija — atvērta grāmata -->
            <svg v-if="section.icon === 'book'" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" />
              <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" />
            </svg>
            <!-- Uzdevumi — kods/iekavas -->
            <svg v-else-if="section.icon === 'code'" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="16 18 22 12 16 6" />
              <polyline points="8 6 2 12 8 18" />
            </svg>
            <!-- Iesniegumi — pasta kaste -->
            <svg v-else-if="section.icon === 'inbox'" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="22 12 16 12 14 15 10 15 8 12 2 12" />
              <path d="M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z" />
            </svg>
            <!-- Pārstāvniecības — cilvēku grupa -->
            <svg v-else-if="section.icon === 'users'" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>
            <!-- Reitings — kauss -->
            <svg v-else-if="section.icon === 'trophy'" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6" />
              <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18" />
              <path d="M4 22h16" />
              <path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22" />
              <path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22" />
              <path d="M18 2H6v7a6 6 0 0 0 12 0V2z" />
            </svg>
          </span>
          <span class="sidebar-link__body">
            <span class="sidebar-link-label">{{ section.label }}</span>
            <small class="sidebar-link-description">{{ section.description }}</small>
          </span>
        </RouterLink>
      </nav>
    </div>
  </aside>
</template>
