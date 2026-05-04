<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { isAuthenticated } from '../services/auth'
import { getRatingUsers } from '../services/ratings'

const platformHighlights = [
  {
    label: 'Teorija',
    title: 'Mācies pa valodām un tēmām',
    summary:
      'Python, Java, JavaScript, PHP un C# materiāli ir sadalīti īsās lapās ar grūtības pakāpēm, piemēriem un progresu.',
    route: { name: 'theory' },
    action: 'Atvērt teoriju',
  },
  {
    label: 'Uzdevumi',
    title: 'Pārbaudi zināšanas praksē',
    summary:
      'Risini programmēšanas uzdevumus, iesniedz kodu un saņem rezultātu pēc testu izpildes.',
    route: { name: 'exercises' },
    action: 'Skatīt uzdevumus',
  },
  {
    label: 'Reitings',
    title: 'Seko progresam kopā ar citiem',
    summary:
      'Lietotāju un pārstāvniecību reitingi parāda, kā aug zināšanas, atrisinātie uzdevumi un kopējais ieguldījums.',
    route: { name: 'ratings' },
    action: 'Apskatīt reitingu',
  },
]

const userCount = ref<number | null>(null)

const quickStats = computed(() => [
  {
    value: userCount.value === null ? '...' : new Intl.NumberFormat('lv-LV').format(userCount.value),
    label: 'lietotāji',
  },
  { value: '24/7', label: 'pieejams pašmācībai' },
])

const flowSteps = [
  'Izvēlies valodu un sāc ar teoriju',
  'Atzīmē izlasītās lapas un izpildi testus',
  'Risini praktiskos uzdevumus un krāj reitingu',
]

onMounted(async () => {
  try {
    userCount.value = (await getRatingUsers()).length
  } catch {
    userCount.value = 0
  }
})
</script>

<template>
  <section class="home-shell">
    <header class="home-hero content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="theory-header exercises-header">
          <span class="page-title-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M4 19.5V5a2 2 0 0 1 2-2h12" />
              <path d="M6 17h12a2 2 0 0 1 2 2v1H6a2 2 0 0 1 0-4z" />
              <path d="M9 7h6" />
              <path d="M9 11h4" />
            </svg>
          </span>
          <div class="theory-heading-copy">
            <h1 class="section-heading mb-1">Programmēšanas mācības vienā platformā</h1>
            <p class="home-lead mb-0">
              Mācies teoriju, pārbaudi zināšanas testos, risini praktiskus uzdevumus un seko progresam reitingā.
            </p>
          </div>
          <div class="exercises-header__badges representations-summary home-hero__badges">
            <div v-for="stat in quickStats" :key="stat.label" class="representations-stat-chip home-stat-chip">
              <span>{{ stat.label }}</span>
              <strong>{{ stat.value }}</strong>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div class="home-grid">
      <article
        v-for="item in platformHighlights"
        :key="item.title"
        class="home-feature content-panel card border-primary-subtle"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading mb-3">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 12h14" />
                <path d="M12 5l7 7-7 7" />
              </svg>
            </span>
            <span class="home-feature__label">{{ item.label }}</span>
          </div>
          <h2>{{ item.title }}</h2>
          <p>{{ item.summary }}</p>
          <router-link class="home-feature__link" :to="item.route">{{ item.action }}</router-link>
        </div>
      </article>
    </div>

    <section class="home-flow content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="home-flow__header">
          <div class="representations-section-heading">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 6h18" />
                <path d="M3 12h18" />
                <path d="M3 18h18" />
              </svg>
            </span>
            <h2 class="section-heading section-heading--caps mb-0">Mācību ceļš</h2>
          </div>
          <router-link v-if="!isAuthenticated" class="btn btn-outline-light btn-sm" :to="{ name: 'register' }">Izveidot profilu</router-link>
        </div>

        <ol class="home-flow__steps">
          <li v-for="(step, index) in flowSteps" :key="step">
            <span>{{ index + 1 }}</span>
            <strong>{{ step }}</strong>
          </li>
        </ol>
      </div>
    </section>
  </section>
</template>
