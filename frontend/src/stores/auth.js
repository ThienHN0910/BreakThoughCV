import { defineStore } from 'pinia'
import api from '../services/api'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user') || 'null'),
    token: localStorage.getItem('token') || '',
    isNewUser: false,
    loading: false
  }),
  getters: {
    isLoggedIn: (state) => Boolean(state.token),
    role: (state) => state.user?.role || 'none'
  },
  actions: {
    setSession(payload) {
      this.token = payload.token || ''
      this.user = {
        userId: payload.userId,
        email: payload.email,
        name: payload.name,
        avatarUrl: payload.avatarUrl,
        role: payload.role
      }
      this.isNewUser = payload.isNewUser
      localStorage.setItem('token', this.token)
      localStorage.setItem('user', JSON.stringify(this.user))
    },
    clearSession() {
      this.user = null
      this.token = ''
      this.isNewUser = false
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    },
    async loginWithGoogleIdToken(idToken) {
      this.loading = true
      try {
        const { data } = await api.post('/auth/google-login', { idToken })
        this.setSession(data)
        return data
      } finally {
        this.loading = false
      }
    },
    async updateRole(role) {
      const { data } = await api.put('/auth/update-role', { role })
      this.setSession(data)
      this.isNewUser = false
      return data
    }
  }
})
