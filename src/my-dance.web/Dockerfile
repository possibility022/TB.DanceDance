# Build stage
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production stage
FROM nginx:stable-alpine AS production
COPY --from=build /app/dist/my-dance.web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Generates config.json from env vars before nginx starts (image runs
# /docker-entrypoint.d/*.sh on startup, then launches nginx).
COPY docker-entrypoint.sh /docker-entrypoint.d/40-write-config.sh
RUN chmod +x /docker-entrypoint.d/40-write-config.sh
EXPOSE 80
