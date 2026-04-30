<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getRatingUsers } from '../services/ratings'
import type { RatingUser } from '../services/ratings'

type SortDirection = 'asc' | 'desc'

const ratingUsers = ref<RatingUser[]>([])
const isLoadingRatings = ref(false)
const ratingError = ref('')
const nameSearch = ref('')
const institutionSearch = ref('')
const sortDirection = ref<SortDirection>('desc')

const showChart = ref(true)

const rankedUsers = computed(() =>
  [...ratingUsers.value]
    .filter((user) => user.role.toLowerCase() !== 'administrators')
    .sort((firstUser, secondUser) => {
      const ratingDifference = secondUser.rating - firstUser.rating
      if (ratingDifference !== 0) return ratingDifference

      return getDisplayName(firstUser).localeCompare(getDisplayName(secondUser), 'lv')
    }),
)

const currentTopUsers = computed(() => rankedUsers.value.slice(0, 10))
const visibleTopUsers = computed(() =>
  currentTopUsers.value
    .filter((user) => {
      const nameQuery = normalizeSearch(nameSearch.value)
      const institutionQuery = normalizeSearch(institutionSearch.value)
      const matchesName =
        !nameQuery ||
        normalizeSearch(user.username).includes(nameQuery) ||
        normalizeSearch(user.fullName).includes(nameQuery)
      const matchesInstitution =
        !institutionQuery || normalizeSearch(user.educationInstitution).includes(institutionQuery)

      return matchesName && matchesInstitution
    })
    .sort((firstUser, secondUser) => {
      const ratingDifference =
        sortDirection.value === 'asc'
          ? firstUser.rating - secondUser.rating
          : secondUser.rating - firstUser.rating

      if (ratingDifference !== 0) return ratingDifference

      return getDisplayName(firstUser).localeCompare(getDisplayName(secondUser), 'lv')
    }),
)
const topRating = computed(() => Math.max(0, ...currentTopUsers.value.map((user) => user.rating)))
const averageRating = computed(() => {
  if (!rankedUsers.value.length) return '0'

  const sum = rankedUsers.value.reduce((total, user) => total + user.rating, 0)
  const average = sum / rankedUsers.value.length

  return new Intl.NumberFormat('lv-LV', {
    maximumFractionDigits: 1,
  }).format(average)
})
const chartUsers = computed(() => visibleTopUsers.value)
const chartWidth = 900
const chartHeight = 260
const chartPadding = {
  top: 18,
  right: 64,
  bottom: 30,
  left: 148,
}
const chartInnerWidth = chartWidth - chartPadding.left - chartPadding.right
const chartInnerHeight = chartHeight - chartPadding.top - chartPadding.bottom
const chartMinRating = computed(() => Math.min(...chartUsers.value.map((user) => user.rating)))
const chartMaxRating = computed(() => Math.max(...chartUsers.value.map((user) => user.rating)))
const chartScaleMin = computed(() => {
  if (!chartUsers.value.length) return 0

  const min = chartMinRating.value
  const max = chartMaxRating.value

  if (max === min) {
    return Math.max(0, min - 100)
  }

  const padding = Math.max(10, Math.round((max - min) * 0.15))
  return Math.max(0, min - padding)
})
const chartScaleMax = computed(() => {
  if (!chartUsers.value.length) return 0

  const max = chartMaxRating.value
  if (max === chartScaleMin.value) {
    return max + 100
  }

  return max
})
const chartTicks = computed(() => {
  if (!chartUsers.value.length) return []

  const max = chartScaleMax.value
  const min = chartScaleMin.value
  const mid = Math.round((max + min) / 2)

  return Array.from(new Set([max, mid, min]))
})
const chartColors = ['#f97316', '#14b8a6', '#facc15', '#60a5fa', '#fb7185', '#a78bfa', '#34d399', '#f472b6', '#38bdf8', '#f59e0b']

onMounted(() => {
  void loadRatings()
})

