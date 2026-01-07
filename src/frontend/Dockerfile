# Build Stage
FROM node:25-alpine AS build

ARG REACT_APP_API_BASE_URL
ARG REACT_APP_AUTH_URL

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .

ENV REACT_APP_API_BASE_URL=$REACT_APP_API_BASE_URL
ENV REACT_APP_AUTH_URL=$REACT_APP_AUTH_URL

RUN npm run build
 
# Production Stage
FROM nginx:stable-alpine AS production
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]