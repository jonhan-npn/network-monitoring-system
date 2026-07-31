# Build context: repository root (see docker-compose.yml)

# ---- Build stage ----
FROM node:24.18.0-alpine AS build
WORKDIR /app

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ .
RUN npm run build -- --configuration production

# ---- Runtime stage ----
FROM nginx:1.27-alpine AS runtime

COPY infrastructure/nginx/nginx.conf /etc/nginx/nginx.conf
COPY infrastructure/nginx/conf.d/frontend.conf /etc/nginx/conf.d/default.conf

# Angular CLI outputs to dist/<project-name>/browser for v17+ application builder
COPY --from=build /app/dist/netmonitor-frontend/browser /usr/share/nginx/html

EXPOSE 80
