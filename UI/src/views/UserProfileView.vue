<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState, getPublicUserProfile, type PublicUserProfile } from '../services/auth'

const route = useRoute()
const router = useRouter()

const profile = ref<PublicUserProfile | null>(null)
const isLoading = ref(false)
const profileError = ref('')

const profileId = computed(() => Number(route.params.id))
const isOwnProfile = computed(() => authState.user?.id === profile.value?.id)
const displayName = computed(() => {
  const user = profile.value
  if (!user) return 'Lietotāja profils'
  return user.username?.trim() || user.fullName?.trim() || `Lietotājs #${user.id}`
})

onMounted(loadProfile)
watch(profileId, () => {
  void loadProfile()
})

async function loadProfile() {
  profileError.value = ''
  profile.value = null

  if (!Number.isInteger(profileId.value) || profileId.value <= 0) {
    profileError.value = 'Lietotājs nav atrasts.'
    return
  }

  isLoading.value = true
  try {
    profile.value = await getPublicUserProfile(profileId.value)
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
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4">
      <div class="public-profile__header mb-4">
        <div>
          <button class="btn btn-outline-light btn-sm mb-3" type="button" @click="router.back()">
            ← Atpakaļ
          </button>
          <h1 class="section-heading mb-1">{{ displayName }}</h1>
          <p v-if="profile?.fullName && profile.fullName !== displayName" class="profile-public-lead mb-0">
            {{ profile.fullName }}
          </p>
        </div>
        <router-link v-if="isOwnProfile" class="btn btn-primary" :to="{ name: 'profile' }">
          Mans profils
        </router-link>
      </div>

      <div v-if="isLoading" class="profile-progress-empty">Ielādē profilu...</div>
      <div v-else-if="profileError" class="alert alert-danger">{{ profileError }}</div>

      <template v-else-if="profile">
        <div class="row g-3 mb-4">
          <div class="col-12 col-lg-3">
            <div class="profile-stat">
              <span class="profile-stat__label">Reitings</span>
              <strong>{{ profile.rating }}</strong>
            </div>
          </div>
          <div class="col-12 col-lg-3">
            <div class="profile-stat">
              <span class="profile-stat__label">Loma</span>
              <strong>{{ profile.role }}</strong>
            </div>
          </div>
          <div class="col-12 col-lg-3">
            <div class="profile-stat">
              <span class="profile-stat__label">Izglītības iestāde</span>
              <strong>{{ profile.educationInstitution || 'Nav norādīta' }}</strong>
            </div>
          </div>
          <div class="col-12 col-lg-3">
            <div class="profile-stat">
              <span class="profile-stat__label">Pievienojās</span>
              <strong>{{ formatDate(profile.createdAtUtc) }}</strong>
            </div>
          </div>
        </div>

        <div class="profile-statistics">
          <div class="profile-statistics__header">
            <h2>Statistika</h2>
          </div>

          <div class="profile-progress-grid">
            <section class="profile-progress-panel">
              <div class="profile-progress-panel__header">
                <h2>Teorija</h2>
                <span>{{ profile.stats.theoryLanguages.length }} valodas</span>
              </div>

              <div v-if="profile.stats.theoryLanguages.length" class="profile-progress-list">
                <div
                  v-for="language in profile.stats.theoryLanguages"
                  :key="language.languageId"
                  class="profile-progress-item"
                >
                  <div class="profile-progress-item__top">
                    <div>
                      <strong>{{ language.title }}</strong>
                      <span>{{ language.topicCount }} tēmas</span>
                    </div>
                    <strong>{{ language.progressPercent }}%</strong>
                  </div>
                  <div class="profile-progress-track" aria-hidden="true">
                    <span class="profile-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
                  </div>
                </div>
              </div>
              <div v-else class="profile-progress-empty">Vēl nav iesāktu programmēšanas valodu.</div>
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
              <div class="profile-task-stats">
                <div>
                  <span>Izpildīti</span>
                  <strong>{{ profile.stats.exerciseSolved }} izpildīti</strong>
                </div>
                <div>
                  <span>Mēģināti</span>
                  <strong>{{ profile.stats.exerciseAttempted }}</strong>
                </div>
                <div>
                  <span>Iesniegumi</span>
                  <strong>{{ profile.stats.exerciseSubmissionCount }}</strong>
                </div>
                <div>
                  <span>Pareizo iesniegumu %</span>
                  <strong>{{ profile.stats.exerciseSuccessPercent }}%</strong>
                </div>
                <div>
                  <span>Izmantotākā valoda</span>
                  <strong>{{ languageLabel(profile.stats.mostUsedExerciseLanguage) }}</strong>
                </div>
              </div>
            </section>
          </div>
        </div>
      </template>
    </div>
  </section>
</template>
