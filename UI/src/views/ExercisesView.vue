<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { hasAnyRole } from '../services/auth'
import { getExercises, type ExerciseListItem } from '../services/exercises'

const router = useRouter()
const exercises = ref<ExerciseListItem[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const search = ref('')
const diffFilter = ref('all')
const solvedFilter = ref('all')
const sortField = ref<'title' | 'difficulty' | 'tests' | 'completion'>('title')
const sortDir = ref<'asc' | 'desc'>('asc')
const currentPage = ref(1)
const pageSize = 20
const canCreateExercises = computed(() => hasAnyRole(['Pedagogs', 'Administrators']))

const diffOrder: Record<string, number> = { viegls: 0, vidējs: 1, grūts: 2 }

function languageLabel(languageCode: string, languageVersion: string) {
  const code = languageCode.toLowerCase()

  if (code === 'python') {
    return `Python ${languageVersion}`
  }

  if (code === 'java') {
    return `Java ${languageVersion}`
  }

  return `${languageCode} ${languageVersion}`.trim()
}

const filtered = computed(() => {
  let list = exercises.value
  const query = search.value.trim().toLowerCase()

  if (query) {
    list = list.filter((exercise) =>
      `${exercise.title} ${exercise.description}`.toLowerCase().includes(query),
    )
  }

  if (diffFilter.value !== 'all') {
    list = list.filter((exercise) => exercise.difficulty.toLowerCase() === diffFilter.value)
  }

  if (solvedFilter.value === 'solved') list = list.filter((exercise) => exercise.isSolved)
  if (solvedFilter.value === 'unsolved') list = list.filter((exercise) => !exercise.isSolved)

  return [...list].sort((a, b) => {
    let cmp = 0
    if (sortField.value === 'title') cmp = a.title.localeCompare(b.title, 'lv')
    if (sortField.value === 'difficulty') {
      cmp = (diffOrder[a.difficulty.toLowerCase()] ?? 9) - (diffOrder[b.difficulty.toLowerCase()] ?? 9)
    }
    if (sortField.value === 'tests') cmp = a.testCaseCount - b.testCaseCount
    if (sortField.value === 'completion') {
      cmp = (a.solvedAttemptPercent ?? 0) - (b.solvedAttemptPercent ?? 0)
    }
    return sortDir.value === 'asc' ? cmp : -cmp
  })
})

const pageCount = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize)))

const pagedExercises = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filtered.value.slice(start, start + pageSize)
})

const pageSummary = computed(() => {
  if (!filtered.value.length) return '0 no 0'

  const start = (currentPage.value - 1) * pageSize + 1
  const end = Math.min(currentPage.value * pageSize, filtered.value.length)
  return `${start}-${end} no ${filtered.value.length}`
})

watch([search, diffFilter, solvedFilter], () => {
  currentPage.value = 1
})

watch(pageCount, (count) => {
  if (currentPage.value > count) {
    currentPage.value = count
  }
})

onMounted(async () => {
  try {
    exercises.value = await getExercises()
  } catch (e: any) {
    error.value = e.message ?? 'Neizdevās ielādēt uzdevumus.'
  } finally {
    loading.value = false
  }
})

function toggleSort(field: typeof sortField.value) {
  if (sortField.value === field) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortField.value = field
    sortDir.value = 'asc'
  }
}

function sortIcon(field: typeof sortField.value) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

function diffClass(d: string) {
  const v = d.toLowerCase()
  if (v === 'viegls' || v === 'easy') return 'theory-difficulty--iesācējs'
  if (v === 'grūts' || v === 'hard') return 'theory-difficulty--augsts'
  return 'theory-difficulty--vidējs'
}

function diffLabel(d: string) {
  const v = d.toLowerCase()
  if (v === 'viegls' || v === 'easy') return 'Viegls'
  if (v === 'grūts' || v === 'hard') return 'Grūts'
  return 'Vidējs'
}

function attemptLabel(exercise: ExerciseListItem) {
  if (exercise.attemptedUserCount === 0) {
    return '0 mēģ.'
  }

  return `${exercise.attemptedUserCount} mēģ.`
}

function solvedCount(exercise: ExerciseListItem): number {
  if (!exercise.attemptedUserCount) return 0
  return Math.round((exercise.attemptedUserCount * (exercise.solvedAttemptPercent ?? 0)) / 100)
}

function goTo(id: number) {
  router.push({ name: 'exercise', params: { id } })
}
</script>

