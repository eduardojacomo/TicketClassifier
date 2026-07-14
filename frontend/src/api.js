import axios from 'axios'

// Em produção/imagem, defina VITE_API_URL; no dev, aponta pro backend local.
const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080/api'

export default axios.create({ baseURL })
