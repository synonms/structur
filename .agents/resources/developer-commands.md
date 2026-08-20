# Developer Commands

## Build
```bash
dotnet build
dotnet build Synonms.Structur.Domain
dotnet build --configuration Release
```

## Test
```bash
dotnet test
dotnet test Tests/Synonms.Structur.Domain.Tests.Unit
dotnet test --filter "AggregateRootTests.UpdateProperty_DifferentValue_RecordsUpdatedActionAndUpdatesEntityTag"
dotnet test /p:CollectCoverageFromAttributes=true /p:CoverletOutputFormat=opencover
```

## Notes
- The solution uses .NET 9.0 and Visual Studio 2022/MSBuild.
- xUnit v3 is used for tests.
- NSubstitute is available for mocking internal dependencies.
- WireMock.Net.Testcontainers is used for external API simulation.
- There is no built-in linter configuration in the repo.
