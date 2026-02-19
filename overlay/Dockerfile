FROM node:20-slim
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci --only=production
COPY src/ ./src/
COPY public/ ./public/
ENV PORT=3000
EXPOSE 3000
CMD ["node", "src/index.js"]
