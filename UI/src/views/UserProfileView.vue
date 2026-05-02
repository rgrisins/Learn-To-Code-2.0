<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState, getPublicUserProfile, type PublicUserProfile } from '../services/auth'
import algoritmiLogo from '../assets/algoritmi.png'

function getLanguageImageUrl(language: { languageId: string; title: string }): string {
  if (language.title.trim().toLowerCase() === 'algoritmi') return algoritmiLogo
  return `/theory/${language.languageId.toLowerCase()}.png`
}

function getExerciseLanguageImageUrl(languageCode?: string | null): string {
  const code = (languageCode ?? '').trim().toLowerCase()
  if (!code) return '/theory/python.png'
  return `/theory/${code}.png`
}

const ALGORITHM_TITLES = new Set(['algoritmi'])

function isAlgorithmCategory(language: { title: string }) {
  return ALGORITHM_TITLES.has(language.title.trim().toLowerCase())
}

const route = useRoute()
const router = useRouter()

const profile = ref<PublicUserProfile | null>(null)
const isLoading = ref(false)
const profileError = ref('')

const profileUsername = computed(() => String(route.params.username ?? '').trim().toLowerCase())
const isOwnProfile = computed(() => authState.user?.id === profile.value?.id)
const displayName = computed(() => {
  const user = profile.value
  if (!user) return 'Lietotāja profils'
  return user.username?.trim() || user.fullName?.trim() || `Lietotājs #${user.id}`
})

const sortedTheoryLanguages = computed(() => {
  const list = profile.value?.stats.theoryLanguages ?? []
  return [...list].sort((a, b) => {
    const aIsAlgo = isAlgorithmCategory(a)
    const bIsAlgo = isAlgorithmCategory(b)
    if (aIsAlgo && !bIsAlgo) return -1
    if (!aIsAlgo && bIsAlgo) return 1
    return 0
  })
})

onMounted(loadProfile)
watch(profileUsername, () => {
  void loadProfile()
})

async function loadProfile() {
  profileError.value = ''
  profile.value = null

  if (!profileUsername.value) {
    profileError.value = 'Lietotājs nav atrasts.'
    return
  }

  isLoading.value = true
  try {
    profile.value = await getPublicUserProfile(profileUsername.value)
  } catch (error) {
    profileError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt profilu.'
  } finally {
    isLoading.value = false
  }
}

