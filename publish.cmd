del .\src\Projects\Ruddex\bin\Release\*.nupkg
dotnet pack .\src\Ruddex.sln -c Release
nuget push .\src\Projects\Ruddex\bin\Release\*.nupkg -Source https://api.nuget.org/v3/index.json