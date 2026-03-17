FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Build-only artifact image: static files are available at /build.
FROM scratch AS artifact
COPY --from=build /app/build /build
