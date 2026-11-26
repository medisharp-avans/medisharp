import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
// import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // vueDevTools(),
    tailwindcss()
  ],
  server: {
    port: 5173,
    strictPort: true,
    // Same as nginx in production: the client calls /api on its own port and Vite forwards it to the API.
    // API_URL is set by the end-to-end tests (AppServer) and the CI, which run the API on another port.
    proxy: {
      '/api': {
        target: process.env.API_URL || 'https://localhost:7004',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
