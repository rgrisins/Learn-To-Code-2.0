import { createRouter, createWebHistory } from 'vue-router'
import { hasAnyRole, isAuthenticated } from '../services/auth'
import HomeView from '../views/HomeView.vue'
import AboutView from '../views/AboutView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import TheoryView from '../views/TheoryView.vue'
import TheoryQuizView from '../views/TheoryQuizView.vue'
import TasksView from '../views/TasksView.vue'
import RatingView from '../views/RatingView.vue'
import ProfileView from '../views/ProfileView.vue'
import UserProfileView from '../views/UserProfileView.vue'
import ForbiddenView from '../views/ForbiddenView.vue'
import AdminView from '../views/AdminView.vue'
import ExercisesView from '../views/ExercisesView.vue'
import ExerciseDetailView from '../views/ExerciseDetailView.vue'
import SubmissionsView from '../views/SubmissionsView.vue'
import RepresentationsView from '../views/RepresentationsView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/learning',
      name: 'learning',
      component: HomeView,
    },
    {
      path: '/about',
      name: 'about',
      component: AboutView,
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { publicOnly: true },
    },
    {
      path: '/register',
      name: 'register',
      component: RegisterView,
      meta: { publicOnly: true },
    },
    {
      path: '/theory',
      name: 'theory',
      component: TheoryView,
    },
    {
      path: '/theory/:languageId/:topicId/quiz',
      name: 'theory-quiz',
      component: TheoryQuizView,
    },
    {
      path: '/tasks',
      name: 'tasks',
      component: TasksView,
      meta: { requiresAuth: true, roles: ['Pedagogs', 'Administrators'] },
    },
    {
      path: '/exercises',
      name: 'exercises',
      component: ExercisesView,
      meta: { requiresAuth: true },
    },
    {
      path: '/exercises/:id',
      name: 'exercise',
      component: ExerciseDetailView,
      meta: { requiresAuth: true },
    },
    {
      path: '/submissions',
      name: 'submissions',
      component: SubmissionsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/representations',
      name: 'representations',
      component: RepresentationsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/ratings',
      name: 'ratings',
      component: RatingView,
    },
    {
      path: '/profile',
      name: 'profile',
      component: ProfileView,
      meta: { requiresAuth: true },
    },
    {
      path: '/users/:id',
      name: 'public-profile',
      component: UserProfileView,
    },
    {
      path: '/admin',
      name: 'admin',
      component: AdminView,
      meta: { requiresAuth: true, roles: ['Administrators'] },
    },
    {
      path: '/forbidden',
      name: 'forbidden',
      component: ForbiddenView,
    },
  ],
})

router.beforeEach((to) => {
  if (to.meta.publicOnly && isAuthenticated.value) {
    return { name: 'profile' }
  }

  if (to.meta.requiresAuth && !isAuthenticated.value) {
    return {
      name: 'login',
      query: { redirect: to.fullPath },
    }
  }

  if (Array.isArray(to.meta.roles) && to.meta.roles.length > 0) {
    const roles = to.meta.roles as string[]
    if (!hasAnyRole(roles)) {
      return {
        name: 'forbidden',
        query: { required: roles.join(',') },
      }
    }
  }

  return true
})

export default router
