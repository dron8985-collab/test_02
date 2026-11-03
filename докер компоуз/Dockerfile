FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./src/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TasksApi.dll"]
