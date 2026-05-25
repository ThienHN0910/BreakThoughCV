import { defineStore } from 'pinia'
import { useAuthStore } from './auth'

function userKeyFromAuth(auth) {
  return auth?.user?.userId || auth?.user?.email || ''
}

function storageKey(userKey) {
  return `notifications:${userKey}`
}

function seenKey(userKey) {
  return `notificationsSeenAt:${userKey}`
}

function safeParseArray(raw) {
  try {
    const parsed = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function createId() {
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export const useNotificationsStore = defineStore('notifications', {
  state: () => ({
    items: [],
    loadedFor: '',
    lastSeenAt: ''
  }),
  getters: {
    list: (state) => state.items || [],
    unreadCount: (state) => {
      const items = state.items || []
      if (!items.length) return 0

      const lastSeen = state.lastSeenAt ? Date.parse(state.lastSeenAt) : 0
      if (!lastSeen) return items.length

      let count = 0
      for (const it of items) {
        const ts = it?.createdAt ? Date.parse(it.createdAt) : 0
        if (ts && ts > lastSeen) count += 1
      }
      return count
    }
  },
  actions: {
    ensureLoaded() {
      const auth = useAuthStore()
      const key = userKeyFromAuth(auth)
      if (!key) {
        this.items = []
        this.loadedFor = ''
        this.lastSeenAt = ''
        return
      }

      const cleanupLoginNotifications = () => {
        const current = this.items || []
        const next = current.filter(it => it?.title !== 'Đăng nhập thành công')
        if (next.length !== current.length) {
          this.items = next
          this.persist()
        }
      }

      if (this.loadedFor === key) {
        cleanupLoginNotifications()
        return
      }

      this.loadedFor = key
      this.items = safeParseArray(localStorage.getItem(storageKey(key)))
      this.lastSeenAt = localStorage.getItem(seenKey(key)) || ''

      cleanupLoginNotifications()
    },
    persist() {
      if (!this.loadedFor) return
      try {
        localStorage.setItem(storageKey(this.loadedFor), JSON.stringify(this.items))
      } catch {
      }
    },
    persistSeen() {
      if (!this.loadedFor) return
      try {
        localStorage.setItem(seenKey(this.loadedFor), this.lastSeenAt || '')
      } catch {
      }
    },
    add(payload) {
      this.ensureLoaded()
      if (!this.loadedFor) return

      const title = payload?.title || 'Thông báo'
      const message = payload?.message || ''
      const type = payload?.type || 'info' // info | success | warning
      const href = payload?.href || ''

      const item = {
        id: createId(),
        type,
        title,
        message,
        href,
        createdAt: new Date().toISOString()
      }

      this.items = [item, ...(this.items || [])].slice(0, 100)
      this.persist()
    },
    clearAll() {
      this.ensureLoaded()
      if (!this.loadedFor) return
      this.items = []
      this.persist()
      this.lastSeenAt = new Date().toISOString()
      this.persistSeen()
    },
    markAllRead() {
      this.ensureLoaded()
      if (!this.loadedFor) return
      this.lastSeenAt = new Date().toISOString()
      this.persistSeen()
    }
  }
})
