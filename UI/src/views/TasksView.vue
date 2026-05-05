<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { createExercise } from '../services/exercises'
import { clearDraft, loadDraft, saveDraft } from '../services/drafts'

type SupportedLanguage = 'python' | 'java'

interface BuilderTestCase {
  input: string
  expectedOutput: string
}

const TASK_DRAFT_KEY = 'task-builder'

interface TaskDraft {
  title: string
  description: string
  difficulty: (typeof difficultyOptions)[number]
  languageCode: SupportedLanguage
  solutionCode: string
  tests: BuilderTestCase[]
}

const minimumTotalTests = 20
const sampleTestCount = 2
const minimumHiddenTests = minimumTotalTests - sampleTestCount

const languageOptions: Array<{ code: SupportedLanguage; label: string }> = [
  { code: 'python', label: 'Python 3.11' },
  { code: 'java', label: 'Java 21' },
]

const difficultyOptions = ['viegls', 'vidējs', 'grūts'] as const

const starterSolutions: Record<SupportedLanguage, string> = {
  python: `# Ievieto autora risinājumu šeit
`,
  java: `import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        // Ievieto autora risinājumu šeit
    }
}
`,
}

let lastStarterSolution = starterSolutions.python

const form = reactive({
  title: '',
  description: '',
  difficulty: 'viegls' as (typeof difficultyOptions)[number],
  languageCode: 'python' as SupportedLanguage,
  solutionCode: starterSolutions.python,
})

const tests = ref<BuilderTestCase[]>(createInitialTests())
const fieldErrors = ref<Record<string, string>>({})
const validationMessages = ref<string[]>([])
const submitError = ref('')
const submitSuccess = ref('')
const isSubmitting = ref(false)
const draftRestored = ref(false)
const draftSavedAt = ref<Date | null>(null)

restoreTaskDraft()

const draftSavedLabel = computed(() => {
  if (!draftSavedAt.value) return ''
  return `Melnraksts saglabāts ${formatDraftTime(draftSavedAt.value)}`
})

watch(
  [form, tests],
  () => {
    persistTaskDraft()
  },
  { deep: true },
)

function persistTaskDraft() {
  const isPristine = !form.title.trim() &&
    !form.description.trim() &&
    form.solutionCode === starterSolutions[form.languageCode] &&
    tests.value.every((testCase) => !testCase.input.trim() && !testCase.expectedOutput.trim())

  if (isPristine) {
    clearDraft(TASK_DRAFT_KEY)
    draftSavedAt.value = null
    return
  }

  saveDraft<TaskDraft>(TASK_DRAFT_KEY, {
    title: form.title,
    description: form.description,
    difficulty: form.difficulty,
    languageCode: form.languageCode,
    solutionCode: form.solutionCode,
    tests: tests.value.map((testCase) => ({
      input: testCase.input,
      expectedOutput: testCase.expectedOutput,
    })),
  })
  draftSavedAt.value = new Date()
}

function restoreTaskDraft() {
  const draft = loadDraft<TaskDraft>(TASK_DRAFT_KEY)
  if (!draft) return

  form.title = draft.title ?? ''
  form.description = draft.description ?? ''
  if (difficultyOptions.includes(draft.difficulty)) {
    form.difficulty = draft.difficulty
  }
  if (languageOptions.some((option) => option.code === draft.languageCode)) {
    form.languageCode = draft.languageCode
  }
  if (typeof draft.solutionCode === 'string' && draft.solutionCode.length > 0) {
    form.solutionCode = draft.solutionCode
  }
  lastStarterSolution = starterSolutions[form.languageCode]

  if (Array.isArray(draft.tests) && draft.tests.length > 0) {
    const restored = draft.tests.map((testCase) => ({
      input: testCase?.input ?? '',
      expectedOutput: testCase?.expectedOutput ?? '',
    }))
    while (restored.length < minimumTotalTests) {
      restored.push({ input: '', expectedOutput: '' })
    }
    tests.value = restored
  }

  draftRestored.value = true
}

function discardDraft() {
  clearDraft(TASK_DRAFT_KEY)
  draftRestored.value = false
  draftSavedAt.value = null
  form.title = ''
  form.description = ''
  form.difficulty = 'viegls'
  form.languageCode = 'python'
  form.solutionCode = starterSolutions.python
  lastStarterSolution = starterSolutions.python
  tests.value = createInitialTests()
  fieldErrors.value = {}
  validationMessages.value = []
}

function formatDraftTime(value: Date) {
  return value.toLocaleTimeString('lv-LV', { hour: '2-digit', minute: '2-digit' })
}

const totalTestCount = computed(() => tests.value.length)
const completedTestCount = computed(() =>
  tests.value.filter((testCase) => isTestComplete(testCase)).length,
)

