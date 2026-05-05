<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  getMyRepresentations,
  getRepresentationByName,
  getRepresentationMembers,
  getRepresentations,
  joinRepresentation,
  localizedRoleLabel,
  type Representation,
  type RepresentationMember,
} from '../services/representations'
import { authState, getCurrentProfile } from '../services/auth'

type SortDirection = 'asc' | 'desc'

const route = useRoute()
const router = useRouter()

const representation = ref<Representation | null>(null)
const allRepresentations = ref<Representation[]>([])
const myRepresentations = ref<Representation[]>([])
const members = ref<RepresentationMember[]>([])
const isLoading = ref(false)
const isLoadingMembers = ref(false)
const isJoining = ref(false)
const errorMessage = ref('')
const joinNotice = ref('')

const memberSearch = ref('')
const memberSortDirection = ref<SortDirection>('desc')

const representationName = computed(() => String(route.params.name ?? '').trim())

onMounted(load)
watch(representationName, () => {
  void load()
})

async function load() {
  errorMessage.value = ''
  representation.value = null
  members.value = []

  if (!representationName.value) {
    errorMessage.value = 'Pārstāvniecība nav atrasta.'
    return
  }

  isLoading.value = true
  try {
    const [single, all, mine] = await Promise.all([
      getRepresentationByName(representationName.value),
      getRepresentations().catch(() => [] as Representation[]),
      authState.user
        ? getMyRepresentations().catch(() => [] as Representation[])
        : Promise.resolve([] as Representation[]),
    ])
    representation.value = single
    allRepresentations.value = all
    myRepresentations.value = mine
    await loadMembers(single.id)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Neizdevās ielādēt pārstāvniecību.'
  } finally {
    isLoading.value = false
  }
}

const canJoin = computed(() => {
  if (!authState.user || !representation.value) return false
  if (representation.value.isMember) return false
  if (representation.value.hasPendingJoinRequest) return false
  return myRepresentations.value.length === 0
})

