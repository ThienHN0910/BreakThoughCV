import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

import LoginPage from '../pages/Login.vue'
import SelectRolePage from '../pages/SelectRole.vue'
import DashboardPage from '../pages/Dashboard.vue'
import JobListPage from '../pages/JobList.vue'
import CVManagementPage from '../pages/CVManagement.vue'
import CompanyManagementPage from '../pages/CompanyManagement.vue'
import JobManagementPage from '../pages/JobManagement.vue'
import ApplicationManagementPage from '../pages/ApplicationManagement.vue'
import AIReviewPage from '../pages/AIReview.vue'
import AiPurchaseHistoryPage from '../pages/AiPurchaseHistory.vue'
import NotificationsPage from '../pages/Notifications.vue'
import UserManagementPage from '../pages/admin/UserManagement.vue'

const routes = [
  { path: '/login', component: LoginPage },
  { path: '/select-role', component: SelectRolePage, meta: { requiresAuth: true } },
  { path: '/', component: DashboardPage, meta: { requiresAuth: true } },
  { path: '/jobs', component: JobListPage, meta: { requiresAuth: true } },
  { path: '/candidate/cv', component: CVManagementPage, meta: { requiresAuth: true, role: 'candidate' } },
  { path: '/candidate/ai-review', component: AIReviewPage, meta: { requiresAuth: true, role: 'candidate' } },
  { path: '/candidate/ai-purchases', component: AiPurchaseHistoryPage, meta: { requiresAuth: true, role: 'candidate' } },
  { path: '/notifications', component: NotificationsPage, meta: { requiresAuth: true } },
  { path: '/recruiter/company', component: CompanyManagementPage, meta: { requiresAuth: true, role: 'recruiter' } },
  { path: '/recruiter/jobs', component: JobManagementPage, meta: { requiresAuth: true, role: 'recruiter' } },
  { path: '/recruiter/applications', component: ApplicationManagementPage, meta: { requiresAuth: true, role: 'recruiter' } },
  { path: '/admin/users', component: UserManagementPage, meta: { requiresAuth: true, role: 'admin' } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isLoggedIn) return '/login'
  if (auth.isLoggedIn && auth.role === 'none' && to.path !== '/select-role') return '/select-role'
  if (to.path === '/select-role' && auth.role !== 'none') return '/'
  if (to.meta.role && to.meta.role !== auth.role) return '/'
  return true
})

export default router
