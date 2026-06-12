FROM mcr.microsoft.com/dotnet/sdk:{{DotnetVersion}} AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/{{SolutionName}}.Api/{{SolutionName}}.Api.csproj
RUN dotnet publish src/{{SolutionName}}.Api/{{SolutionName}}.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:{{DotnetVersion}} AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "{{SolutionName}}.Api.dll"]
