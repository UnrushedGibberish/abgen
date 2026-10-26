import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const rootEnv = loadEnv(mode, '../../..', '')
  const frontendEnv = loadEnv(mode, process.cwd(), '')
  const apiPort = rootEnv.ABGEN_API_PORT || '8080'
  const frontendPort = frontendEnv.ABGEN_FRONTEND_PORT || rootEnv.ABGEN_FRONTEND_PORT || '5173'

  return {
    plugins: [react()],
    server: {
      host: '127.0.0.1',
      port: Number(frontendPort),
      strictPort: true,
      proxy: {
        '/api': `http://127.0.0.1:${apiPort}`,
        '/health': `http://127.0.0.1:${apiPort}`,
      },
    },
  }
})
