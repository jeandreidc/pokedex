# Kota Pokedex — Web Frontend

Angular standalone SPA for the Pokedex UI. All data comes from the backend API — never PokeAPI directly.

## Prerequisites

- Node.js 20+
- Backend API running (see root [README.md](../../README.md))

## Development

```bash
npm install
npm start          # http://localhost:4200 — proxies /api → backend
```

The dev proxy is configured in `proxy.conf.json` (default target: `http://localhost:5164`).

## Production build

```bash
npm run build -- --configuration production
```

Output: `dist/web/browser/` — served by nginx in the Docker `web` target.

## Key paths

| Path | Purpose |
|------|---------|
| `src/app/features/pokedex/` | Main browse/search UI |
| `src/app/core/services/bootstrap-api.service.ts` | `/api/ready` + `/api/bootstrap` |
| `src/app/core/services/pokemon-api.service.ts` | `/api/pokemon` search |
| `src/app/core/constants/pokemon-pagination.constants.ts` | Fixed page size (24) |
| `src/app/core/models/api.models.ts` | TypeScript DTOs |

## Initial load flow

```
GET /api/ready        → wait for API warmup
GET /api/bootstrap    → filters + pokemonTotalCount
GET /api/pokemon?page=1 → first 24 cards
```

Next page: `GET /api/pokemon?page=2` (on demand).

## Related docs

- [README.md](../../README.md) — full stack run instructions
- [ARCHITECTURE.md](../../ARCHITECTURE.md) — bootstrap, prefetch, and pagination design
