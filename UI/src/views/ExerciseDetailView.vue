<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { hasAnyRole, isAuthenticated } from '../services/auth'
import {
  deleteExercise,
  getExercise,
  requestExerciseDescriptionEdit,
  submitCodeWithProgress,
  type ExerciseDetail,
  type SubmissionProgressEvent,
  type SubmissionResult,
} from '../services/exercises'

const route = useRoute()
const router = useRouter()
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
const codeHighlightRef = ref<HTMLPreElement | null>(null)
const isEditDescriptionModalOpen = ref(false)
const editDescription = ref('')
const editDescriptionError = ref<string | null>(null)
const editDescriptionNotice = ref<string | null>(null)
const isSendingEditDescriptionRequest = ref(false)
const isDeleteExerciseModalOpen = ref(false)
const deleteExerciseError = ref<string | null>(null)
const isDeletingExercise = ref(false)

const canRequestExerciseEdit = computed(() => hasAnyRole(['Pedagogs', 'Administrators']))
const canDeleteExercise = computed(() => hasAnyRole(['Administrators']))

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

function openEditDescriptionModal() {
  if (!exercise.value || !canRequestExerciseEdit.value) return
  editDescription.value = exercise.value.description
  editDescriptionError.value = null
  editDescriptionNotice.value = null
  isEditDescriptionModalOpen.value = true
}

function closeEditDescriptionModal() {
  if (isSendingEditDescriptionRequest.value) return
  isEditDescriptionModalOpen.value = false
}

async function submitDescriptionEditRequest() {
  if (!exercise.value || isSendingEditDescriptionRequest.value) return

  const description = editDescription.value.trim()
  editDescriptionError.value = null
  editDescriptionNotice.value = null

  if (!description) {
    editDescriptionError.value = 'Apraksts nedrīkst būt tukšs.'
    return
  }

  if (description === exercise.value.description.trim()) {
    editDescriptionError.value = 'Apraksts nav mainīts.'
    return
  }

  isSendingEditDescriptionRequest.value = true
  try {
    await requestExerciseDescriptionEdit(exercise.value.id, { description })
    exercise.value = {
      ...exercise.value,
      hasPendingDescriptionEditRequest: true,
    }
    editDescriptionNotice.value = 'Pieprasījums nosūtīts administratoram.'
    isEditDescriptionModalOpen.value = false
  } catch (error) {
    editDescriptionError.value = error instanceof Error ? error.message : 'Neizdevās nosūtīt labojuma pieprasījumu.'
  } finally {
    isSendingEditDescriptionRequest.value = false
  }
}

function openDeleteExerciseModal() {
  if (!exercise.value || !canDeleteExercise.value) return
  deleteExerciseError.value = null
  isDeleteExerciseModalOpen.value = true
}

function closeDeleteExerciseModal() {
  if (isDeletingExercise.value) return
  isDeleteExerciseModalOpen.value = false
}

async function confirmDeleteExercise() {
  if (!exercise.value || isDeletingExercise.value) return

  isDeletingExercise.value = true
  deleteExerciseError.value = null
  try {
    await deleteExercise(exercise.value.id)
    await router.push({ name: 'exercises' })
  } catch (error) {
    deleteExerciseError.value = error instanceof Error ? error.message : 'Neizdevās dzēst uzdevumu.'
  } finally {
    isDeletingExercise.value = false
  }
}

function diffClass(d: string) {
  const v = d.toLowerCase()
  if (v === 'viegls' || v === 'easy') return 'theory-difficulty--iesācējs'
  if (v === 'grūts' || v === 'hard') return 'theory-difficulty--augsts'
  return 'theory-difficulty--vidējs'
}

