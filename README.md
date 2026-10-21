# CoffeeSchedule

Сервис составления и учёта расписания для сети кофеен: планирование смен, подача пожеланий сотрудниками, обмен сменами, учёт отработанного времени и выгрузка отчётов для бухгалтерии.

Основной способ отслеживания прогресса и задач — [Issues](https://github.com/reflexum/coffee-schedule/issues) и доска [Projects](https://github.com/orgs/reflexum/projects).  
Правила командной разработки описаны в [CONTRIBUTING.md](CONTRIBUTING.md), шаблон для оформления изменений — в [.github/pull_request_template.md](.github/pull_request_template.md).

## Статус проекта

Проект находится на этапе активной разработки базового каркаса. На текущий момент реализована основа солюшена и базовый мониторинг; предметная модель, хранилище данных и бизнес-логика находятся в процессе реализации по задачам из бэклога.

## Реализация и архитектурный план

### Текущее состояние
- Базовая структура солюшена на .NET 9 (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).
- Базовый эндпоинт мониторинга `/health`.
- Конфигурация PostgreSQL 17 для локальной разработки в `docker-compose.yml`.
- CI пайплайн (сборка, запуск тестов, проверка форматирования кода) в GitHub Actions.

### В планах (согласно бэклогу)
- Доменные сущности кофеен, сотрудников и ролей (управляющий, менеджер смены, бариста, стажёр).
- Модуль расписания: создание, валидация пересечений и публикация смен.
- Подача пожеланий и ограничений по сменам от сотрудников.
- Настройка Entity Framework Core, миграций и валидации входных данных через FluentValidation.
- Механизм обмена сменами, аудит истории изменений и уведомления (версия v2).
- Табель отработанных часов, экспорт для бухгалтерии и личный кабинет с контролем медосмотров (версия v3).

## Стек технологий

- .NET 9 (C# 13)
- ASP.NET Core Web API
- PostgreSQL 17 (локально через Docker Compose)
- Entity Framework Core (запланировано)
- xUnit (тестирование)
- GitHub Actions (CI)

## Требования к окружению

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (версия 9.0.x)
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

3. Собрать решение и выполнить тесты:
   ```bash
   dotnet build
   dotnet test
   ```

4. Запустить сервис API:
   ```bash
   dotnet run --project src/CoffeeSchedule.Api
   ```

5. Проверить доступность:
   - http://localhost:5000/health — возвращает статус `Healthy`

## Проверка стиля кода

```bash
dotnet format --verify-no-changes
```

## Как участвовать

Порядок выбора задач, ветвления и прохождения код-ревью описан в [CONTRIBUTING.md](CONTRIBUTING.md).
