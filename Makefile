build:
	dotnet build
run:
	dotnet run --project application
test:
	dotnet test --filter FullyQualifiedName!~Integration
integration:
	dotnet test --filter FullyQualifiedName~integration
restore:
	dotnet restore
