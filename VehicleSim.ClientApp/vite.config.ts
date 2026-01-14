import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    // proxy: {
    //   // This allows us to call the backend without specifying the port. (we also replace /api/v1/movu with /api/v1/movu/v1/movu so we can call the api with just /api/v1/movu)
    //   '/api/v1/movu': {
    //     target: 'http://localhost:5244',
    //     changeOrigin: true,
    //     secure: false,
    //     rewrite: (path) => path.replace(/^\/api/v1/movu/, '/api/v1/movu/v1/movu')
    //   }
    // }
  },
});
