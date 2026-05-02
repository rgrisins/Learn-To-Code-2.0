<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import {
  createRepresentation,
  getMyRepresentations,
  getRepresentationMembers,
  getRepresentations,
  joinRepresentation,
  leaveRepresentation,
  type Representation,
  type RepresentationMember,
} from '../services/representations'
import { authState, getCurrentProfile } from '../services/auth'

const allRepresentations = ref<Representation[]>([])
const myRepresentations = ref<Representation[]>([])
const members = ref<RepresentationMember[]>([])
const selectedRepresentationId = ref<number | null>(null)
const isLoading = ref(false)
const isLoadingMembers = ref(false)
const isSaving = ref(false)
const isJoiningId = ref<number | null>(null)
const pageError = ref('')
const formError = ref('')

const createForm = reactive({
  name: '',
  description: '',
})

const selectedRepresentation = computed(() =>
  myRepresentations.value.find((representation) => representation.id === selectedRepresentationId.value)
    ?? myRepresentations.value[0]
    ?? null,
)

const availableRepresentations = computed(() =>
  allRepresentations.value
    .filter((representation) => !representation.isMember)
    .sort((first, second) => first.name.localeCompare(second.name, 'lv')),
)

const topMembers = computed(() =>
  [...members.value]
    .sort((first, second) => {
      const ratingDifference = second.rating - first.rating
      if (ratingDifference !== 0) return ratingDifference
      return getMemberName(first).localeCompare(getMemberName(second), 'lv')
    })
    .slice(0, 8),
)

onMounted(() => {
  void loadRepresentations()
})

async function loadRepresentations() {
  isLoading.value = true
  pageError.value = ''

  try {
    const [all, mine] = await Promise.all([getRepresentations(), getMyRepresentations()])
    allRepresentations.value = all
    myRepresentations.value = mine

    if (!selectedRepresentationId.value && mine.length) {
      selectedRepresentationId.value = mine[0].id
    }

    await loadMembers()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt pārstāvniecības.'
  } finally {
    isLoading.value = false
  }
}

