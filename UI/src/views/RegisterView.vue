<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { register } from '../services/auth'

const router = useRouter()
const route = useRoute()

const form = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  email: '',
  password: '',
  confirmPassword: '',
  role: 'Audzeknis',
})

const errorMessage = ref('')
const isSubmitting = ref(false)
const birthDatePickerOpen = ref(false)
const isRoleReasonModalOpen = ref(false)
const roleRequestReason = ref('')
const roleRequestReasonError = ref('')
const calendarMonth = ref(new Date())
const availableYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return Array.from({ length: 101 }, (_, index) => currentYear - index)
})
const availableRoles = ['Audzeknis', 'Pedagogs'] as const
const errors = reactive({
  username: '',
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  birthDate: '',
  role: '',
})

const redirectPath = computed(() => {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : ''
  return redirect.startsWith('/') ? redirect : '/profile'
})

function clearFieldErrors() {
  Object.keys(errors).forEach((key) => {
    errors[key as keyof typeof errors] = ''
  })
}

function formatDisplayDate(isoDate: Date) {
  const day = String(isoDate.getDate()).padStart(2, '0')
  const month = String(isoDate.getMonth() + 1).padStart(2, '0')
  const year = isoDate.getFullYear()
  return `${day}/${month}/${year}`
}

function formatCalendarDate(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function parseBirthDate(input: string) {
  const trimmed = input.trim()

  if (!trimmed) {
    return { value: null, error: '' }
  }

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(trimmed)
  if (isoMatch) {
    const year = Number(isoMatch[1])
    const month = Number(isoMatch[2])
    const day = Number(isoMatch[3])
    const parsed = new Date(year, month - 1, day)

    if (
      parsed.getFullYear() !== year ||
      parsed.getMonth() !== month - 1 ||
      parsed.getDate() !== day
    ) {
      return { value: null, error: 'Dzimšanas datumam jābūt derīgam datumam.' }
    }

    return { value: formatCalendarDate(parsed), error: '' }
  }

  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed)
  if (!match) {
    return { value: null, error: 'Dzimšanas datumam jābūt formātā dd/mm/yyyy.' }
  }

  const day = Number(match[1])
  const month = Number(match[2])
  const year = Number(match[3])
  const parsed = new Date(year, month - 1, day)

  if (
    parsed.getFullYear() !== year ||
    parsed.getMonth() !== month - 1 ||
    parsed.getDate() !== day
  ) {
    return { value: null, error: 'Dzimšanas datumam jābūt derīgam datumam.' }
  }

  return { value: formatCalendarDate(parsed), error: '' }
}

const calendarTitle = computed(() =>
  new Intl.DateTimeFormat('lv-LV', { month: 'long', year: 'numeric' }).format(calendarMonth.value),
)

const calendarWeeks = computed(() => {
  const firstDay = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth(), 1)
  const startOffset = (firstDay.getDay() + 6) % 7
  const daysInMonth = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth() + 1, 0).getDate()
  const cells: Array<{ label: string; iso: string | null; isEmpty: boolean }> = []

  for (let index = 0; index < startOffset; index += 1) {
    cells.push({ label: '', iso: null, isEmpty: true })
  }

  for (let day = 1; day <= daysInMonth; day += 1) {
    const date = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth(), day)
    cells.push({
      label: String(day),
      iso: formatCalendarDate(date),
      isEmpty: false,
    })
  }

  return cells
})

function selectCalendarDate(isoDate: string) {
  const [year, month, day] = isoDate.split('-').map(Number)
  const parsed = new Date(year, month - 1, day)
  form.birthDate = formatDisplayDate(parsed)
  birthDatePickerOpen.value = false
  errors.birthDate = ''
}

function moveCalendarMonth(offset: number) {
  calendarMonth.value = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth() + offset, 1)
}

function selectCalendarYear(event: Event) {
  const target = event.target as HTMLSelectElement
  calendarMonth.value = new Date(Number(target.value), calendarMonth.value.getMonth(), 1)
}

