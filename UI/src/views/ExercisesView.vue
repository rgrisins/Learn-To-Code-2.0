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
const sortField = ref<'title' | 'difficulty' | 'tests'>('title')
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
  if (v === 'viegls' || v === 'easy') return 'diff-easy'
  if (v === 'grūts' || v === 'hard') return 'diff-hard'
  return 'diff-medium'
}

function attemptLabel(exercise: ExerciseListItem) {
  if (exercise.attemptedUserCount === 0) {
    return '0 mēģ.'
  }

  return `${exercise.attemptedUserCount} mēģ.`
}

function goTo(id: number) {
  router.push({ name: 'exercise', params: { id } })
}
</script>

<template>
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4">
      <div class="d-flex align-items-start justify-content-between gap-3 flex-wrap mb-4">
        <h1 class="section-heading mb-0">Programmēšanas uzdevumi</h1>
        <RouterLink v-if="canCreateExercises" class="btn btn-primary btn-sm" to="/tasks">
          Izveidot uzdevumu
        </RouterLink>
      </div>

      <div class="ex-toolbar mb-3">
        <input
          v-model="search"
          type="search"
          class="ex-search auth-input"
          placeholder="Meklēt uzdevumu..."
        />

        <div class="ex-filter-group">
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

        <div class="ex-filter-group">
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
      </div>

      <div v-if="loading" class="text-secondary-emphasis small py-3">Ielādē...</div>
      <div v-else-if="error" class="text-danger small py-3">{{ error }}</div>
      <div v-else-if="!filtered.length" class="text-secondary-emphasis small py-3">
        Nav uzdevumu, kas atbilst filtriem.
      </div>

      <template v-else>
        <div class="ex-table-wrap">
          <table class="ex-table">
            <thead>
              <tr>
                <th class="ex-col-status"></th>
                <th class="ex-col-title ex-sortable" @click="toggleSort('title')">
                  Nosaukums <span class="ex-sort-icon">{{ sortIcon('title') }}</span>
                </th>
                <th class="ex-col-diff ex-sortable" @click="toggleSort('difficulty')">
                  Grūtība <span class="ex-sort-icon">{{ sortIcon('difficulty') }}</span>
                </th>
                <th class="ex-col-language">
                  Rekomendējamā valoda
                </th>
                <th class="ex-col-tests ex-sortable" @click="toggleSort('tests')">
                  Testi <span class="ex-sort-icon">{{ sortIcon('tests') }}</span>
                </th>
                <th class="ex-col-submissions">Iesniegumi</th>
                <th class="ex-col-success">Izpildīja</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="ex in pagedExercises"
                :key="ex.id"
                class="ex-table-row"
                :class="{ 'ex-row-solved': ex.isSolved }"
                @click="goTo(ex.id)"
              >
                <td class="ex-col-status">
                  <span v-if="ex.isSolved" class="ex-solved-mark">✓</span>
                </td>
                <td class="ex-col-title">
                  <span class="ex-title-text">{{ ex.title }}</span>
                  <small class="ex-desc-preview">{{ ex.description }}</small>
                </td>
                <td class="ex-col-diff">
                  <span class="ex-diff-badge" :class="diffClass(ex.difficulty)">
                    {{ ex.difficulty }}
                  </span>
                </td>
                <td class="ex-col-language">
                  <span class="ex-language-tag">{{ languageLabel(ex.languageCode, ex.languageVersion) }}</span>
                </td>
                <td class="ex-col-tests">{{ ex.testCaseCount }}</td>
                <td class="ex-col-submissions">{{ ex.submissionCount }}</td>
                <td class="ex-col-success">
                  <strong>{{ ex.solvedAttemptPercent }}%</strong>
                  <small>{{ attemptLabel(ex) }}</small>
                </td>
              </tr>
            </tbody>
          </table>
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
