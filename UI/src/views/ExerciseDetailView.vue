<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { isAuthenticated } from '../services/auth'
import {
  getExercise,
  submitCodeWithProgress,
  type ExerciseDetail,
  type SubmissionProgressEvent,
  type SubmissionResult,
} from '../services/exercises'

const route = useRoute()
const id = parseInt(route.params.id as string)

const exercise = ref<ExerciseDetail | null>(null)
const loading = ref(true)
const loadError = ref<string | null>(null)

const code = ref('')
const language = ref('python')
const submitting = ref(false)
const submitError = ref<string | null>(null)
const submitProgress = ref<SubmissionProgressEvent | null>(null)
const result = ref<SubmissionResult | null>(null)
const isSolvedNow = ref(false)
const isSubmitLockedUntilRefresh = ref(false)
const codeEditorRef = ref<HTMLTextAreaElement | null>(null)
const codeGutterRef = ref<HTMLDivElement | null>(null)

const editorLanguageVersions = {
  python: '3.11',
  java: '21',
} as const

const editorLanguageTemplates = {
  python: `# Raksti risinājumu šeit
`,
  java: `import java.util.Scanner;

public class Main {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        // Raksti risinājumu šeit
    }
}
`,
} as const

type EditorLanguageCode = keyof typeof editorLanguageTemplates

const languageDrafts = ref<Partial<Record<EditorLanguageCode, string>>>({})

type SamplePair = {
  title: string
  input: string
  output: string
}

function languageLabel(languageCode: string, languageVersion: string) {
  const code = languageCode.toLowerCase()

  if (code === 'python') {
    return `Python ${languageVersion}`
  }

  if (code === 'csharp') {
    return `C# ${languageVersion}`
  }

  if (code === 'java') {
    return `Java ${languageVersion}`
  }

  return `${languageCode} ${languageVersion}`.trim()
}

function isEditorLanguageCode(languageCode: string): languageCode is EditorLanguageCode {
  return languageCode in editorLanguageTemplates
}

function getEditorLanguageCode(languageCode: string): EditorLanguageCode {
  return isEditorLanguageCode(languageCode) ? languageCode : 'python'
}

function getEditorLanguageVersion(languageCode: EditorLanguageCode) {
  return editorLanguageVersions[languageCode]
}

function getStarterTemplate(languageCode: EditorLanguageCode) {
  return editorLanguageTemplates[languageCode]
}

const languages = computed(() => {
  return (Object.keys(editorLanguageTemplates) as EditorLanguageCode[]).map((code) => ({
    code,
    label: languageLabel(code, getEditorLanguageVersion(code)),
  }))
})

onMounted(async () => {
  try {
    exercise.value = await getExercise(id)
    isSolvedNow.value = exercise.value.isSolved
    const initialLanguage = getEditorLanguageCode(exercise.value.languageCode)
    language.value = initialLanguage
    const starterTemplate = getStarterTemplate(initialLanguage)
    code.value = starterTemplate
    languageDrafts.value[initialLanguage] = starterTemplate
  } catch (e: any) {
    loadError.value = e.message ?? 'Neizdevās ielādēt uzdevumu.'
  } finally {
    loading.value = false
  }
})

function handleLanguageChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const nextLanguage = getEditorLanguageCode(target.value)

  if (nextLanguage === language.value) {
    return
  }

  const currentLanguage = getEditorLanguageCode(language.value)
  languageDrafts.value[currentLanguage] = code.value
  language.value = nextLanguage
  code.value = languageDrafts.value[nextLanguage] ?? getStarterTemplate(nextLanguage)
}

async function handleSubmit() {
  if (!exercise.value || submitting.value || isSubmitLockedUntilRefresh.value || !code.value.trim()) return

  submitting.value = true
  submitError.value = null
  submitProgress.value = null
  result.value = null

  try {
    const finalResult = await submitCodeWithProgress(id, code.value, language.value, (event) => {
      submitProgress.value = event

      if (event.result) {
        result.value = event.result
      }
    })

    result.value = finalResult
    if (finalResult.status === 'Passed') {
      isSolvedNow.value = true
      isSubmitLockedUntilRefresh.value = true
    }
  } catch (e: any) {
    submitError.value = e.message ?? 'Neizdevās iesniegt kodu.'
  } finally {
    submitting.value = false
  }
}

