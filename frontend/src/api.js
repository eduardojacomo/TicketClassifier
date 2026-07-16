import axios from 'axios'

// In production/image, set VITE_API_URL; in dev, points to the local backend.
const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5177/api'

export default axios.create({ baseURL })
