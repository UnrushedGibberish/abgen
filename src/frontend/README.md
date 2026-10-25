# Frontend

React/Vite renderer for backend-owned project metadata. It can create projects and render the persisted project list; project lifecycle and data remain owned by the API.

From this folder, run `npm install`, then `npm run dev` to start Vite at `http://localhost:5173`. Vite proxies `/api` and `/health` calls to the API at `http://localhost:8080`. Set `VITE_API_BASE_URL` only to override that default for another API host.

Use `npm run lint` and `npm run build` to validate the frontend.
