<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authState, isAuthenticated } from '../services/auth'
import { getTheoryLanguage, type TheoryLanguageDetail, type TheoryTopic } from '../services/theory'
import algoritmiLogo from '../assets/algoritmi.png'

const ALGORITHM_TITLES = new Set(['algoritmi'])

function getLanguageImageUrl(lang: { title: string; imageUrl: string }): string {
  return ALGORITHM_TITLES.has(lang.title.trim().toLowerCase()) ? algoritmiLogo : lang.imageUrl
}
import {
  getTheoryQuiz,
  submitTheoryQuizAnswer,
  type TheoryQuiz,
  type TheoryQuizAnswerResult,
  type TheoryQuizOption,
  type TheoryQuizQuestion,
} from '../services/theoryQuizzes'

interface AnswerReview {
  selectedOptionId: number | null
  correctOptionId: number | null
  isCorrect: boolean
  ratingAwarded: boolean
  ratingDelta: number
  explanation: string | null
}

const route = useRoute()
const router = useRouter()

const languageCode = computed(() => String(route.params.languageId ?? ''))
const topicSlug = computed(() => String(route.params.topicId ?? ''))

const quiz = ref<TheoryQuiz | null>(null)
const language = ref<TheoryLanguageDetail | null>(null)
const isLoading = ref(false)
const loadError = ref('')

const questionIndex = ref(0)
const selectedOptionId = ref<number | null>(null)
const lastResult = ref<TheoryQuizAnswerResult | null>(null)
const answeredQuestionIds = ref(new Set<number>())
const correctQuestionIds = ref(new Set<number>())
const totalRatingAwarded = ref(0)
const isSubmitting = ref(false)
const showFinishScreen = ref(false)

const currentQuestion = computed<TheoryQuizQuestion | null>(() => {
  if (!quiz.value) return null
  return quiz.value.questions[questionIndex.value] ?? null
})

const totalQuestions = computed(() => quiz.value?.questions.length ?? 0)
const answeredSoFar = computed(() => answeredQuestionIds.value.size)
const correctSoFar = computed(() => correctQuestionIds.value.size)
const scorePercent = computed(() => {
  const total = totalQuestions.value
  if (!total) return 0
  return Math.round((correctSoFar.value / total) * 100)
})
const isLastQuestion = computed(
  () => quiz.value !== null && questionIndex.value === quiz.value.questions.length - 1,
)
const nextTopic = computed<TheoryTopic | null>(() => {
  if (!language.value || !quiz.value) return null
  const currentIndex = language.value.topics.findIndex((topic) => topic.id === quiz.value?.topicSlug)
  return currentIndex >= 0 ? language.value.topics[currentIndex + 1] ?? null : null
})
const currentQuestionLocked = computed(() =>
  Boolean(currentQuestion.value && answeredQuestionIds.value.has(currentQuestion.value.id)),
)

const submittedResult = computed<TheoryQuizAnswerResult | null>(() => {
  const question = currentQuestion.value
  if (!question || lastResult.value?.questionId !== question.id) return null
  return lastResult.value
})

const nextAnsweredActionLabel = computed(() => {
  if (!isLastQuestion.value) return 'Nākamais jautājums →'
  return nextTopic.value ? 'Nākamā teorijas tēma →' : 'Atpakaļ uz tēmu'
})

const answerReview = computed<AnswerReview | null>(() => {
  const question = currentQuestion.value
  if (!question) return null

  if (submittedResult.value) {
    return {
      selectedOptionId: submittedResult.value.selectedOptionId,
      correctOptionId: submittedResult.value.correctOptionId,
      isCorrect: submittedResult.value.isCorrect,
      ratingAwarded: submittedResult.value.ratingAwarded,
      ratingDelta: submittedResult.value.ratingDelta,
      explanation: submittedResult.value.explanation,
    }
  }

  if (!answeredQuestionIds.value.has(question.id)) return null

  const correctOptionId = getQuestionCorrectOptionId(question)
  const selectedAnswerId = getQuestionSelectedOptionId(question)

  return {
    selectedOptionId: selectedAnswerId,
    correctOptionId,
    isCorrect: question.isAnsweredCorrectly,
    ratingAwarded: false,
    ratingDelta: 0,
    explanation: question.explanation,
  }
})

const selectedOptionText = computed(() => {
  const optionId = answerReview.value?.selectedOptionId
  if (optionId == null) return ''
  return currentQuestion.value?.options.find((option) => option.id === optionId)?.text ?? ''
})

const correctOptionText = computed(() => {
  const optionId = answerReview.value?.correctOptionId
  if (optionId == null) return ''
  return currentQuestion.value?.options.find((option) => option.id === optionId)?.text ?? ''
})