async function loadRatings() {
  isLoadingRatings.value = true
  ratingError.value = ''

  try {
    ratingUsers.value = await getRatingUsers()
  } catch (error) {
    ratingError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt reitingu.'
  } finally {
    isLoadingRatings.value = false
  }
}

function getDisplayName(user: RatingUser) {
  return user.username?.trim() || `Lietotājs #${user.id}`
}

function getShortChartLabel(user: RatingUser) {
  const name = getDisplayName(user)
  return name.length > 14 ? `${name.slice(0, 13)}...` : name
}

function getCurrentRank(user: RatingUser) {
  return currentTopUsers.value.findIndex((topUser) => topUser.id === user.id) + 1
}

function normalizeSearch(value?: string | null) {
  return value?.trim().toLocaleLowerCase('lv-LV') ?? ''
}

function getChartY(index: number) {
  if (chartUsers.value.length <= 1) {
    return chartPadding.top + chartInnerHeight / 2
  }

  return chartPadding.top + (index / (chartUsers.value.length - 1)) * chartInnerHeight
}

function getRatingX(rating: number) {
  const min = chartScaleMin.value
  const max = chartScaleMax.value

  if (max === min) {
    return chartPadding.left + chartInnerWidth
  }

  return chartPadding.left + ((rating - min) / (max - min)) * chartInnerWidth
}

function getRatingLabelX(rating: number) {
  return Math.min(getRatingX(rating) + 8, chartWidth - 22)
}

function getChartColor(index: number) {
  return chartColors[index % chartColors.length]
}

function setSortDirection(direction: SortDirection) {
  sortDirection.value = direction
}
</script>