async function loadMembers() {
  const selected = selectedRepresentation.value
  members.value = []

  if (!selected) {
    return
  }

  isLoadingMembers.value = true
  try {
    members.value = await getRepresentationMembers(selected.id)
  } catch {
    members.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

async function submitCreate() {
  const name = createForm.name.trim()
  const description = createForm.description.trim()
  formError.value = ''

  if (name.length < 3) {
    formError.value = 'Nosaukumam jābūt vismaz 3 rakstzīmes garam.'
    return
  }

  isSaving.value = true
  try {
    const created = await createRepresentation({
      name,
      description: description || null,
    })
    createForm.name = ''
    createForm.description = ''
    selectedRepresentationId.value = created.id
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    formError.value = error instanceof Error ? error.message : 'Neizdevās izveidot pārstāvniecību.'
  } finally {
    isSaving.value = false
  }
}

async function join(id: number) {
  isJoiningId.value = id
  pageError.value = ''

  try {
    const joined = await joinRepresentation(id)
    selectedRepresentationId.value = joined.id
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās pievienoties pārstāvniecībai.'
  } finally {
    isJoiningId.value = null
  }
}

async function leaveCurrent() {
  const selected = selectedRepresentation.value
  if (!selected) return

  pageError.value = ''
  try {
    await leaveRepresentation(selected.id)
    selectedRepresentationId.value = null
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās izstāties no pārstāvniecības.'
  }
}

async function refreshProfileState() {
  try {
    authState.user = await getCurrentProfile()
  } catch {
    // The page data still reflects membership changes even if session refresh fails.
  }
}

function selectRepresentation(id: number) {
  selectedRepresentationId.value = id
  void loadMembers()
}

function getMemberName(member: RepresentationMember) {
  return member.username?.trim() || member.fullName?.trim() || `Lietotājs #${member.userId}`
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
</script>

<template>
  <section class="representations-view">
    <header class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 p-xl-5">
        <div class="representations-hero">
          <div>
            <p class="section-kicker mb-2">Pārstāvniecības</p>
            <h1 class="section-heading mb-2">Tavas grupas un to progress</h1>
            <p class="representations-lead mb-0">
              Izveido komandu, pievienojies esošai pārstāvniecībai un seko kopējam progresam vienā vietā.
            </p>
          </div>
          <div class="representations-summary">
            <div>
              <span>Manas</span>
              <strong>{{ myRepresentations.length }}</strong>
            </div>
            <div>
              <span>Pieejamas</span>
              <strong>{{ availableRepresentations.length }}</strong>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div v-if="isLoading" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Ielādē pārstāvniecības...</div>
    </div>

    <div v-else-if="pageError" class="alert alert-danger">
      {{ pageError }}
    </div>

    <div v-else class="representations-layout">
      <section class="content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="representations-title mb-3">Izveidot pārstāvniecību</h2>

          <form class="representation-form" @submit.prevent="submitCreate">
            <label class="rating-control">
              <span>Nosaukums</span>
              <input
                v-model="createForm.name"
                class="form-control auth-input"
                type="text"
                maxlength="160"
                placeholder="Piemēram, RVT 2. kurss"
              />
            </label>

            <label class="rating-control">
              <span>Apraksts</span>
              <textarea
                v-model="createForm.description"
                class="form-control auth-input"
                maxlength="800"
                rows="4"
                placeholder="Īsi par grupu, klasi vai organizāciju"
              ></textarea>
            </label>

            <div v-if="formError" class="invalid-feedback d-block">{{ formError }}</div>

            <button class="btn btn-primary" type="submit" :disabled="isSaving">
              {{ isSaving ? 'Veido...' : 'Izveidot' }}
            </button>
          </form>
        </div>
      </section>

      <section class="content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="representations-title mb-3">Pievienoties</h2>

          <div v-if="!availableRepresentations.length" class="profile-progress-empty">
            Nav citu pārstāvniecību, kurām pievienoties.
          </div>

          <div v-else class="representation-list">
            <article
              v-for="representation in availableRepresentations"
              :key="representation.id"
              class="representation-list-item"
            >
              <div>
                <strong>{{ representation.name }}</strong>
                <span>{{ representation.memberCount }} dalībnieki</span>
              </div>
              <button
                class="btn btn-outline-light btn-sm"
                type="button"
                :disabled="isJoiningId === representation.id"
                @click="join(representation.id)"
              >
                {{ isJoiningId === representation.id ? 'Pievienojas...' : 'Pievienoties' }}
              </button>
            </article>
          </div>
        </div>
      </section>

      <section class="content-panel card border-primary-subtle representations-stats-panel">
        <div class="card-body p-3 p-lg-4">
          <div class="representations-panel-header">
            <h2 class="representations-title mb-0">Mana pārstāvniecība</h2>
            <button
              v-if="selectedRepresentation"
              class="btn btn-outline-light btn-sm"
              type="button"
              @click="leaveCurrent"
            >
              Izstāties
            </button>
          </div>

          <div v-if="!myRepresentations.length" class="profile-progress-empty">
            Tu vēl neesi nevienā pārstāvniecībā.
          </div>

          <template v-else>
            <div class="representation-tabs" role="tablist" aria-label="Manas pārstāvniecības">
              <button
                v-for="representation in myRepresentations"
                :key="representation.id"
                class="ex-filter-btn"
                :class="{ active: selectedRepresentation?.id === representation.id }"
                type="button"
                @click="selectRepresentation(representation.id)"
              >
                {{ representation.name }}
              </button>
            </div>

            <div v-if="selectedRepresentation" class="representation-detail">
              <div class="representation-detail__heading">
                <div>
                  <strong>{{ selectedRepresentation.name }}</strong>
                  <span>{{ selectedRepresentation.description || 'Bez apraksta' }}</span>
                </div>
                <small>Izveidota {{ formatDate(selectedRepresentation.createdAtUtc) }}</small>
              </div>

              <div class="representation-metrics">
                <div>
                  <span>Dalībnieki</span>
                  <strong>{{ selectedRepresentation.memberCount }}</strong>
                </div>
                <div>
                  <span>Vidējais reitings</span>
                  <strong>{{ selectedRepresentation.averageRating }}</strong>
                </div>
                <div>
                  <span>Teorija</span>
                  <strong>{{ selectedRepresentation.theoryProgressPercent }}%</strong>
                </div>
                <div>
                  <span>Atrisināti</span>
                  <strong>{{ selectedRepresentation.exerciseSolved }}</strong>
                </div>
              </div>

              <div class="profile-progress-track profile-progress-track--large" aria-hidden="true">
                <span
                  class="profile-progress-bar"
                  :style="{ width: `${selectedRepresentation.theoryProgressPercent}%` }"
                ></span>
              </div>

              <div class="representations-panel-header">
                <h3 class="representations-subtitle mb-0">Dalībnieku tops</h3>
                <span v-if="isLoadingMembers" class="sidebar-link-description">Ielādē...</span>
              </div>

              <div v-if="!topMembers.length && !isLoadingMembers" class="profile-progress-empty">
                Dalībnieku dati vēl nav pieejami.
              </div>

              <div v-else class="representation-member-list">
                <router-link
                  v-for="member in topMembers"
                  :key="member.userId"
                  class="representation-member"
                  :to="{ name: 'public-profile', params: { id: member.userId } }"
                >
                  <span>{{ getMemberName(member) }}</span>
                  <small>{{ member.role }}</small>
                  <strong>{{ member.rating }}</strong>
                </router-link>
              </div>
            </div>
          </template>
        </div>
      </section>
    </div>
  </section>
</template>
