# Use the Node.js image to build the Angular app
FROM node:20 AS build
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy the rest of the app files and build the app
COPY . ./
RUN npm run build --prod

# Use the Nginx image to serve the Angular app
FROM nginx:alpine
COPY --from=build /app/dist/one-inc-test-app /usr/share/nginx/html

# Expose the port Nginx runs on
EXPOSE 80
