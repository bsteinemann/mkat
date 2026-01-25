import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  base: '/mkat/',
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
      '/webhook': 'http://localhost:8080',
      '/heartbeat': 'http://localhost:8080',
      '/health': 'http://localhost:8080',
    },
  },
  build: {
    outDir: '../Mkat.Api/wwwroot',
    emptyOutDir: true,
  },
});
