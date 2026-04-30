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
  <section class="auth-shell card border-primary-subtle">
    <div class="card-body p-3 p-lg-4 p-xl-5">
      <h1 class="section-heading mb-3">Pieslēgties kontam</h1>

      <div v-if="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <form class="row g-3" @submit.prevent="handleSubmit">
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