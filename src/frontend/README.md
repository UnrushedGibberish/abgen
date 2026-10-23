# Frontend

React/Vite renderer for backend-owned job state. It may collect input, send API requests, and render responses; it must not own conversion workflow or progress state.

From this folder, run `npm install`, then `npm run dev` to start Vite at `http://localhost:5173`. Vite proxies `/api` and `/health` calls to the API at `http://localhost:8080`. Set `VITE_API_BASE_URL` only to override that default for another API host.

Use `npm run lint` and `npm run build` to validate the frontend.
