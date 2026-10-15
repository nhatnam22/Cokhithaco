FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CoKhiThaCo.csproj", "./"]
RUN dotnet restore "CoKhiThaCo.csproj"
COPY . .
RUN dotnet publish "CoKhiThaCo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgdiplus libc6-dev && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/Uploads
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENV TZ=Asia/Ho_Chi_Minh
EXPOSE 80
ENTRYPOINT ["dotnet", "CoKhiThaCo.dll"]
