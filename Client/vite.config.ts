import { fileURLToPath, URL } from 'node:url'

import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
// import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
    vue(),
    // vueDevTools(),
    tailwindcss(),
  ],
  server: {
    proxy: {
      '/api': {
        target: loadEnv(mode, process.cwd()).VITE_API_URL || 'http://localhost:5174',
        changeOrigin: true,
        secure: mode !== 'test',
      },
    },
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
}))