const testProgressLabel = computed(
  () => `${completedTestCount.value}/${totalTestCount.value} testi`,
)

const currentLanguageLabel = computed(
  () => languageOptions.find((option) => option.code === form.languageCode)?.label ?? '',
)

const currentDifficultyLabel = computed(() => formatDifficultyLabel(form.difficulty))

function formatDifficultyLabel(value: string) {
  const normalized = value.trim()
  if (!normalized) return ''
  return normalized.charAt(0).toUpperCase() + normalized.slice(1)
}

function createInitialTests(): BuilderTestCase[] {
  return Array.from({ length: minimumTotalTests }, () => ({
    input: '',
    expectedOutput: '',
  }))
}

function normalizeText(value: string) {
  return value.replace(/\r\n/g, '\n').replace(/\r/g, '\n').trim()
}

function isTestComplete(testCase: BuilderTestCase) {
  return Boolean(normalizeText(testCase.input) && normalizeText(testCase.expectedOutput))
}

function isSampleTest(index: number) {
  return index < sampleTestCount
}

function canRemoveTest(index: number) {
  if (isSampleTest(index)) return false
  return tests.value.length > minimumTotalTests
}

function handleLanguageChange() {
  const nextTemplate = starterSolutions[form.languageCode]

  if (!form.solutionCode.trim() || form.solutionCode === lastStarterSolution) {
    form.solutionCode = nextTemplate
  }

  lastStarterSolution = nextTemplate
}

function setFieldError(errors: Record<string, string>, key: string, message: string) {
  errors[key] = message
}

function addTest() {
  tests.value.push({ input: '', expectedOutput: '' })
}

function removeTest(index: number) {
  if (!canRemoveTest(index)) return
  tests.value.splice(index, 1)
}

function validateForm() {
  const errors: Record<string, string> = {}
  const messages: string[] = []

  if (!normalizeText(form.title)) {
    setFieldError(errors, 'title', 'Nosaukums ir obligāts.')
    messages.push('Norādi uzdevuma nosaukumu.')
  }

  if (!normalizeText(form.description)) {
    setFieldError(errors, 'description', 'Apraksts ir obligāts.')
    messages.push('Norādi uzdevuma aprakstu.')
  }

  if (!difficultyOptions.includes(form.difficulty)) {
    setFieldError(errors, 'difficulty', 'Izvēlies grūtību.')
    messages.push('Izvēlies grūtības pakāpi.')
  }

  if (!languageOptions.some((option) => option.code === form.languageCode)) {
    setFieldError(errors, 'languageCode', 'Izvēlies valodu.')
    messages.push('Izvēlies atbalstītu programmēšanas valodu.')
  }

  if (!normalizeText(form.solutionCode)) {
    setFieldError(errors, 'solutionCode', 'Autora risinājums ir obligāts.')
    messages.push('Pievieno autora risinājumu izvēlētajā valodā.')
  }

  if (tests.value.length < minimumTotalTests) {
    messages.push(`Uzdevumam jābūt vismaz ${minimumTotalTests} testiem.`)
  }

  let hasMissingTests = false
  tests.value.forEach((testCase, index) => {
    if (!normalizeText(testCase.input)) {
      setFieldError(errors, `test-${index}-input`, 'Ievaddati ir obligāti.')
      hasMissingTests = true
    }

    if (!normalizeText(testCase.expectedOutput)) {
      setFieldError(errors, `test-${index}-output`, 'Izvaddati ir obligāti.')
      hasMissingTests = true
    }
  })

  if (hasMissingTests) {
    messages.push('Aizpildi ievaddatus un izvaddatus visiem testiem.')
  }

  fieldErrors.value = errors
  validationMessages.value = [...new Set(messages)]

  return validationMessages.value.length === 0
}

function resetForm() {
  form.title = ''
  form.description = ''
  form.difficulty = 'viegls'
  form.languageCode = 'python'
  form.solutionCode = starterSolutions.python
  lastStarterSolution = starterSolutions.python
  tests.value = createInitialTests()
  fieldErrors.value = {}
  validationMessages.value = []
}

