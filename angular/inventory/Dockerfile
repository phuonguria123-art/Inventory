FROM node:22-alpine AS build

WORKDIR /app

# Sao chép file khai báo dependency trước để tận dụng cache của Docker.
COPY package.json package-lock.json ./
#RUN npm install: Cài dependencies khi build image.
RUN npm ci  

# Build Angular ở cấu hình production, bao gồm browser và SSR server output.
COPY . .
RUN npm run build -- --configuration production

FROM node:22-alpine AS final
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=4000
ENV NG_ALLOWED_HOSTS=localhost,127.0.0.1

# Chỉ cài dependency cần thiết khi chạy ứng dụng.
COPY package.json package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY --from=build /app/dist/inventory ./dist/inventory
# mở cổng cho docker container để có thể truy cập từ bên ngoài. cổng này phải dùng -p khi chạy docker run.
EXPOSE 4000

USER node
CMD ["node", "dist/inventory/server/server.mjs"]