const progressPercent = computed(() => {
  const total = totalQuestions.value
  if (!total) return 0
  return Math.round(((questionIndex.value + 1) / total) * 100)
})

watch(currentQuestion, (question) => {
  selectedOptionId.value = question ? getQuestionSelectedOptionId(question) : null
  lastResult.value = null
  if (question?.isAnswered) {
    answeredQuestionIds.value.add(question.id)
    if (question.isAnsweredCorrectly) {
      correctQuestionIds.value.add(question.id)
    }
  }
})

onMounted(loadQuiz)

async function loadQuiz() {
  loadError.value = ''
  isLoading.value = true
  showFinishScreen.value = false
  try {
    const [result, languageResult] = await Promise.all([
      getTheoryQuiz(languageCode.value, topicSlug.value),
      getTheoryLanguage(languageCode.value),
    ])
    const preparedQuiz = prepareQuizForDisplay(result)
    quiz.value = preparedQuiz
    language.value = languageResult
    questionIndex.value = 0
    selectedOptionId.value = preparedQuiz.questions[0]
      ? getQuestionSelectedOptionId(preparedQuiz.questions[0])
      : null
    lastResult.value = null

    answeredQuestionIds.value = new Set(
      preparedQuiz.questions.filter((q) => q.isAnswered).map((q) => q.id),
    )
    correctQuestionIds.value = new Set(
      preparedQuiz.questions.filter((q) => q.isAnsweredCorrectly).map((q) => q.id),
    )
    totalRatingAwarded.value = 0
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt testu.'
  } finally {
    isLoading.value = false
  }
}

async function submitAnswer() {
  if (!quiz.value || !currentQuestion.value || selectedOptionId.value == null) return
  if (currentQuestionLocked.value) return
  if (!isAuthenticated.value) {
    loadError.value = 'Lūdzu ielogojies, lai iesniegtu atbildi.'
    return
  }

  isSubmitting.value = true
  try {
    const result = await submitTheoryQuizAnswer(
      languageCode.value,
      topicSlug.value,
      currentQuestion.value.id,
      selectedOptionId.value,
    )
    lastResult.value = result
    answeredQuestionIds.value.add(currentQuestion.value.id)
    currentQuestion.value.isAnswered = true
    currentQuestion.value.isAnsweredCorrectly = result.isCorrect
    currentQuestion.value.selectedOptionId = result.selectedOptionId
    currentQuestion.value.correctOptionId = result.correctOptionId
    if (result.isCorrect) {
      correctQuestionIds.value.add(currentQuestion.value.id)
    } else {
      correctQuestionIds.value.delete(currentQuestion.value.id)
    }
    if (result.ratingAwarded) {
      totalRatingAwarded.value += result.ratingDelta
      if (authState.user) {
        authState.user.rating += result.ratingDelta
      }
    }
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Neizdevās iesniegt atbildi.'
  } finally {
    isSubmitting.value = false
  }
}

function goToQuestion(index: number) {
  if (!quiz.value) return
  if (index < 0 || index >= quiz.value.questions.length) return
  questionIndex.value = index
}

function goNext() {
  if (!quiz.value) return
  if (isLastQuestion.value) {
    if (nextTopic.value) {
      goToNextTopic()
    } else {
      backToTopic()
    }
    return
  }
  questionIndex.value += 1
}

function goPrevious() {
  if (questionIndex.value > 0) {
    questionIndex.value -= 1
  }
}

function backToTopic() {
  router.push({
    name: 'theory',
    query: { language: languageCode.value, topic: topicSlug.value, page: 1 },
  })
}

function goToLanguages() {
  router.push({ name: 'theory' })
}

function goToLanguageTopics() {
  router.push({ name: 'theory', query: { language: languageCode.value } })
}

function goToNextTopic() {
  if (!nextTopic.value) return
  router.push({
    name: 'theory',
    query: { language: languageCode.value, topic: nextTopic.value.id, page: 1 },
  })
}

function restartReview() {
  showFinishScreen.value = false
  questionIndex.value = 0
}

function prepareQuizForDisplay(sourceQuiz: TheoryQuiz): TheoryQuiz {
  return {
    ...sourceQuiz,
    questions: sourceQuiz.questions.map((question) => ({
      ...question,
      options: arrangeQuizOptions(question),
    })),
  }
}