<template>
  <section class="rating-view">
    <header class="rating-hero content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 p-xl-5">
        <div class="rating-hero__content">
          <div>
            <h1 class="section-heading mb-3">Lietotāju reitings</h1>
            <p class="rating-lead mb-0">
              Tekošais Top 10 pēc reitinga; administratori šajā skatā netiek rādīti.
            </p>
          </div>

          <div class="rating-summary" aria-label="Reitinga kopsavilkums">
            <div class="rating-summary__item">
              <span>Augstākais</span>
              <strong>{{ topRating }}</strong>
            </div>
            <div class="rating-summary__item">
              <span>Vidējais</span>
              <strong>{{ averageRating }}</strong>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div v-if="isLoadingRatings" class="rating-state content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Ielādē reitingu...</div>
    </div>

    <div v-else-if="ratingError" class="rating-state rating-state--error content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">{{ ratingError }}</div>
    </div>

    <div v-else-if="!currentTopUsers.length" class="rating-state content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Reitingā vēl nav lietotāju.</div>
    </div>

    <div v-else class="rating-view__body">
      <article v-if="showChart" class="rating-panel content-panel card border-primary-subtle">
        <div class="rating-chart-card-header card-body p-3 p-lg-4">
          <span></span>
          <h2 class="rating-panel__title mb-0">Aktīvākie lietotāji</h2>
          <button class="rating-chart-toggle" type="button" @click="showChart = false">
            Paslēpt
          </button>
        </div>

        <div class="px-3 px-lg-4 pb-3 pb-lg-4">
          <div v-if="!visibleTopUsers.length" class="rating-state">
            Top 10 nav lietotāju, kas atbilst meklēšanai.
          </div>

          <div v-else class="rating-line-chart-wrap mx-n3 mx-lg-n4">
            <svg class="rating-line-chart" :viewBox="`0 0 ${chartWidth} ${chartHeight}`" role="img" aria-label="Aktīvāko lietotāju reitinga grafiks">
              <g class="rating-line-chart__grid">
                <line
                  v-for="tick in chartTicks"
                  :key="tick"
                  :x1="getRatingX(tick)"
                  :x2="getRatingX(tick)"
                  :y1="chartPadding.top"
                  :y2="chartPadding.top + chartInnerHeight"
                />
              </g>

              <g class="rating-line-chart__axis">
                <text
                  v-for="tick in chartTicks"
                  :key="`label-${tick}`"
                  :x="getRatingX(tick)"
                  :y="chartHeight - 8"
                  text-anchor="middle"
                >
                  {{ tick }}
                </text>
              </g>

              <g v-for="(user, index) in chartUsers" :key="user.id" class="rating-line-chart__point">
                <text
                  class="rating-line-chart__user"
                  x="10"
                  :y="getChartY(index) + 4"
                >
                  #{{ getCurrentRank(user) }} {{ getShortChartLabel(user) }}
                </text>
                <line
                  class="rating-line-chart__rating-line"
                  :x1="chartPadding.left"
                  :x2="getRatingX(user.rating)"
                  :y1="getChartY(index)"
                  :y2="getChartY(index)"
                  :stroke="getChartColor(index)"
                />
                <circle
                  :cx="getRatingX(user.rating)"
                  :cy="getChartY(index)"
                  r="5"
                  :fill="getChartColor(index)"
                />
                <text
                  class="rating-line-chart__rating"
                  :x="getRatingLabelX(user.rating)"
                  :y="getChartY(index) + 4"
                >
                  {{ user.rating }}
                </text>
              </g>
            </svg>
          </div>
        </div>
      </article>

      <div v-else class="rating-chart-collapsed content-panel card border-primary-subtle">
        <div class="rating-chart-card-header card-body p-3 p-lg-4">
          <span></span>
          <h2 class="rating-panel__title mb-0">Aktīvākie lietotāji</h2>
          <button class="rating-chart-toggle" type="button" @click="showChart = true">
            Rādīt
          </button>
        </div>
      </div>

      <section class="rating-controls content-panel card border-primary-subtle" aria-label="Reitinga filtri">
        <div class="card-body p-3 p-lg-4">
          <div class="rating-controls__grid">
            <label class="rating-control">
              <span>Lietotāja vārds</span>
              <input
                v-model="nameSearch"
                class="form-control auth-input"
                type="search"
                placeholder="Meklēt pēc vārda"
              />
            </label>

            <label class="rating-control">
              <span>Izglītības iestāde</span>
              <input
                v-model="institutionSearch"
                class="form-control auth-input"
                type="search"
                placeholder="Meklēt pēc iestādes"
              />
            </label>

            <div class="rating-sort" role="group" aria-label="Kārtot pēc reitinga">
              <span>Kārtot</span>
              <div class="ex-filter-group">
                <button
                  class="ex-filter-btn"
                  :class="{ active: sortDirection === 'desc' }"
                  type="button"
                  @click="setSortDirection('desc')"
                >
                  Dilstoši
                </button>
                <button
                  class="ex-filter-btn"
                  :class="{ active: sortDirection === 'asc' }"
                  type="button"
                  @click="setSortDirection('asc')"
                >
                  Augoši
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      <article class="rating-panel content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="rating-panel__header">
            <h2 class="rating-panel__title mb-0">Top 10 lietotāji</h2>
          </div>

          <div v-if="!visibleTopUsers.length" class="rating-state">
            Top 10 nav lietotāju, kas atbilst meklēšanai.
          </div>

          <div v-else class="rating-table-wrap">
            <table class="table rating-table align-middle mb-0">
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col">Lietotājs</th>
                  <th scope="col">Iestāde</th>
                  <th scope="col">Loma</th>
                  <th scope="col" class="text-end">Reitings</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="user in visibleTopUsers" :key="user.id">
                  <td class="rating-table__rank">
                    {{ getCurrentRank(user) }}
                  </td>
                  <td>
                    <div class="rating-user-cell">
                      <router-link class="rating-user-link" :to="{ name: 'public-profile', params: { id: user.id } }">
                        {{ getDisplayName(user) }}
                      </router-link>
                    </div>
                  </td>
                  <td>{{ user.educationInstitution || 'Nav norādīta' }}</td>
                  <td>{{ user.role }}</td>
                  <td class="rating-table__score text-end">{{ user.rating }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </article>
    </div>
  </section>
</template>
