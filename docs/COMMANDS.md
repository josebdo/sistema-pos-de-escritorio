# COMMANDS

## Build
```bash
dotnet build SistemaCelulares.sln
```

## Tests
```bash
dotnet test SistemaCelulares.sln
```

## Migraciones EF Core
```bash
# Agregar migración
dotnet ef migrations add <NombreMigracion> --project src/SistemaCelulares.Infrastructure --startup-project src/SistemaCelulares.App

# Aplicar migraciones a la base de datos
dotnet ef database update --project src/SistemaCelulares.Infrastructure --startup-project src/SistemaCelulares.App
```

## Ejecutar la aplicación WinForms
```bash
dotnet run --project src/SistemaCelulares.App
```
