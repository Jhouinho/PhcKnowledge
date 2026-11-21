import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, ".", "PHC_");
  const hostTarget = env.PHC_KNOWLEDGE_HOST_URL || "http://localhost:5000";

  return {
    plugins: [react()],
    server: {
      host: "127.0.0.1",
      port: 5173,
      strictPort: true,
      proxy: {
        "/api": {
          target: hostTarget,
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
