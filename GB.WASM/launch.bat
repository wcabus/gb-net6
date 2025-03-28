dotnet publish -c Release
dotnet serve -h "Cross-Origin-Opener-Policy:same-origin" -h "Cross-Origin-Embedder-Policy:require-corp" --directory bin\Release\net9.0\publish\wwwroot