function diffLabel(d: string) {
  const v = d.toLowerCase()
  if (v === 'viegls' || v === 'easy') return 'Viegls'
  if (v === 'grūts' || v === 'hard') return 'Grūts'
  return 'Vidējs'
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

const failedTestResults = computed(() => {
  return result.value?.testResults?.filter((tc) => !tc.passed) ?? []
})

const codeLineNumbers = computed(() => {
  const lineCount = Math.max(1, code.value.split(/\r\n|\r|\n/).length)
  return Array.from({ length: lineCount }, (_, index) => index + 1)
})

function syncCodeEditorScroll() {
  if (!codeEditorRef.value) {
    return
  }

  if (codeGutterRef.value) {
    codeGutterRef.value.scrollTop = codeEditorRef.value.scrollTop
  }
  if (codeHighlightRef.value) {
    codeHighlightRef.value.scrollTop = codeEditorRef.value.scrollTop
    codeHighlightRef.value.scrollLeft = codeEditorRef.value.scrollLeft
  }
}

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

const highlightedCode = computed(() => {
  const lang = (language.value || '').toLowerCase()
  // Trailing newline ensures the last line is fully visible inside <pre>
  const source = code.value + '\n'

  if (lang === 'python') {
    return highlightPython(source)
  }
  if (lang === 'java' || lang === 'csharp' || lang === 'cpp' || lang === 'c' || lang === 'javascript') {
    return highlightCStyle(source)
  }

  return escapeHtml(source)
})

function highlightPython(source: string): string {
  // # comments līdz rindas beigām
  let result = ''
  let i = 0
  while (i < source.length) {
    const ch = source[i]
    if (ch === '#') {
      const newline = source.indexOf('\n', i)
      const end = newline === -1 ? source.length : newline
      const comment = source.slice(i, end)
      result += `<span class="ex-code-comment">${escapeHtml(comment)}</span>`
      i = end
    } else if (ch === '"' || ch === "'") {
      // Saturs starp pēdiņām neuzskata par komentāru
      const quote = ch
      let end = i + 1
      while (end < source.length && source[end] !== quote) {
        if (source[end] === '\\') end += 1
        end += 1
      }
      end = Math.min(end + 1, source.length)
      result += escapeHtml(source.slice(i, end))
      i = end
    } else {
      const newline = source.indexOf('\n', i)
      const end = newline === -1 ? source.length : newline + 1
      // Pārējā rindas daļa līdz # vai pēdiņām — ātri pārbaudām vēlreiz pa simbolam
      let chunk = ''
      let j = i
      while (j < end && source[j] !== '#' && source[j] !== '"' && source[j] !== "'") {
        chunk += source[j]
        j += 1
      }
      result += escapeHtml(chunk)
      i = j
    }
  }
  return result
}

function highlightCStyle(source: string): string {
  let result = ''
  let i = 0
  while (i < source.length) {
    // Block komentārs /* ... */
    if (source[i] === '/' && source[i + 1] === '*') {
      const close = source.indexOf('*/', i + 2)
      const end = close === -1 ? source.length : close + 2
      result += `<span class="ex-code-comment">${escapeHtml(source.slice(i, end))}</span>`
      i = end
      continue
    }
    // Line komentārs // ... (neuzskata, ja tas ir iekšā stringā — vienkāršots)
    if (source[i] === '/' && source[i + 1] === '/') {
      const newline = source.indexOf('\n', i)
      const end = newline === -1 ? source.length : newline
      result += `<span class="ex-code-comment">${escapeHtml(source.slice(i, end))}</span>`
      i = end
      continue
    }
    // String literāls "..."
    if (source[i] === '"' || source[i] === "'") {
      const quote = source[i]
      let end = i + 1
      while (end < source.length && source[end] !== quote) {
        if (source[end] === '\\') end += 1
        end += 1
      }
      end = Math.min(end + 1, source.length)
      result += escapeHtml(source.slice(i, end))
      i = end
      continue
    }
    result += escapeHtml(source[i])
    i += 1
  }
  return result
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
      <RouterLink to="/exercises" class="app-back-link">← Uzdevumi</RouterLink>
    </div>

    <div v-if="loading" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 text-secondary-emphasis small">Ielādē...</div>
    </div>
    <div v-else-if="loadError" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4 text-danger small">{{ loadError }}</div>
    </div>

    <template v-else-if="exercise">
      <article
        class="content-panel card border-primary-subtle mb-3"
        :class="{ 'theory-list-item--done': isSolvedNow }"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="theory-header exercises-header exercise-detail-header">
            <span class="page-title-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="16 18 22 12 16 6" />
                <polyline points="8 6 2 12 8 18" />
              </svg>
            </span>
            <div class="theory-heading-copy">
              <h1 class="section-heading section-heading--with-mark mb-0">
                <span v-if="isSolvedNow" class="ex-solved-mark" aria-label="Izpildīts">✓</span>
                <span class="section-heading__text">{{ exercise.title }}</span>
              </h1>
            </div>
            <div class="exercises-header__badges">
              <span class="theory-difficulty" :class="diffClass(exercise.difficulty)">
                {{ diffLabel(exercise.difficulty) }}
              </span>
              <span class="ex-language-chip">Ieteikts: {{ languageLabel(exercise.languageCode, exercise.languageVersion) }}</span>
            </div>
            <div v-if="canRequestExerciseEdit || canDeleteExercise" class="theory-header__actions exercise-detail-actions">
              <button
                v-if="canRequestExerciseEdit"
                class="btn btn-theory-edit btn-sm"
                type="button"
                :disabled="exercise.hasPendingDescriptionEditRequest"
                @click="openEditDescriptionModal"
              >
                {{ exercise.hasPendingDescriptionEditRequest ? 'Labojums nosūtīts' : 'Rediģēt aprakstu' }}
              </button>
              <button
                v-if="canDeleteExercise"
                class="btn btn-theory-delete btn-sm"
                type="button"
                @click="openDeleteExerciseModal"
              >
                Dzēst
              </button>
            </div>
          </div>
        </div>
      </article>

      <article class="content-panel card border-primary-subtle mb-3">
        <div class="card-body p-3 p-lg-4">
          <p class="ex-description mb-0">{{ exercise.description }}</p>
          <hr v-if="samplePairs.length" class="exercise-description-divider" />

          <div v-if="samplePairs.length" class="ex-samples">
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
            <h2 class="section-heading section-heading--caps mb-0">Risinājums</h2>
            <select
              id="exerciseLanguage"
              :value="language"
              aria-label="Programmēšanas valoda un versija"
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

            <div class="ex-code-editor-area">
              <pre
                ref="codeHighlightRef"
                class="ex-code-editor-highlight"
                aria-hidden="true"
                v-html="highlightedCode"
              ></pre>
              <textarea
                ref="codeEditorRef"
                v-model="code"
                class="ex-code-editor ex-code-editor--transparent"
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
          </div>

          <div v-if="submitError" class="text-danger mt-2 small">{{ submitError }}</div>

          <div v-if="!isAuthenticated" class="ex-auth-notice mt-3">
            <RouterLink to="/login">Ielogojies</RouterLink>, lai iesniegtu risinājumu.
          </div>

          <div v-else-if="isSolvedNow" class="alert alert-success mt-3 mb-0 ex-already-solved">
            <strong>✓ Šis uzdevums jau ir atrisināts.</strong>
            Atkārtota iesniegšana nav atļauta.
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
            <strong>Kompilācijas/izpildes kļūda:</strong>
            <pre class="ex-error-detail">{{ result.errorMessage }}</pre>
          </div>

          <div v-if="failedTestResults.length" class="ex-test-list">
            <h3 class="ex-test-list__heading">Neizpildītie testi</h3>
            <article
              v-for="(tc, idx) in failedTestResults"
              :key="idx"
              class="ex-test-case ex-test-case--failed"
            >
              <div class="ex-test-case__head">
                <span class="ex-test-case__index">Tests {{ tc.orderIndex + 1 }}</span>
                <span class="ex-test-case__status is-fail">✗ Neizpildīts</span>
                <span v-if="tc.isHidden" class="ex-test-case__hidden">Slēpts</span>
              </div>

              <div v-if="!tc.isHidden" class="ex-test-case__body">
                <div v-if="tc.errorMessage" class="ex-test-case__error">
                  <span class="ex-test-case__label">Kļūda</span>
                  <pre class="ex-test-case__pre">{{ tc.errorMessage }}</pre>
                </div>

                <div class="ex-test-case__diff">
                  <div class="ex-test-case__col">
                    <span class="ex-test-case__label">Paredzamais izvads</span>
                    <pre class="ex-test-case__pre ex-test-case__pre--expected">{{ tc.expectedOutput || '(tukšs)' }}</pre>
                  </div>
                  <div class="ex-test-case__col">
                    <span class="ex-test-case__label">Tavs izvads</span>
                    <pre class="ex-test-case__pre ex-test-case__pre--actual">{{ tc.actualOutput || '(tukšs)' }}</pre>
                  </div>
                </div>
              </div>

              <div v-else class="ex-test-case__body">
                <p class="ex-test-case__hidden-msg mb-0">
                  Šis tests ir slēpts, tāpēc tā detaļas nav redzamas. Pārbaudi savu kodu uz citiem ievades gadījumiem.
                </p>
              </div>
            </article>
          </div>
        </div>
      </article>
    </template>

    <div v-if="isEditDescriptionModalOpen" class="app-modal-backdrop" @click.self="closeEditDescriptionModal">
      <div class="app-modal app-modal--sm card border-primary-subtle" role="dialog" aria-modal="true" aria-labelledby="editExerciseDescriptionTitle">
        <div class="card-body p-3 p-lg-4">
          <h2 id="editExerciseDescriptionTitle" class="section-heading section-heading--caps mb-3">Mainīt aprakstu</h2>
          <p class="text-secondary mb-3">
            Tiks nosūtīts pieprasījums administratoram. Šeit var mainīt tikai uzdevuma aprakstu.
          </p>

          <label class="representation-field">
            <span class="representation-field__label">Apraksts</span>
            <textarea
              v-model="editDescription"
              class="form-control auth-input"
              rows="7"
              maxlength="4000"
            ></textarea>
          </label>

          <div v-if="editDescriptionError" class="alert alert-danger mt-3 mb-0">{{ editDescriptionError }}</div>
          <div v-if="editDescriptionNotice" class="alert alert-success mt-3 mb-0">{{ editDescriptionNotice }}</div>

          <div class="d-flex justify-content-end gap-2 mt-3">
            <button class="btn btn-outline-light" type="button" :disabled="isSendingEditDescriptionRequest" @click="closeEditDescriptionModal">Atcelt</button>
            <button class="btn btn-primary" type="button" :disabled="isSendingEditDescriptionRequest" @click="submitDescriptionEditRequest">
              {{ isSendingEditDescriptionRequest ? 'Sūta...' : 'Nosūtīt pieprasījumu' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="isDeleteExerciseModalOpen" class="app-modal-backdrop" @click.self="closeDeleteExerciseModal">
      <div class="app-modal app-modal--sm card border-primary-subtle delete-modal" role="dialog" aria-modal="true" aria-labelledby="deleteExerciseTitle">
        <div class="card-body p-3 p-lg-4">
          <div class="delete-modal__icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 6h18" />
              <path d="M8 6V4h8v2" />
              <path d="M19 6l-1 14H6L5 6" />
              <path d="M10 11v5" />
              <path d="M14 11v5" />
            </svg>
          </div>
          <h2 id="deleteExerciseTitle" class="section-heading delete-modal__title mb-3">Dzēst uzdevumu?</h2>
          <p class="delete-modal__text mb-4">
            “{{ exercise?.title }}” un visi iesniegumi šim uzdevumam tiks dzēsti.
          </p>
          <div v-if="deleteExerciseError" class="alert alert-danger mb-3">{{ deleteExerciseError }}</div>
          <div class="delete-modal__actions">
            <button class="btn btn-outline-light" type="button" :disabled="isDeletingExercise" @click="closeDeleteExerciseModal">Atcelt</button>
            <button class="btn btn-primary delete-modal__confirm" type="button" :disabled="isDeletingExercise" @click="confirmDeleteExercise">
              <span v-if="isDeletingExercise" class="logout-modal__spinner" aria-hidden="true"></span>
              {{ isDeletingExercise ? 'Dzēš...' : 'Dzēst' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
