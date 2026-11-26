# CoffeeSchedule

Сервис составления и учёта расписания для сети кофеен: планирование смен, подача пожеланий сотрудниками, обмен сменами, учёт отработанного времени и выгрузка отчётов для бухгалтерии.

Основной способ отслеживания прогресса и задач — [Issues](https://github.com/reflexum/coffee-schedule/issues) и доска [Projects](https://github.com/orgs/reflexum/projects).  
Правила командной разработки описаны в [CONTRIBUTING.md](CONTRIBUTING.md), шаблон для оформления изменений — в [.github/pull_request_template.md](.github/pull_request_template.md).

## Статус проекта

Реализован базовый функционал первой версии (`v1`): доменная модель сети кофеен, EF Core с миграциями PostgreSQL, JWT-аутентификация и разграничение ролей, составление и публикация расписания с проверкой пересечений, подача пожеланий и просмотр графика сотрудника с коллегами по смене. Функции версий `v2` и `v3` находятся в бэклоге.

## Реализация и архитектурный план

### Текущее состояние (v1)
- Архитектура решения на .NET 10 LTS (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).
- Доменные сущности кофеен, сотрудников, ролей (`Manager`, `ShiftLead`, `Barista`, `Trainee`), смен и пожеланий.
- EF Core с конфигурациями под PostgreSQL 17 и миграциями (`InitialCreate`, `AddShifts`, `AddShiftPreferences`, `AddEmployeePasswordHash`).
- Глобальный обработчик исключений (`RFC 7807 ProblemDetails`) и автоматическая валидация запросов через `FluentValidation`.
- JWT-аутентификация (`POST /api/v1/auth/bootstrap`, `POST /api/v1/auth/login`) и ролевой доступ.
- Управление сменами (`/api/v1/shifts`, `/api/v1/shops/{shopId}/shifts`, `/api/v1/shops/{shopId}/schedule/publish`) с проверкой пересечений по всей сети.
- Подача пожеланий по сменам (`/api/v1/preferences/my`, `/api/v1/shops/{shopId}/preferences`) и предупреждения о недоступности.
- Расписание сотрудника с коллегами по смене (`GET /api/v1/schedule/my`).
- Проверки состояния `/health` (liveness) и `/health/ready` (readiness с проверкой БД).
- Набор юнит- и интеграционных тестов (xUnit + `WebApplicationFactory` + SQLite in-memory).

### В планах (v2 и v3)
- Заявки на обмен сменами между сотрудниками (`#10`).
- Аудит истории изменений расписания (`#11`).
- Фиксация фактического времени (check-in / check-out) и опозданий (`#12`).
- Уведомления об изменениях в графике (`#13`).
- Табель отработанного времени и экспорт статистики в CSV (`#14`, `#15`).
- Контроль медосмотров и аттестаций сотрудников (`#16`), HTTP-кэширование (`#17`).

## Стек технологий

- .NET 10 LTS (C# 14)
- ASP.NET Core Web API (JWT Bearer, `IExceptionHandler`, `ProblemDetails`)
- Entity Framework Core 10 + Npgsql (PostgreSQL 17)
- FluentValidation
- xUnit + `Microsoft.AspNetCore.Mvc.Testing`
- GitHub Actions (CI)

## Требования к окружению

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (версия 10.0.x)
- Docker и Docker Compose
- Git

## Локальный запуск

1. Клонировать репозиторий:
   ```bash
   git clone https://github.com/reflexum/coffee-schedule.git
   cd coffee-schedule
   ```

2. Запустить PostgreSQL в контейнере:
   ```bash
   docker compose up -d
   ```
   По умолчанию база доступна на `127.0.0.1:5432` с реквизитами `coffeeschedule / coffeeschedule` (пароль можно переопределить через переменную окружения `POSTGRES_PASSWORD`).

3. Применить миграции к локальной базе данных (при необходимости):
   ```bash
   dotnet ef database update --project src/CoffeeSchedule.Infrastructure --startup-project src/CoffeeSchedule.Api
   ```
   Для автоматического применения миграций при запуске можно передать `Database__MigrateOnStartup=true`.

4. Собрать решение и выполнить тесты:
   ```bash
   dotnet build
   dotnet test
   ```

5. Запустить сервис API:
   ```bash
   dotnet run --project src/CoffeeSchedule.Api
   ```

6. Проверить доступность:
   - `GET http://localhost:5118/health` — статус приложения (`Healthy`)
   - `GET http://localhost:5118/health/ready` — готовность приложения и подключения к PostgreSQL
   - `POST http://localhost:5118/api/v1/auth/bootstrap` — создание первого управляющего при пустой БД (примеры запросов в `src/CoffeeSchedule.Api/CoffeeSchedule.Api.http`)

## Проверка стиля кода

```bash
dotnet format --verify-no-changes
```

## Как участвовать

Порядок выбора задач, ветвления и прохождения код-ревью описан в [CONTRIBUTING.md](CONTRIBUTING.md).