function arrangeQuizOptions(question: TheoryQuizQuestion): TheoryQuizOption[] {
  const options = [...question.options]
  if (options.length <= 1) return options

  let shuffleState = hashOptionState(`${question.id}:${question.prompt}`)
  for (let index = options.length - 1; index > 0; index -= 1) {
    shuffleState = nextOptionState(shuffleState)
    const swapIndex = shuffleState % (index + 1)
    ;[options[index], options[swapIndex]] = [options[swapIndex], options[index]]
  }

  const firstOriginalOptionId = question.options[0]?.id
  if (options[0]?.id === firstOriginalOptionId) {
    options.push(options.shift()!)
  }

  return options
}

function hashOptionState(value: string): number {
  let hash = 2166136261
  for (const character of value) {
    hash ^= character.charCodeAt(0)
    hash = Math.imul(hash, 16777619)
  }

  return hash >>> 0
}

function nextOptionState(state: number): number {
  return (Math.imul(state, 1664525) + 1013904223) >>> 0
}

function getQuestionSelectedOptionId(question: TheoryQuizQuestion): number | null {
  if (question.selectedOptionId != null) return question.selectedOptionId
  return question.isAnsweredCorrectly ? getQuestionCorrectOptionId(question) : null
}

function getQuestionCorrectOptionId(question: TheoryQuizQuestion): number | null {
  if (question.correctOptionId != null) return question.correctOptionId
  if (question.isAnsweredCorrectly && question.selectedOptionId != null) return question.selectedOptionId
  return question.isAnsweredCorrectly ? inferOptionIdFromExplanation(question) : null
}

function inferOptionIdFromExplanation(question: TheoryQuizQuestion): number | null {
  const explanation = normalizeAnswerText(question.explanation ?? '')
  if (!explanation) return null

  const matchingOptions = question.options.filter((option) => {
    const optionText = normalizeAnswerText(option.text)
    return optionText.length >= 2 && explanation.includes(optionText)
  })

  return matchingOptions.length === 1 ? matchingOptions[0].id : null
}

function normalizeAnswerText(value: string): string {
  return value.toLocaleLowerCase('lv-LV').replace(/\s+/g, ' ').trim()
}

function isOptionCorrect(optionId: number): boolean {
  return answerReview.value?.correctOptionId === optionId
}

function isOptionSelected(optionId: number): boolean {
  return (answerReview.value ? answerReview.value.selectedOptionId : selectedOptionId.value) === optionId
}

function isOptionIncorrect(optionId: number): boolean {
  const review = answerReview.value
  return Boolean(review && review.selectedOptionId === optionId && review.correctOptionId !== optionId)
}

function questionStatus(question: TheoryQuizQuestion, index: number): string {
  if (correctQuestionIds.value.has(question.id)) return 'correct'
  if (answeredQuestionIds.value.has(question.id)) return 'answered'
  if (index === questionIndex.value) return 'current'
  return 'pending'
}
</script>

