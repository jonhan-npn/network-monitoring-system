# Build context: repository root (see docker-compose.yml)

# ---- Build stage ----
FROM node:24-alpine AS build
WORKDIR /app

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ .
RUN npm run build -- --configuration production

# ---- Runtime stage ----
FROM nginx:1.27-alpine AS runtime

COPY infrastructure/nginx/nginx.conf /etc/nginx/nginx.conf
COPY infrastructure/nginx/conf.d/frontend.conf /etc/nginx/conf.d/default.conf

# Angular's application builder outputs to dist/<project-name>/browser.
# The project is named "frontend" in angular.json.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80
