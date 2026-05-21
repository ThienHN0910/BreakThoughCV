import axios from 'axios'

const api = axios.create({
  // Use VITE_API_URL when provided.
  // Otherwise:
  // - Dev: use '/api' (Vite proxy -> local backend)
  // - Prod: use '/api-backend/api' (works with existing vercel.json rewrite without needing changes)
  baseURL: import.meta.env.VITE_API_URL || (import.meta.env.PROD ? '/api-backend/api' : '/api')
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')

      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }

    return Promise.reject(error)
  }
)

export default api
