import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/webhook': 'http://localhost:5000',
      '/heartbeat': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
  build: {
    outDir: '../Mkat.Api/wwwroot',
    emptyOutDir: true,
  },
});
