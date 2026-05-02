<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  modelValue: string                  // ISO datums (YYYY-MM-DD) vai tukšs
  invalid?: boolean
  inputId?: string
  placeholder?: string
}>()

const emit = defineEmits<{
  (event: 'update:modelValue', value: string): void
  (event: 'update:error', value: string): void
}>()

const displayValue = ref(toDisplay(props.modelValue))
const calendarMonth = ref(initialCalendarMonth(props.modelValue))
const isOpen = ref(false)

watch(
  () => props.modelValue,
  (next) => {
    const formatted = toDisplay(next)
    if (formatted !== displayValue.value) {
      displayValue.value = formatted
    }
    if (next) {
      calendarMonth.value = initialCalendarMonth(next)
    }
  },
)

const calendarTitle = computed(() =>
  new Intl.DateTimeFormat('lv-LV', { month: 'long', year: 'numeric' }).format(calendarMonth.value),
)

const availableYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return Array.from({ length: 101 }, (_, index) => currentYear - index)
})

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
      iso: formatIsoDate(date),
      isEmpty: false,
    })
  }

  return cells
})

function initialCalendarMonth(iso: string) {
  if (!iso) return new Date()
  const parsed = parseIsoDate(iso)
  if (!parsed) return new Date()
  return new Date(parsed.getFullYear(), parsed.getMonth(), 1)
}

function parseIsoDate(iso: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso.trim())
  if (!match) return null
  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const parsed = new Date(year, month - 1, day)
  if (
    parsed.getFullYear() !== year ||
    parsed.getMonth() !== month - 1 ||
    parsed.getDate() !== day
  ) {
    return null
  }
  return parsed
}

function toDisplay(iso: string) {
  const parsed = parseIsoDate(iso)
  if (!parsed) return ''
  return formatDisplayDate(parsed)
}

function formatDisplayDate(date: Date) {
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  return `${day}/${month}/${year}`
}

function formatIsoDate(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function selectCalendarDate(iso: string) {
  emit('update:modelValue', iso)
  emit('update:error', '')
  displayValue.value = toDisplay(iso)
  isOpen.value = false
}

function moveCalendarMonth(offset: number) {
  calendarMonth.value = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth() + offset, 1)
}

function selectCalendarYear(event: Event) {
  const target = event.target as HTMLSelectElement
  calendarMonth.value = new Date(Number(target.value), calendarMonth.value.getMonth(), 1)
}

function onInput() {
  const trimmed = displayValue.value.trim()

  if (!trimmed) {
    emit('update:modelValue', '')
    emit('update:error', '')
    return
  }

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(trimmed)
  if (isoMatch) {
    const parsed = parseIsoDate(trimmed)
    if (parsed) {
      emit('update:modelValue', formatIsoDate(parsed))
      emit('update:error', '')
      return
    }
    emit('update:error', 'Datumam jābūt derīgam.')
    return
  }

  const ddmm = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed)
  if (!ddmm) {
    emit('update:error', 'Formāts: dd/mm/yyyy')
    return
  }
  const day = Number(ddmm[1])
  const month = Number(ddmm[2])
  const year = Number(ddmm[3])
  const parsed = new Date(year, month - 1, day)
  if (
    parsed.getFullYear() !== year ||
    parsed.getMonth() !== month - 1 ||
    parsed.getDate() !== day
  ) {
    emit('update:error', 'Datumam jābūt derīgam.')
    return
  }
  emit('update:modelValue', formatIsoDate(parsed))
  emit('update:error', '')
}

function clear() {
  displayValue.value = ''
  emit('update:modelValue', '')
  emit('update:error', '')
}

function onClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (!target.closest('.birthdate-field-wrapper')) {
    isOpen.value = false
  }
}

watch(isOpen, (open) => {
  if (open) {
    document.addEventListener('mousedown', onClickOutside)
  } else {
    document.removeEventListener('mousedown', onClickOutside)
  }
})
</script>

<template>
  <div class="birthdate-field-wrapper position-relative">
    <div class="input-group birthdate-field">
      <input
        :id="inputId"
        v-model="displayValue"
        type="text"
        inputmode="numeric"
        :placeholder="placeholder ?? 'dd/mm/yyyy'"
        class="form-control form-control-lg auth-input"
        :class="{ 'is-invalid': invalid }"
        @focus="isOpen = true"
        @click="isOpen = true"
        @input="onInput"
        @blur="onInput"
      />
      <button v-if="modelValue" class="btn btn-outline-light" type="button" :title="'Notīrīt'" @click="clear">
        ×
      </button>
      <button class="btn btn-outline-light" type="button" @click="isOpen = !isOpen">
        Kalendārs
      </button>
    </div>

    <div v-if="isOpen" class="birthdate-calendar card border-primary-subtle mt-2">
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
              :class="{ 'is-selected': cell.iso === modelValue }"
              @click="selectCalendarDate(cell.iso)"
            >
              {{ cell.label }}
            </button>
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
