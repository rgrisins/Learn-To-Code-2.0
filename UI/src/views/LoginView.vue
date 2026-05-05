<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login } from '../services/auth'

const router = useRouter()
const route = useRoute()

const form = reactive({
  username: '',
  password: '',
})

const errorMessage = ref('')
const isSubmitting = ref(false)

const redirectPath = computed(() => {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : ''
  return redirect.startsWith('/') ? redirect : '/profile'
})

async function handleSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true

  try {
    await login(form)
    await router.replace(redirectPath.value)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Neizdevās ielogoties.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="auth-shell auth-shell--login card border-primary-subtle">
    <div class="card-body p-3 p-lg-4 p-xl-5">
      <div class="auth-header">
        <span class="page-title-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
            <polyline points="10 17 15 12 10 7" />
            <line x1="15" y1="12" x2="3" y2="12" />
          </svg>
        </span>
        <div class="auth-header__copy">
          <h1 class="section-heading mb-0">Pieslēgties kontam</h1>
          <p class="mb-0">Turpini mācības, uzdevumus un progresu savā profilā.</p>
        </div>
      </div>

      <div v-if="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <form class="auth-form row g-3" @submit.prevent="handleSubmit">
        <div class="col-12">
          <label class="form-label" for="username">Lietotājvārds vai e-pasts</label>
          <input id="username" v-model="form.username" type="text" class="form-control form-control-lg auth-input" required />
        </div>

        <div class="col-12">
          <label class="form-label" for="password">Parole</label>
          <input id="password" v-model="form.password" type="password" class="form-control form-control-lg auth-input" required />
        </div>

        <div class="col-12 d-grid gap-2">
          <button class="btn btn-primary btn-lg" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? 'Notiek ielogošanās...' : 'Ielogoties' }}
          </button>

          <RouterLink class="auth-secondary-link" to="/register">
            Reģistrēties
          </RouterLink>
        </div>
      </form>
    </div>
  </section>
</template>
