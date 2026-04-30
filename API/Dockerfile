FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LearnToCode.slnx ./
COPY API/LearnToCode.csproj API/
COPY Data/LearnToCode.Data.csproj Data/
RUN dotnet restore API/LearnToCode.csproj

COPY . .
RUN dotnet publish API/LearnToCode.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LearnToCode.dll"]