async function joinThis() {
  const current = representation.value
  if (!current) return

  isJoining.value = true
  errorMessage.value = ''
  joinNotice.value = ''
  try {
    const result = await joinRepresentation(current.id)
    if (result.kind === 'pending') {
      joinNotice.value = result.message
      // Refresh single rep so hasPendingJoinRequest updates
      representation.value = await getRepresentationByName(representationName.value)
    } else {
      // Successfully joined — refresh page state and auth session
      try {
        const refreshed = await getCurrentProfile()
        authState.user = refreshed
      } catch { /* ignore */ }
      await load()
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Neizdevās pievienoties pārstāvniecībai.'
  } finally {
    isJoining.value = false
  }
}

const rankInfo = computed(() => {
  const current = representation.value
  if (!current) return null
  if (isRankingExcludedRepresentation(current)) return null

  const ranked = allRepresentations.value
    .filter((item) => !isRankingExcludedRepresentation(item))
    .sort((first, second) => {
      if (second.averageRating !== first.averageRating) {
        return second.averageRating - first.averageRating
      }
      return second.memberCount - first.memberCount
    })

  if (!ranked.length) return null

  const index = ranked.findIndex((item) => item.id === current.id)
  if (index < 0) return null
  return { rank: index + 1, total: ranked.length }
})

function normalizeSearch(value?: string | null) {
  return value?.trim().toLocaleLowerCase('lv-LV') ?? ''
}

function isRankingExcludedRepresentation(item: Pick<Representation, 'name'>) {
  return normalizeSearch(item.name) === 'learntocode'
}

function rankChipClass(index: number) {
  if (index === 0) return 'representations-stat-chip--gold'
  if (index === 1) return 'representations-stat-chip--silver'
  if (index === 2) return 'representations-stat-chip--bronze'
  return ''
}

async function loadMembers(id: number) {
  isLoadingMembers.value = true
  try {
    members.value = await getRepresentationMembers(id)
  } catch {
    members.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

function getMemberName(member: RepresentationMember) {
  return member.username?.trim() || member.fullName?.trim() || `Lietotājs #${member.userId}`
}

const topMembers = computed(() => {
  const query = normalizeSearch(memberSearch.value)
  return [...members.value]
    .filter((member) => {
      if (!query) return true
      return (
        normalizeSearch(member.username).includes(query) ||
        normalizeSearch(member.fullName).includes(query) ||
        normalizeSearch(member.role).includes(query)
      )
    })
    .sort((first, second) => {
      const ratingDifference =
        memberSortDirection.value === 'asc'
          ? first.rating - second.rating
          : second.rating - first.rating
      if (ratingDifference !== 0) return ratingDifference
      return getMemberName(first).localeCompare(getMemberName(second), 'lv')
    })
})

function setMemberSortDirection(direction: SortDirection) {
  memberSortDirection.value = direction
}

function memberRankClass(index: number) {
  if (index === 0) return 'representation-member__rank--gold'
  if (index === 1) return 'representation-member__rank--silver'
  if (index === 2) return 'representation-member__rank--bronze'
  return ''
}

function formatDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('lv-LV', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function goBack() {
  if (window.history.length > 1) {
    router.back()
  } else {
    void router.push({ name: 'ratings' })
  }
}
</script>

<template>
  <section class="representations-view">
    <div class="mb-1">
      <button class="app-back-link" type="button" @click="goBack">
        <span aria-hidden="true">←</span>
        Iepriekšējā lapa
      </button>
    </div>

    <div v-if="isLoading" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Ielādē pārstāvniecību...</div>
    </div>

    <div v-else-if="errorMessage" class="alert alert-danger">
      {{ errorMessage }}
    </div>

    <template v-else-if="representation">
      <header class="content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="theory-header exercises-header">
            <span class="page-title-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>
            </span>
            <div class="theory-heading-copy">
              <h1 class="section-heading mb-1">
                {{ representation.name }}
                <span
                  class="ex-language-chip representation-visibility-chip"
                  :class="representation.isPublic ? 'representation-visibility-chip--public' : 'representation-visibility-chip--private'"
                >
                  {{ representation.isPublic ? 'Publiska' : 'Privāta' }}
                </span>
              </h1>
              <p class="representations-lead mb-0">
                {{ representation.description || 'Bez apraksta' }}
              </p>
            </div>
            <div class="exercises-header__badges representations-summary">
              <button
                v-if="canJoin"
                type="button"
                class="btn btn-primary"
                :disabled="isJoining"
                @click="joinThis"
              >
                <template v-if="isJoining">Sūta...</template>
                <template v-else-if="representation.isPublic">Pievienoties</template>
                <template v-else>Sūtīt pieprasījumu</template>
              </button>
              <span v-else-if="representation.hasPendingJoinRequest" class="representations-stat-chip">
                <span>Statuss</span>
                <strong>Pieprasījums iesniegts</strong>
              </span>
              <div
                v-if="rankInfo"
                class="representations-stat-chip"
                :class="rankChipClass(rankInfo.rank - 1)"
              >
                <span>Vieta reitingā</span>
                <strong>{{ rankInfo.rank }}<small>/{{ rankInfo.total }}</small></strong>
              </div>
              <div v-else class="representations-stat-chip">
                <span>Izveidota</span>
                <strong>{{ formatDate(representation.createdAtUtc) }}</strong>
              </div>
            </div>
          </div>
        </div>
      </header>

      <div v-if="joinNotice" class="alert alert-info">
        {{ joinNotice }}
      </div>

      <section class="content-panel card border-primary-subtle representations-stats-panel">
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 21v-2a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
            </span>
            <h2 class="section-heading section-heading--caps mb-0">Statistika</h2>
          </div>

          <div class="representation-detail">
            <div class="representation-metrics">
              <div>
                <span>Dalībnieki</span>
                <strong>{{ representation.memberCount }}</strong>
              </div>
              <div>
                <span>Vidējais reitings</span>
                <strong>{{ representation.averageRating }}</strong>
              </div>
              <div>
                <span>Atrisināti kopā</span>
                <strong>{{ representation.exerciseSolved }}</strong>
              </div>
              <div>
                <span>Atrisināti pēdējās 7 dienās</span>
                <strong>{{ representation.exerciseSolvedLast7Days ?? 0 }}</strong>
              </div>
            </div>

            <div class="representations-panel-header representations-panel-header--sub">
              <h3 class="section-heading section-heading--caps section-heading--sm mb-0">Dalībnieku tops</h3>
              <span v-if="isLoadingMembers" class="sidebar-link-description">Ielādē...</span>
            </div>

            <div class="rating-controls__grid rating-controls__grid--compact">
              <label class="rating-control">
                <span>Meklēt dalībnieku</span>
                <input
                  v-model="memberSearch"
                  class="form-control auth-input"
                  type="search"
                  placeholder="Vārds, lietotājvārds vai loma"
                />
              </label>
              <div class="rating-sort" role="group" aria-label="Kārtot dalībniekus pēc reitinga">
                <span>Kārtot</span>
                <div class="ex-filter-group">
                  <button
                    class="ex-filter-btn"
                    :class="{ active: memberSortDirection === 'desc' }"
                    type="button"
                    @click="setMemberSortDirection('desc')"
                  >
                    Dilstoši
                  </button>
                  <button
                    class="ex-filter-btn"
                    :class="{ active: memberSortDirection === 'asc' }"
                    type="button"
                    @click="setMemberSortDirection('asc')"
                  >
                    Augoši
                  </button>
                </div>
              </div>
            </div>

            <div v-if="!topMembers.length && !isLoadingMembers" class="representations-empty">
              {{ memberSearch ? 'Nav dalībnieku, kas atbilst meklēšanai.' : 'Dalībnieku dati vēl nav pieejami.' }}
            </div>

            <div v-else class="representation-member-list">
              <component
                :is="member.username ? 'router-link' : 'div'"
                v-for="(member, idx) in topMembers"
                :key="member.userId"
                class="representation-member"
                :to="member.username ? { name: 'public-profile', params: { username: member.username } } : undefined"
              >
                <span class="representation-member__rank" :class="memberRankClass(idx)">{{ idx + 1 }}</span>
                <div class="representation-member__name">
                  <span>{{ getMemberName(member) }}</span>
                  <small>{{ localizedRoleLabel(member.role) }}</small>
                </div>
                <strong>{{ member.rating }}</strong>
              </component>
            </div>
          </div>
        </div>
      </section>
    </template>
  </section>
</template>
