#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SampleGrpcBiDirection/SampleGrpcBiDirection.csproj", "SampleGrpcBiDirection/"]
RUN dotnet restore "SampleGrpcBiDirection/SampleGrpcBiDirection.csproj"
COPY . .
WORKDIR "/src/SampleGrpcBiDirection"
RUN dotnet build "SampleGrpcBiDirection.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SampleGrpcBiDirection.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV QI_ALGO_SERVICE_TARGET="127.0.0.1:40056"
EXPOSE 40081
ENTRYPOINT ["dotnet", "SampleGrpcBiDirection.dll"]