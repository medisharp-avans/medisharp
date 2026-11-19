# Projectafspraken voor MediSharp

Lees ook `CONTRIBUTING.md` voor de afspraken over branches en pull requests.

## Codeconventies

- Gebruik gewone classes zonder `sealed`, ook voor modellen, configuraties en tests.
- Gebruik Entity Framework Core en sterk getypeerde LINQ voor databasequeries.
  Schrijf geen SQL-queries als strings, ook niet via raw of interpolated SQL-API's.
- Gebruik geen singleton-pattern, singleton-registraties in dependency injection
  of statische gedeelde service-instanties. Kies een scoped of transient lifetime
  passend bij de verantwoordelijkheid van de service.
