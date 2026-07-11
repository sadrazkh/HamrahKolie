# ── مرحله ۱: Build فرانت‌اند (Vite/Vue) ────────────────────────────
FROM node:22-alpine AS client
WORKDIR /client
COPY src/HamrahKolie.Web/ClientApp/package*.json ./
RUN npm ci
COPY src/HamrahKolie.Web/ClientApp/ ./
RUN npm run build

# ── مرحله ۲: Build و Publish بک‌اند (.NET) ─────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# جایگزینی خروجی فرانت‌اند ساخته‌شده در مرحله قبل (vite در /wwwroot/dist می‌سازد)
COPY --from=client /wwwroot/dist ./src/HamrahKolie.Web/wwwroot/dist
RUN dotnet restore HamrahKolie.slnx
RUN dotnet publish src/HamrahKolie.Web/HamrahKolie.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── مرحله ۳: Runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# پوشه‌های داده (رسانه و لاگ) به‌صورت Volume نگه‌داری می‌شوند.
RUN mkdir -p /app/wwwroot/uploads /app/logs
ENTRYPOINT ["dotnet", "HamrahKolie.Web.dll"]