function languageLabel(languageCode?: string | null) {
  if (!languageCode) return 'Nav datu'

  const code = languageCode.toLowerCase()
  if (code === 'python') return 'Python'
  if (code === 'java') return 'Java'
  return languageCode
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
  <div>
    <div class="mb-3">
      <button class="app-back-link" type="button" @click="router.back()">
        <span aria-hidden="true">←</span>
        Iepriekšējā lapa
      </button>
    </div>

    <section class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
      <div v-if="isOwnProfile" class="public-profile__header mb-3">
        <router-link class="btn btn-primary" :to="{ name: 'profile' }">
          Mans profils
        </router-link>
      </div>

      <div v-if="isLoading" class="profile-progress-empty">Ielādē profilu...</div>
      <div v-else-if="profileError" class="alert alert-danger">{{ profileError }}</div>

      <template v-else-if="profile">
        <div class="profile-hero mb-4">
          <div class="profile-hero__identity">
            <span class="profile-avatar">{{ displayName.slice(0, 1).toUpperCase() }}</span>
            <div class="profile-hero__details">
              <p class="section-kicker mb-2">Publiskais profils</p>
              <h1 class="section-heading mb-1">{{ displayName }}</h1>
              <p class="profile-public-lead mb-0">
                <template v-if="profile.fullName && profile.fullName !== displayName">{{ profile.fullName }} · </template>pievienojās {{ formatDate(profile.createdAtUtc) }}
              </p>
            </div>
          </div>

          <div class="profile-hero__bio">
            <span class="profile-stat__label">Par sevi</span>
            <p v-if="profile.bio" class="profile-hero__bio-text">{{ profile.bio }}</p>
            <p v-else class="profile-hero__bio-placeholder">Vēl nav aprakstīts</p>
          </div>

          <div class="profile-hero__score">
            <span>Reitings</span>
            <strong>{{ profile.rating }}</strong>
          </div>
        </div>

        <div class="profile-overview-grid mb-4">
          <div class="profile-stat">
            <span class="profile-stat__label">Loma</span>
            <strong>{{ profile.role }}</strong>
          </div>
          <div class="profile-stat">
            <span class="profile-stat__label">Dzimšanas datums</span>
            <strong>{{ profile.birthDate ? formatDate(profile.birthDate) : 'Nav norādīts' }}</strong>
          </div>
          <div class="profile-stat profile-stat--wide-2">
            <span class="profile-stat__label">Pārstāvniecība</span>
            <strong>{{ profile.representation || 'Nav norādīta' }}</strong>
          </div>
        </div>

        <hr class="profile-section-divider" />

        <div class="profile-statistics">
          <div class="profile-progress-grid profile-progress-grid--wide-first">
            <section class="profile-progress-panel profile-progress-panel--theory">
              <div class="profile-progress-panel__header">
                <h2>Teorija</h2>
              </div>

              <div v-if="sortedTheoryLanguages.length" class="profile-progress-list">
                <div
                  v-for="language in sortedTheoryLanguages"
                  :key="language.languageId"
                  class="profile-progress-item"
                >
                  <div class="profile-progress-item__top">
                    <img
                      class="profile-progress-item__icon"
                      :src="getLanguageImageUrl(language)"
                      :alt="language.title"
                    />
                    <div class="profile-progress-item__title">
                      <strong>{{ language.title }}</strong>
                    </div>
                    <span class="theory-card__progress">{{ language.progressPercent }}% apgūts</span>
                  </div>
                  <div class="profile-progress-track" aria-hidden="true">
                    <span class="profile-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
                  </div>
                </div>
              </div>
              <div v-else class="profile-progress-list">
                <div class="profile-progress-item">
                  <div class="profile-progress-item__top">
                    <img
                      class="profile-progress-item__icon"
                      src="/theory/python.png"
                      alt="Python"
                    />
                    <div class="profile-progress-item__title">
                      <strong>Python</strong>
                    </div>
                    <span class="theory-card__progress">0% apgūts</span>
                  </div>
                  <div class="profile-progress-track" aria-hidden="true">
                    <span class="profile-progress-bar" style="width: 0%"></span>
                  </div>
                </div>
              </div>
            </section>

            <section class="profile-progress-panel">
              <div class="profile-progress-panel__header">
                <h2>Uzdevumi</h2>
                <span>{{ profile.stats.exerciseCompletionPercent }}% izpildīti</span>
              </div>

              <div class="profile-progress-track profile-progress-track--large" aria-hidden="true">
                <span
                  class="profile-progress-bar"
                  :style="{ width: `${profile.stats.exerciseCompletionPercent}%` }"
                ></span>
              </div>

              <div class="profile-task-stats profile-task-stats--grid">
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Izpildīti</span>
                  <strong class="profile-task-stat__value">{{ profile.stats.exerciseSolved }}</strong>
                </div>
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Mēģināti</span>
                  <strong class="profile-task-stat__value">{{ profile.stats.exerciseAttempted }}</strong>
                </div>
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Iesniegumi</span>
                  <strong class="profile-task-stat__value">{{ profile.stats.exerciseSubmissionCount }}</strong>
                </div>
                <div class="profile-task-stat profile-task-stat--accent">
                  <span class="profile-task-stat__label">Pareizi</span>
                  <strong class="profile-task-stat__value">{{ profile.stats.exerciseSuccessPercent }}%</strong>
                </div>
              </div>

              <div class="profile-favourite-language">
                <img
                  v-if="profile.stats.mostUsedExerciseLanguage"
                  class="profile-favourite-language__icon"
                  :src="getExerciseLanguageImageUrl(profile.stats.mostUsedExerciseLanguage)"
                  :alt="languageLabel(profile.stats.mostUsedExerciseLanguage)"
                />
                <div v-else class="profile-favourite-language__icon profile-favourite-language__icon--empty" aria-hidden="true">?</div>
                <div class="profile-favourite-language__text">
                  <span class="profile-favourite-language__label">Visbiežāk lietotā valoda</span>
                  <strong v-if="profile.stats.mostUsedExerciseLanguage">{{ languageLabel(profile.stats.mostUsedExerciseLanguage) }}</strong>
                  <strong v-else class="profile-favourite-language__placeholder">Pagaidām nav datu</strong>
                </div>
              </div>
            </section>
          </div>
        </div>
      </template>
      </div>
    </section>
  </div>
</template>
