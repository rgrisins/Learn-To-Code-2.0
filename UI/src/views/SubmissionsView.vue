<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import {
  getSubmissions,
  type ExerciseSubmission,
  type TestCaseResult,
} from '../services/exercises'

const submissions = ref<ExerciseSubmission[]>([])
const loading = ref(true)
const loadError = ref<string | null>(null)
const statusFilter = ref<'all' | 'passed' | 'failed'>('all')
const search = ref('')
const currentPage = ref(1)
const testPages = ref<Record<number, number>>({})

const submissionsPerPage = 5
const testsPerPage = 5

onMounted(async () => {
  try {
    submissions.value = await getSubmissions()
  } catch (e: any) {
    loadError.value = e.message ?? 'Neizdevās ielādēt iesniegumus.'
  } finally {
    loading.value = false
  }
})

const filteredSubmissions = computed(() => {
  let list = submissions.value
  const query = search.value.trim().toLowerCase()

  if (statusFilter.value === 'passed') {
    list = list.filter((submission) => submission.status === 'Passed')
  }

  if (statusFilter.value === 'failed') {
    list = list.filter((submission) => submission.status !== 'Passed')
  }

  if (query) {
    list = list.filter((submission) => {
      const haystack = [
        submission.exerciseTitle,
        submission.difficulty,
        submission.languageCode,
        statusLabel(submission.status),
        `${submission.testsPassed}/${submission.testsTotal}`,
      ].join(' ').toLowerCase()

      return haystack.includes(query)
    })
  }

  return list
})

const pageCount = computed(() => Math.max(1, Math.ceil(filteredSubmissions.value.length / submissionsPerPage)))

const pagedSubmissions = computed(() => {
  const start = (currentPage.value - 1) * submissionsPerPage
  return filteredSubmissions.value.slice(start, start + submissionsPerPage)
})

const pageSummary = computed(() => {
  if (!filteredSubmissions.value.length) return '0 no 0'

  const start = (currentPage.value - 1) * submissionsPerPage + 1
  const end = Math.min(currentPage.value * submissionsPerPage, filteredSubmissions.value.length)
  return `${start}-${end} no ${filteredSubmissions.value.length}`
})

const passedCount = computed(() => submissions.value.filter((submission) => submission.status === 'Passed').length)
const failedCount = computed(() => submissions.value.filter((submission) => submission.status !== 'Passed').length)

watch([search, statusFilter], () => {
  currentPage.value = 1
})

watch(pageCount, (count) => {
  if (currentPage.value > count) {
    currentPage.value = count
  }
})

function statusLabel(status: string) {
  if (status === 'Passed') return 'Izpildīts'
  if (status === 'Failed') return 'Neizpildīts'
  if (status === 'TimedOut') return 'Laiks beidzies'
  if (status === 'Running') return 'Pārbauda'
  return 'Kļūda'
}

function statusClass(status: string) {
  if (status === 'Passed') return 'ex-status-passed'
  if (status === 'Failed') return 'ex-status-failed'
  return 'ex-status-error'
}

function diffClass(difficulty: string) {
  const value = difficulty.toLowerCase()
  if (value === 'viegls' || value === 'easy') return 'diff-easy'
  if (value === 'grūts' || value === 'hard') return 'diff-hard'
  return 'diff-medium'
}