function validateForm() {
  clearFieldErrors()

  const username = form.username.trim()
  const firstName = form.firstName.trim()
  const lastName = form.lastName.trim()
  const email = form.email.trim()
  const password = form.password
  const confirmPassword = form.confirmPassword
  const birthDateResult = parseBirthDate(form.birthDate)

  if (!username) {
    errors.username = 'Lietotājvārds ir obligāts.'
  } else if (!/^[a-zA-Z0-9._-]{3,30}$/.test(username)) {
    errors.username = 'Lietotājvārdam jābūt 3-30 rakstzīmes un drīkst saturēt burtus, ciparus, punktu, domuzīmi vai apakšsvītru.'
  }

  if (!firstName) {
    errors.firstName = 'Vārds ir obligāts.'
  }

  if (!lastName) {
    errors.lastName = 'Uzvārds ir obligāts.'
  }

  if (!email) {
    errors.email = 'E-pasts ir obligāts.'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.email = 'Ievadi derīgu e-pasta adresi.'
  }

  if (!password) {
    errors.password = 'Parole ir obligāta.'
  } else if (password.length < 8) {
    errors.password = 'Parolei jābūt vismaz 8 rakstzīmes garai.'
  } else if (!/\d/.test(password)) {
    errors.password = 'Parolei jāsatur vismaz viens cipars.'
  } else if (!/[A-Z]/.test(password)) {
    errors.password = 'Parolei jāsatur vismaz viens lielais burts.'
  } else if (!/[^A-Za-z0-9]/.test(password)) {
    errors.password = 'Parolei jāsatur vismaz viens simbols.'
  }

  if (!confirmPassword) {
    errors.confirmPassword = 'Apstiprini paroli.'
  } else if (password !== confirmPassword) {
    errors.confirmPassword = 'Paroles nesakrīt.'
  }

  if (form.birthDate.trim() && birthDateResult.error) {
    errors.birthDate = birthDateResult.error
  }

  if (!form.role) {
    errors.role = 'Izvēlies lomu.'
  } else if (!availableRoles.includes(form.role as (typeof availableRoles)[number])) {
    errors.role = 'Administrators nav pieejams pašreģistrācijai.'
  }

  return Object.values(errors).every((value) => !value)
}

async function saveRoleReason() {
  const reason = roleRequestReason.value.trim()
  if (reason.length < 10) {
    roleRequestReasonError.value = 'Uzraksti vismaz 10 rakstzimes.'
    return
  }

  roleRequestReason.value = reason
  roleRequestReasonError.value = ''
  errors.role = ''
  isRoleReasonModalOpen.value = false

  await submitRegistration()
}

function cancelRoleReason() {
  form.role = 'Audzeknis'
  roleRequestReason.value = ''
  roleRequestReasonError.value = ''
  errors.role = ''
  errorMessage.value = ''
  isRoleReasonModalOpen.value = false
}

async function handleSubmit() {
  errorMessage.value = ''

  const isFormValid = validateForm()
  if (!isFormValid) {
    errorMessage.value = 'Aizpildi iezīmētos laukus.'
    return
  }

  if (form.role === 'Pedagogs' && roleRequestReason.value.trim().length < 10) {
    roleRequestReasonError.value = ''
    isRoleReasonModalOpen.value = true
    return
  }

  await submitRegistration()
}