function diffClass(d: string) {
  const v = d.toLowerCase()
  if (v === 'viegls' || v === 'easy') return 'diff-easy'
  if (v === 'grūts' || v === 'hard') return 'diff-hard'
  return 'diff-medium'
}

function statusLabel(s: string) {
  if (s === 'Passed') return 'Izpildīts'
  if (s === 'Failed') return 'Neizpildīts'
  if (s === 'TimedOut') return 'Laiks beidzies'
  if (s === 'Running') return 'Pārbauda'
  return 'Kļūda'
}

function statusClass(s: string) {
  if (s === 'Passed') return 'ex-status-passed'
  if (s === 'Failed') return 'ex-status-failed'
  return 'ex-status-error'
}

function ratingLabel(delta: number): string {
  if (delta > 0) return `+${delta} reitinga punkti`
  return ''
}

function editorLanguageLabel() {
  const currentLanguage = getEditorLanguageCode(language.value)
  return languageLabel(currentLanguage, getEditorLanguageVersion(currentLanguage))
}

const submitProgressText = computed(() => {
  if (!submitting.value) {
    return 'Iesniegt'
  }

  const current = submitProgress.value?.current ?? 0
  const total = submitProgress.value?.total ?? 0

  return total > 0 ? `Pārbauda ${current}/${total}` : 'Sāk pārbaudi...'
})

const progressPercent = computed(() => {
  const current = submitProgress.value?.current ?? 0
  const total = submitProgress.value?.total ?? 0

  return total > 0 ? Math.round((current / total) * 100) : 0
})

const progressCounter = computed(() => {
  const current = submitProgress.value?.current ?? 0
  const total = submitProgress.value?.total ?? 0

  return total > 0 ? `${current}/${total}` : '0/0'
})

const resultProgressPercent = computed(() => {
  if (!result.value || result.value.testsTotal <= 0) return 0
  return Math.round((result.value.testsPassed / result.value.testsTotal) * 100)
})

const codeLineNumbers = computed(() => {
  const lineCount = Math.max(1, code.value.split(/\r\n|\r|\n/).length)
  return Array.from({ length: lineCount }, (_, index) => index + 1)
})

function syncCodeEditorScroll() {
  if (!codeEditorRef.value || !codeGutterRef.value) {
    return
  }

  codeGutterRef.value.scrollTop = codeEditorRef.value.scrollTop
}

const samplePairs = computed<SamplePair[]>(() => {
  if (!exercise.value) {
    return []
  }

  // The first two visible test cases are the samples by convention.
  return exercise.value.visibleTestCases
    .slice(0, 2)
    .map((testCase, index) => ({
      title: `Piemērs ${index + 1}`,
      input: testCase.input,
      output: testCase.expectedOutput,
    }))
    .filter((pair) => pair.input.trim().length > 0 || pair.output.trim().length > 0)
})
</script>