function languageLabel(languageCode: string) {
  const code = languageCode.toLowerCase()
  if (code === 'python') return 'Python'
  if (code === 'java') return 'Java'
  return languageCode
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('lv-LV', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function passPercent(submission: ExerciseSubmission) {
  if (submission.testsTotal <= 0) return 0
  return Math.round((submission.testsPassed / submission.testsTotal) * 100)
}

function testTitle(tc: TestCaseResult) {
  return `Tests ${tc.orderIndex + 1}${tc.isHidden ? ' (slēpts)' : ''}`
}

function testPageCount(submission: ExerciseSubmission) {
  return Math.max(1, Math.ceil(submission.testResults.length / testsPerPage))
}

function testPage(submission: ExerciseSubmission) {
  const page = testPages.value[submission.id] ?? 1
  return Math.min(Math.max(page, 1), testPageCount(submission))
}

function setTestPage(submission: ExerciseSubmission, page: number) {
  testPages.value = {
    ...testPages.value,
    [submission.id]: Math.min(Math.max(page, 1), testPageCount(submission)),
  }
}

function pagedTestResults(submission: ExerciseSubmission) {
  const start = (testPage(submission) - 1) * testsPerPage
  return submission.testResults.slice(start, start + testsPerPage)
}
</script>

<template>
  <section class="submissions-view">
    <article class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 submissions-hero">
        <div>
          <h1 class="section-heading mb-0">Iesniegumi</h1>
        </div>

        <div class="submissions-summary">
          <div class="submissions-summary__item">
            <span>Kopā</span>
            <strong>{{ submissions.length }}</strong>
          </div>
          <div class="submissions-summary__item">
            <span>Veiksmīgi</span>
            <strong>{{ passedCount }}</strong>
          </div>
          <div class="submissions-summary__item">
            <span>Neveiksmīgi</span>
            <strong>{{ failedCount }}</strong>
          </div>
        </div>
      </div>
    </article>

    <article class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="submissions-toolbar">
          <input
            v-model="search"
            type="search"
            class="ex-search auth-input"
            placeholder="Meklēt iesniegumos..."
          />

          <div class="ex-filter-group">
            <button
              type="button"
              class="ex-filter-btn"
              :class="{ active: statusFilter === 'all' }"
              @click="statusFilter = 'all'"
            >
              Visi
            </button>
            <button
              type="button"
              class="ex-filter-btn"
              :class="{ active: statusFilter === 'passed' }"
              @click="statusFilter = 'passed'"
            >
              Veiksmīgi
            </button>
            <button
              type="button"
              class="ex-filter-btn"
              :class="{ active: statusFilter === 'failed' }"
              @click="statusFilter = 'failed'"
            >
              Neveiksmīgi
            </button>
          </div>
        </div>

        <div v-if="loading" class="submission-state">Ielādē...</div>
        <div v-else-if="loadError" class="submission-state submission-state--error">{{ loadError }}</div>
        <div v-else-if="!filteredSubmissions.length" class="submission-state">
          Nav iesniegumu, kas atbilst meklēšanai.
        </div>

        <template v-else>
          <div class="submissions-list">
            <article
              v-for="submission in pagedSubmissions"
              :key="submission.id"
              class="submission-item"
            >
              <div class="submission-item__header">
                <div class="submission-item__title">
                  <RouterLink :to="{ name: 'exercise', params: { id: submission.exerciseId } }">
                    {{ submission.exerciseTitle }}
                  </RouterLink>
                  <div class="submission-item__meta">
                    <span>{{ languageLabel(submission.languageCode) }}</span>
                    <span class="ex-diff-badge" :class="diffClass(submission.difficulty)">
                      {{ submission.difficulty }}
                    </span>
                    <span>{{ formatDate(submission.submittedAtUtc) }}</span>
                  </div>
                </div>

                <span class="ex-status-badge" :class="statusClass(submission.status)">
                  {{ statusLabel(submission.status) }}
                </span>
              </div>

              <div class="submission-score">
                <div class="submission-score__row">
                  <span>{{ submission.testsPassed }} / {{ submission.testsTotal }} testi veiksmīgi</span>
                  <strong>{{ passPercent(submission) }}%</strong>
                </div>
                <div class="submission-score__track">
                  <div class="submission-score__bar" :style="{ width: `${passPercent(submission)}%` }"></div>
                </div>
              </div>

              <div v-if="submission.errorMessage" class="ex-error-box">
                {{ submission.errorMessage }}
              </div>

              <details v-if="submission.testResults.length" class="submission-details">
                <summary>Testu detaļas</summary>

                <div class="submission-tests">
                  <div
                    v-for="(tc, index) in pagedTestResults(submission)"
                    :key="`${submission.id}-${testPage(submission)}-${index}`"
                    class="ex-test-row"
                    :class="tc.passed ? 'ex-test-pass' : 'ex-test-fail'"
                  >
                    <div class="ex-test-row-header">
                      <span>{{ testTitle(tc) }}</span>
                      <span class="ex-test-icon">{{ tc.passed ? '✓' : '✗' }}</span>
                    </div>

                    <template v-if="!tc.passed">
                      <div v-if="tc.errorMessage" class="ex-test-error mt-1">{{ tc.errorMessage }}</div>
                      <template v-else-if="!tc.isHidden">
                        <div class="ex-test-io mt-1">
                          <span class="ex-io-label">Gaidīts:</span>
                          <pre class="ex-io-pre">{{ tc.expectedOutput }}</pre>
                        </div>
                        <div class="ex-test-io">
                          <span class="ex-io-label">Iegūts:</span>
                          <pre class="ex-io-pre">{{ tc.actualOutput }}</pre>
                        </div>
                      </template>
                    </template>
                  </div>
                </div>

                <div v-if="testPageCount(submission) > 1" class="pagination-row pagination-row--compact mt-2">
                  <span>{{ testPage(submission) }} / {{ testPageCount(submission) }}</span>
                  <div class="pagination-controls">
                    <button
                      type="button"
                      class="pagination-btn"
                      :disabled="testPage(submission) === 1"
                      @click="setTestPage(submission, testPage(submission) - 1)"
                    >
                      Iepriekšējā
                    </button>
                    <button
                      type="button"
                      class="pagination-btn"
                      :disabled="testPage(submission) === testPageCount(submission)"
                      @click="setTestPage(submission, testPage(submission) + 1)"
                    >
                      Nākamā
                    </button>
                  </div>
                </div>
              </details>
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
    </article>
  </section>
</template>