<template>
  <div class="theory-breadcrumb mb-3">
    <button class="app-back-link" type="button" @click="goToLanguages">
      <span aria-hidden="true">←</span>
      Teorija
    </button>
    <button v-if="language" class="app-back-link" type="button" @click="goToLanguageTopics">
      <span aria-hidden="true">←</span>
      {{ language.title }}
    </button>
    <button class="app-back-link" type="button" @click="backToTopic">
      <span aria-hidden="true">←</span>
      {{ quiz?.topicTitle ?? 'Tēma' }}
    </button>
  </div>

  <section class="content-panel card border-primary-subtle quiz-panel">
    <div class="card-body p-3 p-lg-4">
      <div class="theory-header">
        <img
          v-if="language"
          class="theory-language-badge"
          :src="getLanguageImageUrl(language)"
          :alt="language.title"
        />
        <div class="theory-heading-copy">
          <h1 class="section-heading mb-0">{{ quiz?.title ?? 'Tests' }}</h1>
          <p v-if="quiz?.description" class="theory-lead mb-0 mt-1">{{ quiz.description }}</p>
        </div>
        <div v-if="quiz" class="theory-language-progress theory-header__progress" aria-label="Testa progress">
          <div class="theory-language-progress__label">
            <strong>{{ scorePercent }}% apgūts</strong>
          </div>
          <span class="theory-language-progress__track" aria-hidden="true">
            <span
              class="theory-language-progress__bar"
              :style="{ width: `${scorePercent}%` }"
            ></span>
          </span>
        </div>
      </div>

      <div v-if="isLoading" class="theory-empty">Ielādē testu...</div>

      <div v-else-if="loadError" class="alert alert-danger">{{ loadError }}</div>

      <template v-else-if="quiz">
        <div class="quiz-progress" aria-label="Jautājumu navigācija">
          <div class="quiz-progress__nav">
            <button
              v-for="(question, index) in quiz.questions"
              :key="question.id"
              type="button"
              class="quiz-progress__step"
              :class="`quiz-progress__step--${questionStatus(question, index)}`"
              :aria-label="`Jautājums ${index + 1}`"
              @click="goToQuestion(index)"
            >
              {{ index + 1 }}
            </button>
          </div>
        </div>

        <template v-if="!showFinishScreen && currentQuestion">
          <article class="quiz-question">
            <div class="quiz-question__head">
              <span class="quiz-question__counter">
                Jautājums {{ questionIndex + 1 }} / {{ totalQuestions }}
              </span>
              <span
                v-if="currentQuestion.isAnsweredCorrectly"
                class="quiz-question__solved"
              >
                ✓ jau atbildēts pareizi
              </span>
              <span
                v-else-if="currentQuestionLocked"
                class="quiz-question__locked"
              >
                Jau atbildēts
              </span>
            </div>
            <h2 class="quiz-question__prompt">{{ currentQuestion.prompt }}</h2>

            <ol class="quiz-options">
              <li
                v-for="option in currentQuestion.options"
                :key="option.id"
                class="quiz-option"
                :class="{
                  'is-selected': isOptionSelected(option.id) && !answerReview,
                  'is-correct': isOptionCorrect(option.id),
                  'is-incorrect': isOptionIncorrect(option.id),
                  'is-locked': !!answerReview || currentQuestionLocked,
                }"
              >
                <label>
                  <input
                    type="radio"
                    :name="`question-${currentQuestion.id}`"
                    :value="option.id"
                    :checked="isOptionSelected(option.id)"
                    :disabled="!!answerReview || currentQuestionLocked"
                    @change="selectedOptionId = option.id"
                  />
                  <span>{{ option.text }}</span>
                </label>
              </li>
            </ol>

            <div
              v-if="answerReview"
              class="quiz-feedback"
              :class="answerReview.isCorrect ? 'is-success' : 'is-error'"
            >
              <strong>{{ answerReview.isCorrect ? 'Pareizi!' : 'Nepareizi.' }}</strong>
              <span v-if="answerReview.ratingAwarded">
                +{{ answerReview.ratingDelta }} reitinga punkts.
              </span>
              <p v-if="selectedOptionText" class="quiz-feedback__choice">
                Tava atbilde: <strong>{{ selectedOptionText }}</strong>
              </p>
              <p v-if="!answerReview.isCorrect && correctOptionText" class="quiz-feedback__choice">
                Pareizā atbilde: <strong>{{ correctOptionText }}</strong>
              </p>
              <p v-if="answerReview.explanation" class="quiz-feedback__explanation mb-0">
                <strong>Skaidrojums:</strong> {{ answerReview.explanation }}
              </p>
            </div>
          </article>

          <div class="quiz-actions">
            <button
              class="btn btn-outline-light"
              type="button"
              :disabled="questionIndex === 0"
              @click="goPrevious"
            >
              Iepriekšējais
            </button>

            <button
              v-if="!answerReview && !currentQuestionLocked"
              class="btn btn-quiz-action"
              type="button"
              :disabled="selectedOptionId == null || isSubmitting"
              @click="submitAnswer"
            >
              {{ isSubmitting ? 'Pārbauda...' : 'Pārbaudīt atbildi' }}
            </button>

            <button
              v-else
              class="btn btn-quiz-action"
              type="button"
              @click="goNext"
            >
              {{ nextAnsweredActionLabel }}
            </button>
          </div>
        </template>

        <template v-else-if="showFinishScreen">
          <article class="quiz-finish">
            <div class="quiz-finish__summary">
              <span class="quiz-finish__eyebrow">Rezultāts</span>
              <h2 class="section-heading mb-2">Tests pabeigts</h2>
              <p class="mb-0">
                Tu pareizi atbildēji uz <strong>{{ correctSoFar }} no {{ totalQuestions }}</strong> jautājumiem.
              </p>
            </div>
            <div class="quiz-finish__score">
              <strong>{{ scorePercent }}%</strong>
              <span>pareizi</span>
            </div>
            <div class="quiz-actions">
              <button class="btn btn-outline-light" type="button" @click="restartReview">
                Pārskatīt jautājumus
              </button>
              <button class="btn btn-outline-light" type="button" @click="backToTopic">
                Atpakaļ uz tēmu
              </button>
              <button
                v-if="nextTopic"
                class="btn btn-quiz-action"
                type="button"
                @click="goToNextTopic"
              >
                Nākamā tēma: {{ nextTopic.title }}
              </button>
            </div>
          </article>
        </template>
      </template>
    </div>
  </section>
</template>
