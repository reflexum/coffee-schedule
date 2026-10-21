# CoffeeSchedule

Сервис составления и учёта расписания для сети кофеен: планирование смен, подача пожеланий сотрудниками, обмен сменами, учёт отработанного времени и выгрузка табелей для бухгалтерии.

Задачи — в [Issues](https://github.com/reflexum/coffee-schedule/issues) и на доске Projects, код — через pull request с ревью.

## Стек

.NET 9 · ASP.NET Core Web API · EF Core + PostgreSQL · xUnit · GitHub Actions

## Структура

```
src/
  CoffeeSchedule.Domain          сущности и правила предметной области (смены, роли, заведения)
  CoffeeSchedule.Application     сценарии, команды и запросы, DTO, интерфейсы
  CoffeeSchedule.Infrastructure  EF Core: AppDbContext, конфигурации, миграции
  CoffeeSchedule.Api             HTTP API, контроллеры, middleware, Program.cs
tests/
  CoffeeSchedule.Tests           xUnit, юнит- и интеграционные тесты
```

## Запуск

Нужны [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) и Docker.

1. Поднять PostgreSQL:
   ```bash
   docker compose up -d
   ```
2. Запустить:
   ```bash
   dotnet run --project src/CoffeeSchedule.Api
   ```
3. Проверить:
   - http://localhost:5000/health — `Healthy`
   - http://localhost:5000/swagger — документация API

## Миграции

```bash
dotnet tool restore
dotnet ef migrations add <Имя> --project src/CoffeeSchedule.Infrastructure --startup-project src/CoffeeSchedule.Api
```

При старте в Development приложение само применяет миграции (`Database:MigrateOnStartup`).

## Тесты и стиль

```bash
dotnet test
dotnet format
```

CI проверяет сборку, тесты и форматирование в каждом PR.

## Как участвовать

Правила работы с задачами — в [CONTRIBUTING.md](CONTRIBUTING.md).