<template>
  <div>
    <div class="mb-3">
      <RouterLink to="/exercises" class="ex-back-link">← Uzdevumi</RouterLink>
    </div>

    <div v-if="loading" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 text-secondary-emphasis small">Ielādē...</div>
    </div>
    <div v-else-if="loadError" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 text-danger small">{{ loadError }}</div>
    </div>

    <template v-else-if="exercise">
      <article class="content-panel card border-primary-subtle mb-3">
        <div class="card-body p-3 p-lg-4">
          <div class="ex-detail-header">
            <div class="ex-detail-title-row">
              <h1 class="section-heading mb-0">{{ exercise.title }}</h1>
              <span class="ex-diff-badge" :class="diffClass(exercise.difficulty)">
                {{ exercise.difficulty }}
              </span>
              <span class="ex-language-chip">Ieteikts: {{ languageLabel(exercise.languageCode, exercise.languageVersion) }}</span>
            </div>
            <div v-if="isSolvedNow" class="ex-solved-banner">
              <span class="ex-solved-check">✓</span> Izpildīts
            </div>
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle mb-3">
        <div class="card-body p-3 p-lg-4">
          <p class="ex-description mb-0">{{ exercise.description }}</p>

          <div v-if="samplePairs.length" class="ex-samples mt-3">
            <section v-for="pair in samplePairs" :key="pair.title" class="ex-sample">
              <div class="ex-sample__title">{{ pair.title }}</div>

              <div class="ex-sample__grid">
                <div class="ex-sample__block">
                  <div class="ex-sample-label">Parauga ievaddati</div>
                  <pre class="ex-sample-code">{{ pair.input }}</pre>
                </div>

                <div class="ex-sample__block">
                  <div class="ex-sample-label">Paredzamie izvaddati</div>
                  <pre class="ex-sample-code">{{ pair.output }}</pre>
                </div>
              </div>
            </section>
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle mb-3">
        <div class="card-body p-3 p-lg-4">
          <div class="ex-editor-header mb-3">
            <div class="ex-editor-title">
              <h2 class="section-heading mb-0">Risinājums</h2>
              <label class="ex-language-select-label" for="exerciseLanguage">Programmēšanas valoda un versija</label>
            </div>
            <select
              id="exerciseLanguage"
              :value="language"
              class="ex-lang-select auth-input"
              @change="handleLanguageChange"
            >
              <option v-for="l in languages" :key="l.code" :value="l.code">{{ l.label }}</option>
            </select>
          </div>

          <div class="ex-code-editor-shell">
            <div ref="codeGutterRef" class="ex-code-editor-gutter" aria-hidden="true">
              <span v-for="line in codeLineNumbers" :key="line" class="ex-code-editor-line">{{ line }}</span>
            </div>

            <textarea
              ref="codeEditorRef"
              v-model="code"
              class="ex-code-editor"
              wrap="off"
              :placeholder="`# Raksti savu ${editorLanguageLabel()} kodu šeit...`"
              spellcheck="false"
              autocomplete="off"
              autocorrect="off"
              autocapitalize="off"
              @input="syncCodeEditorScroll"
              @scroll="syncCodeEditorScroll"
            ></textarea>
          </div>

          <div v-if="submitError" class="text-danger mt-2 small">{{ submitError }}</div>

          <div v-if="!isAuthenticated" class="ex-auth-notice mt-3">
            <RouterLink to="/login">Ielogojies</RouterLink>, lai iesniegtu risinājumu.
          </div>

          <button
            v-else
            type="button"
            class="ex-submit-btn mt-3"
            :disabled="submitting || isSubmitLockedUntilRefresh || !code.trim()"
            @click="handleSubmit"
          >
            {{ submitProgressText }}
          </button>

          <div v-if="submitting" class="ex-submit-progress mt-3" aria-live="polite">
            <div class="ex-submit-progress__header">
              <span>Pārbaude</span>
              <strong>{{ progressCounter }}</strong>
            </div>
            <div class="ex-submit-progress__track">
              <div class="ex-submit-progress__bar" :style="{ width: `${progressPercent}%` }"></div>
            </div>
            <div class="ex-submit-progress__meta">
              {{ submitProgress?.testsPassed ?? 0 }} testi veiksmīgi
            </div>

          </div>
        </div>
      </article>

      <article v-if="result" class="content-panel card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="ex-result-header mb-3">
            <div class="d-flex align-items-center gap-3 flex-wrap">
              <h2 class="section-heading mb-0">Rezultāts</h2>
              <span class="ex-status-badge" :class="statusClass(result.status)">
                {{ statusLabel(result.status) }}
              </span>
              <span
                v-if="result.ratingDelta !== 0"
                class="ex-rating-delta"
                :class="result.ratingDelta > 0 ? 'ex-rating-pos' : 'ex-rating-neg'"
              >
                {{ ratingLabel(result.ratingDelta) }}
              </span>
            </div>
          </div>

          <p class="ex-result-summary mb-3">
            {{ result.testsPassed }} / {{ result.testsTotal }} testi veiksmīgi
          </p>

          <div class="ex-submit-progress__track mb-3" aria-hidden="true">
            <div class="ex-submit-progress__bar" :style="{ width: `${resultProgressPercent}%` }"></div>
          </div>

          <div v-if="result.errorMessage" class="ex-error-box mb-3">
            {{ result.errorMessage }}
          </div>
        </div>
      </article>
    </template>
  </div>
</template>