<template>
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4 theory-panel__body">
      <div class="theory-header exercises-header">
        <span class="exercises-header__icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="16 18 22 12 16 6" />
            <polyline points="8 6 2 12 8 18" />
          </svg>
        </span>
        <div class="theory-heading-copy">
          <h1 class="section-heading mb-0">Programmēšanas uzdevumi</h1>
        </div>
        <div v-if="canCreateExercises" class="theory-header__actions">
          <RouterLink class="btn btn-primary btn-sm" to="/tasks">
            Izveidot uzdevumu
          </RouterLink>
        </div>
      </div>

      <hr class="profile-section-divider" />

      <div class="theory-topics-controls">
        <input
          v-model="search"
          type="search"
          class="theory-search auth-input"
          placeholder="Meklēt uzdevumu..."
        />

        <div class="theory-filter-group">
          <button
            v-for="d in ['all', 'viegls', 'vidējs', 'grūts']"
            :key="d"
            type="button"
            class="ex-filter-btn"
            :class="{ active: diffFilter === d }"
            @click="diffFilter = d"
          >
            {{ d === 'all' ? 'Visas' : d.charAt(0).toUpperCase() + d.slice(1) }}
          </button>
        </div>

        <div class="theory-filter-group">
          <button
            v-for="s in [{ v: 'all', l: 'Visi' }, { v: 'solved', l: 'Izpildīti' }, { v: 'unsolved', l: 'Nav izpildīti' }]"
            :key="s.v"
            type="button"
            class="ex-filter-btn"
            :class="{ active: solvedFilter === s.v }"
            @click="solvedFilter = s.v"
          >
            {{ s.l }}
          </button>
        </div>

        <div class="theory-filter-group">
          <button
            type="button"
            class="ex-filter-btn"
            :class="{ active: sortField === 'completion' }"
            :title="'Kārtot pēc izpildīšanas procenta'"
            @click="toggleSort('completion')"
          >
            Pēc % {{ sortField === 'completion' ? sortIcon('completion') : '↕' }}
          </button>
        </div>
      </div>

      <div v-if="loading" class="theory-empty">Ielādē...</div>
      <div v-else-if="error" class="alert alert-danger mb-0">{{ error }}</div>
      <div v-else-if="!filtered.length" class="theory-empty">
        Nav uzdevumu, kas atbilst filtriem.
      </div>

      <template v-else>
        <div class="theory-list theory-list--topics">
          <article
            v-for="ex in pagedExercises"
            :key="ex.id"
            class="theory-list-item"
            :class="{ 'theory-list-item--done': ex.isSolved }"
            role="button"
            tabindex="0"
            @click="goTo(ex.id)"
            @keydown.enter.self.prevent="goTo(ex.id)"
            @keydown.space.self.prevent="goTo(ex.id)"
          >
            <div class="theory-list-item__badges">
              <span class="theory-difficulty" :class="diffClass(ex.difficulty)">
                {{ diffLabel(ex.difficulty) }}
              </span>
              <span class="theory-volume">{{ ex.testCaseCount }} testi</span>
            </div>
            <div class="theory-list-item__body">
              <strong>
                <span v-if="ex.isSolved" class="ex-solved-mark me-2" aria-label="Izpildīts">✓</span>
                {{ ex.title }}
              </strong>
              <span>{{ ex.description }}</span>
            </div>
            <div class="theory-list-item__meta">
              <div class="theory-language-meta-count">
                <span class="ex-language-chip">{{ languageLabel(ex.languageCode, ex.languageVersion) }}</span>
                <span class="theory-card__kicker">{{ ex.submissionCount }} iesniegumi</span>
              </div>
              <div class="exercises-completion">
                <span class="theory-card__progress">{{ ex.solvedAttemptPercent }}% izpildīja</span>
                <span class="exercises-completion__count">
                  <template v-if="ex.attemptedUserCount === 0">Nav mēģinājumu</template>
                  <template v-else>{{ solvedCount(ex) }} no {{ ex.attemptedUserCount }} cilv.</template>
                </span>
              </div>
            </div>
          </article>
        </div>

        <div class="pagination-row mt-3">
          <span>{{ pageSummary }}</span>
          <div class="pagination-controls">
            <button
              type="button"
              class="pagination-btn"
              :disabled="currentPage === 1"
              @click="currentPage--"
            >
              Iepriekšējā
            </button>
            <strong>{{ currentPage }} / {{ pageCount }}</strong>
            <button
              type="button"
              class="pagination-btn"
              :disabled="currentPage === pageCount"
              @click="currentPage++"
            >
              Nākamā
            </button>
          </div>
        </div>
      </template>
    </div>
  </section>
</template>
