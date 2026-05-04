<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { getRatingRepresentations, getRatingUsers, type RatingRepresentation, type RatingUser } from '../services/ratings'

type RatingTab = 'users' | 'representations'
type SortDirection = 'asc' | 'desc'

const PAGE_SIZE = 15

const ratingUsers = ref<RatingUser[]>([])
const ratingRepresentations = ref<RatingRepresentation[]>([])
const isLoadingRatings = ref(false)
const ratingError = ref('')
const activeTab = ref<RatingTab>('users')
const nameSearch = ref('')
const representationSearch = ref('')
const sortDirection = ref<SortDirection>('desc')
const userPage = ref(1)
const representationPage = ref(1)

const rankedUsers = computed(() =>
  [...ratingUsers.value]
    .filter((user) => user.role.toLowerCase() !== 'administrators')
    .sort((firstUser, secondUser) => {
      return secondUser.rating - firstUser.rating
    }),
)

const filteredSortedUsers = computed(() =>
  rankedUsers.value
    .filter((user) => {
      const query = normalizeSearch(nameSearch.value)
      if (!query) return true

      return (
        normalizeSearch(user.username).includes(query) ||
        normalizeSearch(user.fullName).includes(query) ||
        normalizeSearch(user.representation).includes(query)
      )
    })
    .sort((firstUser, secondUser) => {
      const ratingDifference =
        sortDirection.value === 'asc'
          ? firstUser.rating - secondUser.rating
          : secondUser.rating - firstUser.rating

      return ratingDifference
    }),
)

const userRankById = computed(
  () => new Map(rankedUsers.value.map((user, index) => [user.id, index + 1])),
)

const rankedRepresentations = computed(() =>
  [...ratingRepresentations.value].sort((first, second) => {
    return second.averageRating - first.averageRating
  }),
)

const filteredSortedRepresentations = computed(() =>
  rankedRepresentations.value
    .filter((representation) => {
      const query = normalizeSearch(representationSearch.value)
      if (!query) return true

      return (
        normalizeSearch(representation.name).includes(query) ||
        normalizeSearch(representation.description).includes(query)
      )
    })
    .sort((first, second) => {
      const ratingDifference =
        sortDirection.value === 'asc'
          ? first.averageRating - second.averageRating
          : second.averageRating - first.averageRating

      return ratingDifference
    }),
)

const representationRankById = computed(
  () => new Map(rankedRepresentations.value.map((representation, index) => [representation.id, index + 1])),
)

const userTotalPages = computed(() =>
  Math.max(1, Math.ceil(filteredSortedUsers.value.length / PAGE_SIZE)),
)

const representationTotalPages = computed(() =>
  Math.max(1, Math.ceil(filteredSortedRepresentations.value.length / PAGE_SIZE)),
)

const pagedUsers = computed(() => {
  const start = (userPage.value - 1) * PAGE_SIZE
  return filteredSortedUsers.value.slice(start, start + PAGE_SIZE)
})

const pagedRepresentations = computed(() => {
  const start = (representationPage.value - 1) * PAGE_SIZE
  return filteredSortedRepresentations.value.slice(start, start + PAGE_SIZE)
})

const topUsersChart = computed(() =>
  rankedUsers.value
    .slice()
    .sort((a, b) => b.rating - a.rating)
    .slice(0, 10),
)

const topRepresentationsChart = computed(() =>
  rankedRepresentations.value.slice(0, 10),
)

const topRating = computed(() => Math.max(0, ...rankedUsers.value.map((user) => user.rating)))
const topRepresentationRating = computed(() =>
  Math.max(0, ...ratingRepresentations.value.map((representation) => representation.averageRating)),
)
const averageRating = computed(() => formatAverage(rankedUsers.value.map((user) => user.rating)))
const averageRepresentationRating = computed(() =>
  formatAverage(ratingRepresentations.value.map((representation) => representation.averageRating)),
)

const userChartMax = computed(() =>
  topUsersChart.value.length ? Math.max(...topUsersChart.value.map((user) => user.rating)) : 1,
)

const representationChartMax = computed(() =>
  topRepresentationsChart.value.length
    ? Math.max(...topRepresentationsChart.value.map((rep) => rep.averageRating))
    : 1,
)

onMounted(() => {
  void loadRatings()
})

watch([nameSearch, sortDirection], () => {
  userPage.value = 1
})

watch([representationSearch, sortDirection], () => {
  representationPage.value = 1
})