async function handleSubmit() {
  submitError.value = ''
  submitSuccess.value = ''

  if (!validateForm()) {
    return
  }

  isSubmitting.value = true

  try {
    // Samples are simply the first two tests in the unified list (visible to learners),
    // hidden tests follow.
    const allTests = tests.value.map((testCase, index) => ({
      input: normalizeText(testCase.input),
      expectedOutput: normalizeText(testCase.expectedOutput),
      isHidden: index >= sampleTestCount,
      orderIndex: index,
    }))

    await createExercise({
      title: normalizeText(form.title),
      description: normalizeText(form.description),
      difficulty: form.difficulty,
      languageCode: form.languageCode,
      solutionLanguageCode: form.languageCode,
      solutionCode: form.solutionCode,
      testCases: allTests,
    })

    submitSuccess.value =
      'Uzdevums iesniegts apstiprināšanai. Tas parādīsies sarakstā, tiklīdz administrators to apstiprinās.'
    clearDraft(TASK_DRAFT_KEY)
    draftRestored.value = false
    draftSavedAt.value = null
    resetForm()
  } catch (error) {
    submitError.value = error instanceof Error ? error.message : 'Neizdevās izveidot uzdevumu.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="task-builder">
    <div class="mb-3">
      <RouterLink to="/exercises" class="app-back-link">← Uzdevumu saraksts</RouterLink>
    </div>

    <article class="content-panel card border-primary-subtle mb-3">
      <div class="card-body p-3 p-lg-4">
        <div class="theory-header exercises-header">
          <span class="page-title-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="16 18 22 12 16 6" />
              <polyline points="8 6 2 12 8 18" />
            </svg>
          </span>
          <div class="theory-heading-copy">
            <h1 class="section-heading mb-0">Izveidot uzdevumu</h1>
          </div>
          <div class="exercises-header__badges">
            <div class="task-builder__meta task-builder__meta--header">
              <span>{{ currentDifficultyLabel }}</span>
              <span>{{ testProgressLabel }}</span>
              <span>{{ currentLanguageLabel }}</span>
            </div>
          </div>
        </div>
      </div>
    </article>

    <div v-if="submitSuccess" class="alert alert-success mb-3">
      {{ submitSuccess }}
    </div>

    <div v-if="submitError" class="alert alert-danger mb-3">
      {{ submitError }}
    </div>

    <div v-if="draftRestored" class="alert alert-info task-builder__draft mb-3">
      <span>
        Atjaunots iepriekšējais melnraksts.
        <span v-if="draftSavedAt"> {{ draftSavedLabel }}.</span>
      </span>
      <button type="button" class="btn btn-outline-light btn-sm" @click="discardDraft">
        Sākt no jauna
      </button>
    </div>

    <div v-if="validationMessages.length" class="alert alert-warning task-builder__validation mb-3">
      <strong>Pārbaudi laukus</strong>
      <ul>
        <li v-for="message in validationMessages" :key="message">{{ message }}</li>
      </ul>
    </div>

    <form class="task-builder__form" novalidate @submit.prevent="handleSubmit">
      <article class="content-panel card border-primary-subtle task-builder__card">
        <div class="card-body p-3 p-lg-4">
          <div class="task-builder__section-head">
            <h2 class="section-heading section-heading--caps mb-0">Pamatinformācija</h2>
          </div>
          <hr class="profile-section-divider" />

          <div class="row g-3">
            <div class="col-12 col-lg-8">
              <label class="form-label" for="taskTitle">Nosaukums</label>
              <input
                id="taskTitle"
                v-model="form.title"
                class="form-control auth-input"
                :class="{ 'is-invalid': !!fieldErrors.title }"
                maxlength="200"
                type="text"
              />
              <div v-if="fieldErrors.title" class="invalid-feedback d-block">
                {{ fieldErrors.title }}
              </div>
            </div>

            <div class="col-12 col-md-6 col-lg-2">
              <label class="form-label" for="taskDifficulty">Grūtība</label>
              <select
                id="taskDifficulty"
                v-model="form.difficulty"
                class="form-select auth-input"
                :class="{ 'is-invalid': !!fieldErrors.difficulty }"
              >
                <option v-for="difficulty in difficultyOptions" :key="difficulty" :value="difficulty">
                  {{ formatDifficultyLabel(difficulty) }}
                </option>
              </select>
              <div v-if="fieldErrors.difficulty" class="invalid-feedback d-block">
                {{ fieldErrors.difficulty }}
              </div>
            </div>

            <div class="col-12 col-md-6 col-lg-2">
              <label class="form-label" for="taskLanguage">Ieteicamā valoda</label>
              <select
                id="taskLanguage"
                v-model="form.languageCode"
                class="form-select auth-input"
                :class="{ 'is-invalid': !!fieldErrors.languageCode }"
                @change="handleLanguageChange"
              >
                <option v-for="language in languageOptions" :key="language.code" :value="language.code">
                  {{ language.label }}
                </option>
              </select>
              <div v-if="fieldErrors.languageCode" class="invalid-feedback d-block">
                {{ fieldErrors.languageCode }}
              </div>
            </div>

            <div class="col-12">
              <label class="form-label" for="taskDescription">Apraksts</label>
              <textarea
                id="taskDescription"
                v-model="form.description"
                class="form-control auth-input"
                :class="{ 'is-invalid': !!fieldErrors.description }"
                rows="5"
              ></textarea>
              <div v-if="fieldErrors.description" class="invalid-feedback d-block">
                {{ fieldErrors.description }}
              </div>
            </div>
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle task-builder__card">
        <div class="card-body p-3 p-lg-4">
          <div class="task-builder__section-head">
            <h2 class="section-heading section-heading--caps mb-0">Autora risinājums</h2>
            <span class="ex-language-chip">{{ currentLanguageLabel }}</span>
          </div>
          <hr class="profile-section-divider" />

          <p class="task-builder__hint">
            Izvēlies valodu, kurā uzraksti risinājumu — pirms uzdevuma saglabāšanas tas tiek
            automātiski palaists pret visiem testiem un ir jāiziet tie visi.
          </p>

          <textarea
            v-model="form.solutionCode"
            class="form-control auth-input task-solution-editor"
            :class="{ 'is-invalid': !!fieldErrors.solutionCode }"
            spellcheck="false"
            autocomplete="off"
          ></textarea>
          <div v-if="fieldErrors.solutionCode" class="invalid-feedback d-block">
            {{ fieldErrors.solutionCode }}
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle task-builder__card">
        <div class="card-body p-3 p-lg-4">
          <div class="task-builder__section-head">
            <h2 class="section-heading section-heading--caps mb-0">Testi</h2>
            <span class="task-test-counter">{{ testProgressLabel }}</span>
          </div>
          <hr class="profile-section-divider" />

          <p class="task-builder__hint">
            Pirmie {{ sampleTestCount }} testi vienlaikus ir paraugdati — risinātājs tos redz un
            tie tiek skaitīti kā parastie testi pārbaudē. Pārējie testi vienmēr ir slēpti.
            Kopējais minimums — {{ minimumTotalTests }} testi
            (no kuriem {{ sampleTestCount }} paraugi un {{ minimumHiddenTests }} slēpti).
          </p>

          <div class="task-tests-grid">
            <article
              v-for="(testCase, index) in tests"
              :key="index"
              class="task-test-card"
              :class="{
                'is-complete': isTestComplete(testCase),
                'is-sample': isSampleTest(index),
              }"
            >
              <div class="task-test-card__header">
                <strong>Tests {{ index + 1 }}</strong>
                <span
                  class="task-test-card__tag"
                  :class="isSampleTest(index) ? 'is-sample-tag' : 'is-hidden-tag'"
                >
                  {{ isSampleTest(index) ? 'Paraugs · redzams' : 'Slēpts' }}
                </span>
              </div>

              <label class="form-label" :for="`test-${index}-input`">Ievaddati</label>
              <textarea
                :id="`test-${index}-input`"
                v-model="testCase.input"
                class="form-control auth-input"
                :class="{ 'is-invalid': !!fieldErrors[`test-${index}-input`] }"
                rows="3"
              ></textarea>
              <div v-if="fieldErrors[`test-${index}-input`]" class="invalid-feedback d-block">
                {{ fieldErrors[`test-${index}-input`] }}
              </div>

              <label class="form-label mt-3" :for="`test-${index}-output`">Izvaddati</label>
              <textarea
                :id="`test-${index}-output`"
                v-model="testCase.expectedOutput"
                class="form-control auth-input"
                :class="{ 'is-invalid': !!fieldErrors[`test-${index}-output`] }"
                rows="3"
              ></textarea>
              <div v-if="fieldErrors[`test-${index}-output`]" class="invalid-feedback d-block">
                {{ fieldErrors[`test-${index}-output`] }}
              </div>

              <div class="task-test-card__footer">
                <button
                  type="button"
                  class="task-test-remove"
                  :disabled="!canRemoveTest(index)"
                  :title="
                    isSampleTest(index)
                      ? 'Paraugu nevar dzēst'
                      : tests.length <= minimumTotalTests
                        ? `Minimums ${minimumTotalTests} testi`
                        : 'Dzēst testu'
                  "
                  @click="removeTest(index)"
                >
                  Dzēst
                </button>
              </div>
            </article>
          </div>

          <div class="task-builder__add-test">
            <button type="button" class="btn btn-outline-primary btn-sm" @click="addTest">
              + Pievienot testu
            </button>
            <span class="task-builder__add-test-hint">
              Pašlaik {{ totalTestCount }} testi · vari pievienot vairāk
            </span>
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle task-builder__card task-builder__card--actions">
        <div class="card-body p-3 p-lg-4">
          <div class="task-builder__actions">
            <button
              class="btn btn-outline-light"
              type="button"
              :disabled="isSubmitting"
              @click="resetForm"
            >
              Notīrīt
            </button>
            <button class="btn btn-primary" type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? 'Pārbauda risinājumu...' : 'Saglabāt uzdevumu' }}
            </button>
          </div>
        </div>
      </article>
    </form>
  </div>
</template>
