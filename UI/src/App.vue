<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterView } from 'vue-router'
import AppNavbar from './components/AppNavbar.vue'
import AppFooter from './components/AppFooter.vue'
import AppSidebar from './components/AppSidebar.vue'

const isSidebarOpen = ref(false)

function handleTextareaTab(event: KeyboardEvent) {
  if (event.key !== 'Tab') {
    return
  }

  const target = event.target
  if (!(target instanceof HTMLTextAreaElement) || target.disabled || target.readOnly) {
    return
  }

  const start = target.selectionStart ?? target.value.length
  const end = target.selectionEnd ?? target.value.length
  const selectedText = target.value.slice(start, end)

  event.preventDefault()
  target.setRangeText(selectedText ? `\t${selectedText}` : '\t', start, end, 'end')
  target.dispatchEvent(new Event('input', { bubbles: true }))
}

onMounted(() => {
  document.addEventListener('keydown', handleTextareaTab, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleTextareaTab, true)
})
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--sidebar-open': isSidebarOpen }">
    <div class="app-static-background" aria-hidden="true"></div>

    <AppNavbar />

    <button
      type="button"
      class="app-sidebar-toggle"
      :class="{ 'is-open': isSidebarOpen }"
      :aria-label="isSidebarOpen ? 'Aizvērt sadaļu izvēlni' : 'Atvērt sadaļu izvēlni'"
      :aria-expanded="isSidebarOpen"
      @click="isSidebarOpen = !isSidebarOpen"
    >
      <span class="app-sidebar-toggle__icon" aria-hidden="true"></span>
    </button>

    <button
      type="button"
      class="app-backdrop"
      :class="{ 'is-visible': isSidebarOpen }"
      aria-label="Aizvērt sadaļu izvēlni"
      :tabindex="isSidebarOpen ? 0 : -1"
      @click="isSidebarOpen = false"
    ></button>

    <AppSidebar :open="isSidebarOpen" @close="isSidebarOpen = false" />

    <div class="app-content">
      <div class="container-fluid px-3 px-lg-4 py-4 py-lg-5">
        <main class="app-main">
          <RouterView />
        </main>
      </div>
    </div>

    <AppFooter />
  </div>
</template>