async function loadRatings() {
  isLoadingRatings.value = true
  ratingError.value = ''

  try {
    const [users, representations] = await Promise.all([getRatingUsers(), getRatingRepresentations()])
    ratingUsers.value = users
    ratingRepresentations.value = representations
  } catch (error) {
    ratingError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt reitingu.'
  } finally {
    isLoadingRatings.value = false
  }
}

function getDisplayName(user: RatingUser) {
  return user.username?.trim() || user.fullName?.trim() || `Lietotājs #${user.id}`
}

function normalizeSearch(value?: string | null) {
  return value?.trim().toLocaleLowerCase('lv-LV') ?? ''
}

function setSortDirection(direction: SortDirection) {
  sortDirection.value = direction
}

function setActiveTab(tab: RatingTab) {
  activeTab.value = tab
  nameSearch.value = ''
  representationSearch.value = ''
  userPage.value = 1
  representationPage.value = 1
}

function formatAverage(values: number[]) {
  if (!values.length) return '0'

  const sum = values.reduce((total, value) => total + value, 0)
  return new Intl.NumberFormat('lv-LV', {
    maximumFractionDigits: 1,
  }).format(sum / values.length)
}

function rankMedalClass(index: number) {
  if (index === 0) return 'rank-pill--gold'
  if (index === 1) return 'rank-pill--silver'
  if (index === 2) return 'rank-pill--bronze'
  return ''
}

function userRank(user: RatingUser) {
  return userRankById.value.get(user.id) ?? 0
}

function representationRank(representation: RatingRepresentation) {
  return representationRankById.value.get(representation.id) ?? 0
}

function chartBarClass(index: number) {
  if (index === 0) return 'rating-chart__bar--gold'
  if (index === 1) return 'rating-chart__bar--silver'
  if (index === 2) return 'rating-chart__bar--bronze'
  return ''
}

function userBarPercent(rating: number) {
  if (userChartMax.value <= 0) return 0
  return Math.max(8, Math.round((rating / userChartMax.value) * 100))
}

function representationBarPercent(rating: number) {
  if (representationChartMax.value <= 0) return 0
  return Math.max(8, Math.round((rating / representationChartMax.value) * 100))
}

function changeUserPage(delta: number) {
  const next = userPage.value + delta
  if (next < 1 || next > userTotalPages.value) return
  userPage.value = next
}

function changeRepresentationPage(delta: number) {
  const next = representationPage.value + delta
  if (next < 1 || next > representationTotalPages.value) return
  representationPage.value = next
}

function localizedRoleLabel(role: string | null | undefined) {
  const value = (role ?? '').trim().toLowerCase()
  if (value === 'audzeknis') return 'Audzēknis'
  if (value === 'pedagogs') return 'Pedagogs'
  if (value === 'administrators') return 'Administrators'
  return role ?? ''
}
</script>

