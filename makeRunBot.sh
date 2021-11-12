dotnet restore "./ProjectB.csproj"
dotnet build "ProjectB.csproj" -c Release -o ./app/build
dotnet publish "ProjectB.csproj" -c Release -o ./app/publish
dotnet app/publish/ProjectB.dll