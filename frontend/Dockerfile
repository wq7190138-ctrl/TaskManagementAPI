# 阶段1：构建
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./

RUN npm install
COPY . .
# 构建时注入环境变量（注意：Vite 是在构建时读取环境变量）
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# 阶段2：运行（Nginx）
FROM nginx:alpine
# 复制构建产物到 Nginx 的静态目录
COPY --from=build /app/dist /usr/share/nginx/html
# 复制自定义 Nginx 配置（用于代理 API，解决跨域）
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]