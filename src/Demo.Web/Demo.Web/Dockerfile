# 第一阶段：构建应用
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 复制项目文件 - 注意：构建上下文需要设置为src目录
COPY Demo.Web/Demo.Web/*.csproj Demo.Web/Demo.Web/
COPY Modules/Demo.CustomModule/*.csproj Modules/Demo.CustomModule/

# 恢复依赖
RUN dotnet restore Demo.Web/Demo.Web/Demo.Web.csproj

# 复制所有源代码
COPY . .

# 构建并发布应用
RUN dotnet build Demo.Web/Demo.Web/Demo.Web.csproj -c Release
RUN dotnet publish Demo.Web/Demo.Web/Demo.Web.csproj -c Release -o out

# 第二阶段：运行应用
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 复制发布的应用文件
COPY --from=build /app/out .

# 暴露端口
EXPOSE 80

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# 启动应用
ENTRYPOINT ["dotnet", "Demo.Web.dll"]