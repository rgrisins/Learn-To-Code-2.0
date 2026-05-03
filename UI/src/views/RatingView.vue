<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getRatingRepresentations, getRatingUsers, type RatingRepresentation, type RatingUser } from '../services/ratings'

type RatingTab = 'users' | 'representations'
type SortDirection = 'asc' | 'desc'

const ratingUsers = ref<RatingUser[]>([])
const ratingRepresentations = ref<RatingRepresentation[]>([])
const isLoadingRatings = ref(false)
const ratingError = ref('')
const activeTab = ref<RatingTab>('users')
const nameSearch = ref('')
const representationSearch = ref('')
const sortDirection = ref<SortDirection>('desc')

const rankedUsers = computed(() =>
  [...ratingUsers.value]
    .filter((user) => user.role.toLowerCase() !== 'administrators')
    .sort((firstUser, secondUser) => {
      const ratingDifference = secondUser.rating - firstUser.rating
      if (ratingDifference !== 0) return ratingDifference
      return getDisplayName(firstUser).localeCompare(getDisplayName(secondUser), 'lv')
    }),
)

const visibleTopUsers = computed(() =>
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

      if (ratingDifference !== 0) return ratingDifference
      return getDisplayName(firstUser).localeCompare(getDisplayName(secondUser), 'lv')
    })
    .slice(0, 10),
)

const visibleTopRepresentations = computed(() =>
  [...ratingRepresentations.value]
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

      if (ratingDifference !== 0) return ratingDifference
      return first.name.localeCompare(second.name, 'lv')
    })
    .slice(0, 10),
)

const topRating = computed(() => Math.max(0, ...rankedUsers.value.map((user) => user.rating)))
const topRepresentationRating = computed(() =>
  Math.max(0, ...ratingRepresentations.value.map((representation) => representation.averageRating)),
)
const averageRating = computed(() => formatAverage(rankedUsers.value.map((user) => user.rating)))
const averageRepresentationRating = computed(() =>
  formatAverage(ratingRepresentations.value.map((representation) => representation.averageRating)),
)

onMounted(() => {
  void loadRatings()
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
}

function formatAverage(values: number[]) {
  if (!values.length) return '0'

  const sum = values.reduce((total, value) => total + value, 0)
  return new Intl.NumberFormat('lv-LV', {
    maximumFractionDigits: 1,
  }).format(sum / values.length)
}
</script>

<template>
  <section class="rating-view">
    <header class="rating-hero content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 p-xl-5">
        <div class="rating-hero__content">
          <div class="page-title-with-icon">
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
            <div>
              <h1 class="section-heading mb-3">Reitings</h1>
              <p class="rating-lead mb-0">
                Salīdzini lietotāju progresu atsevišķi no pārstāvniecību kopējās statistikas.
              </p>
            </div>
          </div>

          <div class="rating-summary" aria-label="Reitinga kopsavilkums">
            <div class="rating-summary__item">
              <span>{{ activeTab === 'users' ? 'Augstākais' : 'Top vidējais' }}</span>
              <strong>{{ activeTab === 'users' ? topRating : topRepresentationRating }}</strong>
            </div>
            <div class="rating-summary__item">
              <span>Vidējais</span>
              <strong>{{ activeTab === 'users' ? averageRating : averageRepresentationRating }}</strong>
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

    <div v-else class="rating-view__body">
      <section class="rating-controls content-panel card border-primary-subtle" aria-label="Reitinga filtri">
        <div class="card-body p-3 p-lg-4">
          <div class="rating-tabs mb-3" role="tablist" aria-label="Reitinga veids">
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

          <div class="rating-controls__grid rating-controls__grid--compact">
            <label v-if="activeTab === 'users'" class="rating-control">
              <span>Meklēt lietotāju</span>
              <input
                v-model="nameSearch"
                class="form-control auth-input"
                type="search"
                placeholder="Vārds, lietotājvārds vai pārstāvniecība"
              />
            </label>

            <label v-else class="rating-control">
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

      <article v-if="activeTab === 'users'" class="rating-panel content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="rating-panel__header">
            <h2 class="rating-panel__title mb-0">Top 10 lietotāji</h2>
          </div>

          <div v-if="!visibleTopUsers.length" class="rating-state">
            Nav lietotāju, kas atbilst meklēšanai.
          </div>

          <div v-else class="rating-table-wrap">
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
                <tr v-for="(user, index) in visibleTopUsers" :key="user.id">
                  <td class="rating-table__rank">{{ index + 1 }}</td>
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
                  <td>{{ user.representation || 'Nav norādīta' }}</td>
                  <td>{{ user.role }}</td>
                  <td class="rating-table__score text-end">{{ user.rating }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </article>

      <article v-else class="rating-panel content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="rating-panel__header">
            <h2 class="rating-panel__title mb-0">Top 10 pārstāvniecības</h2>
          </div>

          <div v-if="!visibleTopRepresentations.length" class="rating-state">
            Nav pārstāvniecību, kas atbilst meklēšanai.
          </div>

          <div v-else class="rating-table-wrap">
            <table class="table rating-table align-middle mb-0">
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col">Pārstāvniecība</th>
                  <th scope="col" class="text-end">Dalībnieki</th>
                  <th scope="col" class="text-end">Vidējais reitings</th>
                  <th scope="col" class="text-end">Teorija</th>
                  <th scope="col" class="text-end">Atrisināti</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(representation, index) in visibleTopRepresentations" :key="representation.id">
                  <td class="rating-table__rank">{{ index + 1 }}</td>
                  <td>
                    <div class="rating-user-cell">
                      <strong>{{ representation.name }}</strong>
                      <small>{{ representation.description || 'Bez apraksta' }}</small>
                    </div>
                  </td>
                  <td class="text-end">{{ representation.memberCount }}</td>
                  <td class="rating-table__score text-end">{{ representation.averageRating }}</td>
                  <td class="text-end">{{ representation.theoryProgressPercent }}%</td>
                  <td class="text-end">{{ representation.exerciseSolved }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </article>
    </div>
  </section>
</template>