<template>
  <section class="representations-view rating-view">
    <header class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="theory-header exercises-header">
          <span class="page-title-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6" />
              <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18" />
              <path d="M4 22h16" />
              <path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22" />
              <path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22" />
              <path d="M18 2H6v7a6 6 0 0 0 12 0V2z" />
            </svg>
          </span>
          <div class="theory-heading-copy">
            <h1 class="section-heading mb-1">Reitings</h1>
            <p class="representations-lead mb-0">
              Salīdzini lietotāju progresu atsevišķi no pārstāvniecību kopējās statistikas.
            </p>
          </div>
          <div class="exercises-header__badges representations-summary">
            <div class="representations-stat-chip">
              <span>Augstākais</span>
              <strong>{{ activeTab === 'users' ? topRating : topRepresentationRating }}</strong>
            </div>
            <div class="representations-stat-chip">
              <span>Vidējais</span>
              <strong>{{ activeTab === 'users' ? averageRating : averageRepresentationRating }}</strong>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div v-if="isLoadingRatings" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Ielādē reitingu...</div>
    </div>

    <div v-else-if="ratingError" class="alert alert-danger">
      {{ ratingError }}
    </div>

    <template v-else>
      <section class="content-panel card border-primary-subtle" aria-label="Reitinga veids">
        <div class="card-body p-3 p-lg-4">
          <div class="ex-filter-group rating-tabs rating-tabs--full" role="tablist" aria-label="Reitinga veids">
            <button
              class="ex-filter-btn"
              :class="{ active: activeTab === 'users' }"
              type="button"
              @click="setActiveTab('users')"
            >
              Lietotāji
            </button>
            <button
              class="ex-filter-btn"
              :class="{ active: activeTab === 'representations' }"
              type="button"
              @click="setActiveTab('representations')"
            >
              Pārstāvniecības
            </button>
          </div>
        </div>
      </section>

      <!-- USERS -->
      <template v-if="activeTab === 'users'">
        <section
          v-if="topUsersChart.length"
          class="content-panel card border-primary-subtle"
        >
          <div class="card-body p-3 p-lg-4">
            <div class="representations-section-heading mb-3">
              <span class="representations-section-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="18" y1="20" x2="18" y2="10" />
                  <line x1="12" y1="20" x2="12" y2="4" />
                  <line x1="6" y1="20" x2="6" y2="14" />
                </svg>
              </span>
              <h2 class="section-heading section-heading--caps mb-0">Top 10 lietotāji</h2>
            </div>

            <div class="rating-chart">
              <div
                v-for="(user, idx) in topUsersChart"
                :key="user.id"
                class="rating-chart__row"
              >
                <span class="rank-pill" :class="rankMedalClass(idx)">{{ idx + 1 }}</span>
                <div class="rating-chart__bar-cell">
                  <component
                    :is="user.username ? 'router-link' : 'span'"
                    class="rating-chart__name"
                    :to="user.username ? { name: 'public-profile', params: { username: user.username } } : undefined"
                  >
                    {{ getDisplayName(user) }}
                  </component>
                  <div class="rating-chart__track">
                    <div
                      class="rating-chart__bar"
                      :class="chartBarClass(idx)"
                      :style="{ width: `${userBarPercent(user.rating)}%` }"
                    ></div>
                  </div>
                </div>
                <strong class="rating-chart__value">{{ user.rating }}</strong>
              </div>
            </div>
          </div>
        </section>

        <section class="content-panel card border-primary-subtle" aria-label="Meklēšana un kārtošana">
          <div class="card-body p-3 p-lg-4">
            <div class="rating-controls__grid rating-controls__grid--compact">
              <label class="rating-control">
                <span>Meklēt lietotāju</span>
                <input
                  v-model="nameSearch"
                  class="form-control auth-input"
                  type="search"
                  placeholder="Vārds, lietotājvārds vai pārstāvniecība"
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

        <section class="content-panel card border-primary-subtle">
          <div class="card-body p-3 p-lg-4">
            <div class="representations-section-heading mb-3">
              <span class="representations-section-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M3 21v-2a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </span>
              <h2 class="section-heading section-heading--caps mb-0">Visi lietotāji</h2>
            </div>

            <div v-if="!filteredSortedUsers.length" class="representations-empty">
              Nav lietotāju, kas atbilst meklēšanai.
            </div>

            <template v-else>
              <div class="rating-table-wrap">
                <table class="table rating-table align-middle mb-0">
                  <thead>
                    <tr>
                      <th scope="col">#</th>
                      <th scope="col">Lietotājs</th>
                      <th scope="col">Pārstāvniecība</th>
                      <th scope="col">Loma</th>
                      <th scope="col" class="text-end">Reitings</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="user in pagedUsers" :key="user.id">
                      <td class="rating-table__rank">
                        <span class="rank-pill" :class="rankMedalClass(userRank(user) - 1)">
                          {{ userRank(user) }}
                        </span>
                      </td>
                      <td>
                        <div class="rating-user-cell">
                          <router-link
                            v-if="user.username"
                            class="rating-user-link"
                            :to="{ name: 'public-profile', params: { username: user.username } }"
                          >
                            {{ getDisplayName(user) }}
                          </router-link>
                          <span v-else>{{ getDisplayName(user) }}</span>
                        </div>
                      </td>
                      <td>
                        <router-link
                          v-if="user.representation"
                          class="rating-user-link"
                          :to="{ name: 'representation-detail', params: { name: user.representation } }"
                        >
                          {{ user.representation }}
                        </router-link>
                        <span v-else>Nav norādīta</span>
                      </td>
                      <td>{{ localizedRoleLabel(user.role) }}</td>
                      <td class="rating-table__score text-end">{{ user.rating }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <nav v-if="userTotalPages > 1" class="rating-pagination" aria-label="Lapas">
                <button
                  type="button"
                  class="btn btn-outline-light btn-sm"
                  :disabled="userPage <= 1"
                  @click="changeUserPage(-1)"
                >
                  ← Iepriekšējā
                </button>
                <span class="rating-pagination__info">
                  Lapa <strong>{{ userPage }}</strong> no <strong>{{ userTotalPages }}</strong>
                  · {{ filteredSortedUsers.length }} lietotāji
                </span>
                <button
                  type="button"
                  class="btn btn-outline-light btn-sm"
                  :disabled="userPage >= userTotalPages"
                  @click="changeUserPage(1)"
                >
                  Nākamā →
                </button>
              </nav>
            </template>
          </div>
        </section>
      </template>

      <!-- REPRESENTATIONS -->
      <template v-else>
        <section
          v-if="topRepresentationsChart.length"
          class="content-panel card border-primary-subtle"
        >
          <div class="card-body p-3 p-lg-4">
            <div class="representations-section-heading mb-3">
              <span class="representations-section-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="18" y1="20" x2="18" y2="10" />
                  <line x1="12" y1="20" x2="12" y2="4" />
                  <line x1="6" y1="20" x2="6" y2="14" />
                </svg>
              </span>
              <h2 class="section-heading section-heading--caps mb-0">Top 10 pārstāvniecības</h2>
            </div>

            <div class="rating-chart">
              <div
                v-for="(representation, idx) in topRepresentationsChart"
                :key="representation.id"
                class="rating-chart__row"
              >
                <span class="rank-pill" :class="rankMedalClass(idx)">{{ idx + 1 }}</span>
                <div class="rating-chart__bar-cell">
                  <router-link
                    class="rating-chart__name"
                    :to="{ name: 'representation-detail', params: { name: representation.name } }"
                  >
                    {{ representation.name }}
                  </router-link>
                  <div class="rating-chart__track">
                    <div
                      class="rating-chart__bar"
                      :class="chartBarClass(idx)"
                      :style="{ width: `${representationBarPercent(representation.averageRating)}%` }"
                    ></div>
                  </div>
                </div>
                <strong class="rating-chart__value">{{ representation.averageRating }}</strong>
              </div>
            </div>
          </div>
        </section>

        <section class="content-panel card border-primary-subtle" aria-label="Meklēšana un kārtošana">
          <div class="card-body p-3 p-lg-4">
            <div class="rating-controls__grid rating-controls__grid--compact">
              <label class="rating-control">
                <span>Meklēt pārstāvniecību</span>
                <input
                  v-model="representationSearch"
                  class="form-control auth-input"
                  type="search"
                  placeholder="Nosaukums vai apraksts"
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

        <section class="content-panel card border-primary-subtle">
          <div class="card-body p-3 p-lg-4">
            <div class="representations-section-heading mb-3">
              <span class="representations-section-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
              </span>
              <h2 class="section-heading section-heading--caps mb-0">Visas pārstāvniecības</h2>
            </div>

            <div v-if="!filteredSortedRepresentations.length" class="representations-empty">
              Nav pārstāvniecību, kas atbilst meklēšanai.
            </div>

            <template v-else>
              <div class="rating-table-wrap">
                <table class="table rating-table align-middle mb-0">
                  <thead>
                    <tr>
                      <th scope="col">#</th>
                      <th scope="col">Pārstāvniecība</th>
                      <th scope="col" class="text-end">Dalībnieki</th>
                      <th scope="col" class="text-end">Vidējais reitings</th>
                      <th scope="col" class="text-end">Atrisināti</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="representation in pagedRepresentations" :key="representation.id">
                      <td class="rating-table__rank">
                        <span class="rank-pill" :class="rankMedalClass(representationRank(representation) - 1)">
                          {{ representationRank(representation) }}
                        </span>
                      </td>
                      <td>
                        <div class="rating-user-cell">
                          <router-link
                            class="rating-user-link"
                            :to="{ name: 'representation-detail', params: { name: representation.name } }"
                          >
                            <strong>{{ representation.name }}</strong>
                          </router-link>
                          <small>{{ representation.description || 'Bez apraksta' }}</small>
                        </div>
                      </td>
                      <td class="text-end">{{ representation.memberCount }}</td>
                      <td class="rating-table__score text-end">{{ representation.averageRating }}</td>
                      <td class="text-end">{{ representation.exerciseSolved }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <nav v-if="representationTotalPages > 1" class="rating-pagination" aria-label="Lapas">
                <button
                  type="button"
                  class="btn btn-outline-light btn-sm"
                  :disabled="representationPage <= 1"
                  @click="changeRepresentationPage(-1)"
                >
                  ← Iepriekšējā
                </button>
                <span class="rating-pagination__info">
                  Lapa <strong>{{ representationPage }}</strong> no <strong>{{ representationTotalPages }}</strong>
                  · {{ filteredSortedRepresentations.length }} pārstāvniecības
                </span>
                <button
                  type="button"
                  class="btn btn-outline-light btn-sm"
                  :disabled="representationPage >= representationTotalPages"
                  @click="changeRepresentationPage(1)"
                >
                  Nākamā →
                </button>
              </nav>
            </template>
          </div>
        </section>
      </template>
    </template>
  </section>
</template>
