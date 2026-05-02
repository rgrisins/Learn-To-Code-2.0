<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import logoUrl from '../assets/LTC-LOGO.png'
import { hasAnyRole, isAuthenticated, logout } from '../services/auth'

const router = useRouter()
const isMenuOpen = ref(false)
const isLogoutModalOpen = ref(false)
const isLoggingOut = ref(false)

const publicNavItems = [
  { label: 'Par mums', to: '/about', icon: 'info' },
]

const guestNavItems = [
  ...publicNavItems,
  { label: 'Autorizēties', to: '/login', icon: 'login' },
]

const navItems = computed(() => {
  if (!isAuthenticated.value) {
    return guestNavItems
  }

  return [
    ...publicNavItems,
    ...(hasAnyRole(['Administrators']) ? [{ label: 'Admin', to: '/admin', icon: 'admin' }] : []),
    { label: 'Profils', to: '/profile', icon: 'profile' },
    { label: 'Iziet', action: 'logout', icon: 'logout' },
  ]
})

const navIcons: Record<string, string> = {
  home: 'M4 11.5 12 4l8 7.5V20a1 1 0 0 1-1 1h-4.5v-6.5h-5V21H5a1 1 0 0 1-1-1z',
  book:
    'M6 4.75A1.75 1.75 0 0 0 4.25 6.5v11A1.75 1.75 0 0 0 6 19.25h11.5a.75.75 0 0 0 .75-.75V5.5a.75.75 0 0 0-.75-.75H6zm0 2h10.5v10.5H6a.25.25 0 0 1-.25-.25v-10A.25.25 0 0 1 6 6.75zm2 1.75h5v1.5H8v-1.5zm0 3h7v1.5H8v-1.5z',
  info: 'M12 2.75A9.25 9.25 0 1 0 21.25 12 9.26 9.26 0 0 0 12 2.75Zm0 16.5A7.25 7.25 0 1 1 19.25 12 7.26 7.26 0 0 1 12 19.25Zm-1-5.75h2V8.5h-2Zm0-5h2v-2h-2z',
  login: 'M10 17.25V15h4.25v-6H10V6.75L4.75 12 10 17.25Zm6.75-11.5h-4.5v1.5h4.5c.4 0 .75.35.75.75v9c0 .4-.35.75-.75.75h-4.5v1.5h4.5A2.25 2.25 0 0 0 19 17V8.5a2.25 2.25 0 0 0-2.25-2.25Z',
  profile:
    'M12 12.2a4.2 4.2 0 1 0-4.2-4.2 4.2 4.2 0 0 0 4.2 4.2Zm0 2.1c-4.1 0-7.5 2.2-7.5 4.9V21h15v-1.8c0-2.7-3.4-4.9-7.5-4.9Z',
  admin:
    'M12 2.75 19.25 6v5.2c0 4.45-2.98 8.62-7.25 10.05-4.27-1.43-7.25-5.6-7.25-10.05V6L12 2.75Zm0 2.2L6.75 7.3v3.9c0 3.35 2.07 6.52 5.25 7.9 3.18-1.38 5.25-4.55 5.25-7.9V7.3L12 4.95Zm-1 4.05h2v2h2v2h-2v2h-2v-2H9v-2h2V9Z',
  logout: 'M9.25 16.25V19h6.5A2.25 2.25 0 0 0 18 16.75v-9.5A2.25 2.25 0 0 0 15.75 5h-6.5v2.75h1.5V6.5h5a.75.75 0 0 1 .75.75v9.5a.75.75 0 0 1-.75.75h-5v-1.25h-1.5Zm-4.5-4.25 3.25-3.25v2.25h6v2h-6V15l-3.25-3Z',
}

const closeMenu = () => {
  isMenuOpen.value = false
}

function openLogoutModal() {
  isLogoutModalOpen.value = true
  closeMenu()
}

function closeLogoutModal() {
  isLogoutModalOpen.value = false
}

async function handleLogout() {
  isLoggingOut.value = true

  try {
    await logout()
    await router.replace('/')
    closeLogoutModal()
  } finally {
    isLoggingOut.value = false
  }
}

async function confirmLogout() {
  await handleLogout()
}
</script>

<template>
  <nav class="navbar navbar-expand-lg navbar-dark site-navbar border-bottom border-primary-subtle">
    <div class="container-fluid px-3 px-lg-4 py-2">
      <div class="navbar-brand-wrap d-flex align-items-center gap-2">
        <RouterLink class="navbar-brand d-flex align-items-center gap-2 fw-semibold" to="/" @click="closeMenu">
          <img :src="logoUrl" alt="LearnToCode logo" class="brand-logo" />
          <span>LearnToCode</span>
        </RouterLink>
      </div>

      <button
        class="navbar-toggler"
        type="button"
        :class="{ collapsed: !isMenuOpen }"
        :aria-expanded="isMenuOpen"
        aria-label="Pārslēgt navigāciju"
        @click="isMenuOpen = !isMenuOpen"
      >
        <span class="navbar-toggler-icon"></span>
      </button>

      <div class="collapse navbar-collapse" :class="{ show: isMenuOpen }">
        <ul class="navbar-nav ms-auto mb-2 mb-lg-0 align-items-stretch gap-lg-0">
          <li v-for="item in navItems" :key="'to' in item ? item.to : item.action" class="nav-item">
            <RouterLink
              v-if="!('action' in item)"
              class="nav-link nav-link-block px-4 nav-link-with-icon"
              active-class="active"
              :to="item.to"
              @click="closeMenu"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true" class="navbar-item__icon">
                <path :d="navIcons[item.icon]" fill="currentColor" />
              </svg>
              <span>{{ item.label }}</span>
            </RouterLink>

            <button
              v-else
              type="button"
              class="nav-link nav-link-block px-4 nav-link-with-icon nav-link-button"
              @click="openLogoutModal"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true" class="navbar-item__icon">
                <path :d="navIcons[item.icon]" fill="currentColor" />
              </svg>
              <span>{{ item.label }}</span>
            </button>
          </li>
        </ul>
      </div>
    </div>
  </nav>

  <div v-if="isLogoutModalOpen" class="app-modal-backdrop" @click.self="closeLogoutModal">
    <div class="app-modal app-modal--sm card border-primary-subtle logout-modal">
      <div class="card-body p-3 p-lg-4">
        <div class="logout-modal__icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
        </div>
        <h2 class="section-heading logout-modal-title mb-2">Iziet no konta?</h2>
        <p class="logout-modal-text mb-4">
          Pēc iziešanas tev būs jāautorizējas no jauna, lai turpinātu mācīties.
        </p>

        <div class="logout-modal__actions">
          <button class="btn btn-outline-light" type="button" @click="closeLogoutModal">Atcelt</button>
          <button class="btn btn-primary logout-modal__confirm" type="button" :disabled="isLoggingOut" @click="confirmLogout">
            <span v-if="isLoggingOut" class="logout-modal__spinner" aria-hidden="true"></span>
            {{ isLoggingOut ? 'Notiek iziešana...' : 'Jā, iziet' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
