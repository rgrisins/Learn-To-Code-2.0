import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const configDir = fileURLToPath(new URL('.', import.meta.url))
const certPath = path.resolve(configDir, '..', 'cert.pem')
const keyPath = path.resolve(configDir, '..', 'cert.key')

export default defineConfig({
  plugins: [vue()],
  server: {
    host: '0.0.0.0',
    https: {
      cert: fs.readFileSync(certPath),
      key: fs.readFileSync(keyPath),
    },
    allowedHosts: ['host.docker.internal'],
    hmr: {
      host: 'host.docker.internal',
      protocol: 'wss',
      port: 5173,
    },
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'https://localhost:8080',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          proxy.on('proxyReq', (proxyReq) => {
            proxyReq.setHeader('X-Forwarded-Proto', 'https')
          })
        },
      },
    },
  },
})