async function submitRegistration() {
  errorMessage.value = ''

  isSubmitting.value = true

  const birthDateResult = parseBirthDate(form.birthDate)

  try {
    await register({
      username: form.username.trim(),
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      birthDate: birthDateResult.value,
      email: form.email.trim(),
      password: form.password,
      representation: null,
      role: form.role,
      roleRequestReason: form.role === 'Pedagogs' ? roleRequestReason.value.trim() : null,
    })
    await router.replace(redirectPath.value)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Neizdevās reģistrēties.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="auth-shell card border-primary-subtle">
    <div class="card-body p-3 p-lg-4 p-xl-5">
      <h1 class="section-heading mb-3">Izveidot lietotāju</h1>

      <div v-if="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <form class="row g-3" novalidate @submit.prevent="handleSubmit">
        <div class="col-12">
          <label class="form-label" for="username">Lietotājvārds</label>
          <input id="username" v-model="form.username" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.username }" />
          <div v-if="errors.username" class="invalid-feedback d-block">{{ errors.username }}</div>
        </div>

        <div class="col-12 col-lg-6">
          <label class="form-label" for="firstName">Vārds</label>
          <input id="firstName" v-model="form.firstName" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.firstName }" />
          <div v-if="errors.firstName" class="invalid-feedback d-block">{{ errors.firstName }}</div>
        </div>

        <div class="col-12 col-lg-6">
          <label class="form-label" for="lastName">Uzvārds</label>
          <input id="lastName" v-model="form.lastName" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.lastName }" />
          <div v-if="errors.lastName" class="invalid-feedback d-block">{{ errors.lastName }}</div>
        </div>

        <div class="col-12 col-lg-6">
          <label class="form-label" for="email">E-pasts</label>
          <input id="email" v-model="form.email" type="email" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.email }" />
          <div v-if="errors.email" class="invalid-feedback d-block">{{ errors.email }}</div>
        </div>

        <div class="col-12">
          <div class="row g-3">
            <div class="col-12 col-lg-6">
              <label class="form-label" for="password">Parole</label>
              <input id="password" v-model="form.password" type="password" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.password }" />
              <div v-if="errors.password" class="invalid-feedback d-block">{{ errors.password }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="confirmPassword">Parole vēlreiz</label>
              <input id="confirmPassword" v-model="form.confirmPassword" type="password" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!errors.confirmPassword }" />
              <div v-if="errors.confirmPassword" class="invalid-feedback d-block">{{ errors.confirmPassword }}</div>
            </div>
          </div>
        </div>

        <div class="col-12 col-lg-6 position-relative">
          <label class="form-label" for="birthDate">Dzimšanas datums <span class="text-secondary">(opcionāli)</span></label>
          <div class="input-group birthdate-field">
            <input
              id="birthDate"
              v-model="form.birthDate"
              type="text"
              inputmode="numeric"
              placeholder="dd/mm/yyyy"
              class="form-control form-control-lg auth-input"
              :class="{ 'is-invalid': !!errors.birthDate }"
              @focus="birthDatePickerOpen = true"
              @click="birthDatePickerOpen = true"
            />
            <button class="btn btn-outline-light" type="button" @click="birthDatePickerOpen = !birthDatePickerOpen">
              Kalendārs
            </button>
          </div>
          <div v-if="errors.birthDate" class="invalid-feedback d-block">{{ errors.birthDate }}</div>

          <div v-if="birthDatePickerOpen" class="birthdate-calendar card border-primary-subtle mt-2">
            <div class="card-body p-3">
              <div class="d-flex align-items-center justify-content-between gap-2 mb-3">
                <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(-1)">‹</button>
                <div class="d-flex align-items-center gap-2 flex-grow-1 justify-content-center">
                  <strong class="text-white text-capitalize">{{ calendarTitle }}</strong>
                  <select class="form-select form-select-sm birthdate-year-select" :value="calendarMonth.getFullYear()" @change="selectCalendarYear">
                    <option v-for="year in availableYears" :key="year" :value="year">{{ year }}</option>
                  </select>
                </div>
                <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(1)">›</button>
              </div>

              <div class="calendar-grid calendar-grid__head mb-2">
                <span>P</span>
                <span>O</span>
                <span>T</span>
                <span>C</span>
                <span>P</span>
                <span>S</span>
                <span>S</span>
              </div>

              <div class="calendar-grid">
                <span v-for="(cell, index) in calendarWeeks" :key="`${cell.iso || 'empty'}-${index}`" :class="['calendar-cell', { 'is-empty': cell.isEmpty }]">
                  <button
                    v-if="!cell.isEmpty && cell.iso"
                    type="button"
                    class="calendar-day"
                    @click="selectCalendarDate(cell.iso)"
                  >
                    {{ cell.label }}
                  </button>
                </span>
              </div>
            </div>
          </div>
        </div>

        <div class="col-12">
          <label class="form-label" for="role">Loma</label>
          <select id="role" v-model="form.role" class="form-select form-select-lg auth-input" :class="{ 'is-invalid': !!errors.role }">
            <option value="Audzeknis">Audzēknis</option>
            <option value="Pedagogs">Pedagogs</option>
          </select>
          <div v-if="errors.role" class="invalid-feedback d-block">{{ errors.role }}</div>
          <small v-if="form.role === 'Pedagogs' && roleRequestReason" class="form-text text-secondary">
            Pedagoga loma tiks nosūtīta adminiem apstiprināšanai.
          </small>
        </div>

        <div class="col-12 d-grid gap-2">
          <button class="btn btn-primary btn-lg" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? 'Notiek reģistrācija...' : 'Reģistrēties' }}
          </button>

          <RouterLink class="auth-secondary-link" to="/login">
            Jau ir konts? Ielogoties
          </RouterLink>
        </div>
      </form>
    </div>

    <div v-if="isRoleReasonModalOpen" class="app-modal-backdrop" @click.self="cancelRoleReason">
      <div class="app-modal app-modal--sm card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="section-heading mb-3">Pamato pieprasījumu</h2>
          <p class="logout-text logout-text--navbar mb-3">
            Admini izskatīs, kāpēc tev nepieciešama pedagoga piekļuve.
          </p>

          <label class="form-label" for="roleRequestReason">Iemesls</label>
          <textarea
            id="roleRequestReason"
            v-model="roleRequestReason"
            class="form-control auth-input"
            maxlength="1000"
            rows="5"
            placeholder="Piemēram, es vadu nodarbības savai klasei..."
          ></textarea>
          <div v-if="roleRequestReasonError" class="invalid-feedback d-block">{{ roleRequestReasonError }}</div>

          <div class="d-flex justify-content-end gap-2 mt-4">
            <button class="btn btn-outline-light" type="button" @click="cancelRoleReason">Atcelt</button>
            <button class="btn btn-primary" type="button" @click="saveRoleReason">Turpinat</button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
