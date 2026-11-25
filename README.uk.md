<div align="center">

# RailwayTickets

### Система пошуку рейсів, бронювання квитків і роботи каси

Пошук маршруту, резервування місця, повернення квитків та інструменти для пасажирів, касирів і адміністраторів.

**ASP.NET Core 8 · C# · Entity Framework Core · SQL Server**

[English](README.md)

</div>

---

## Про проєкт

RailwayTickets — навчальна система обліку залізничних квитків. Пасажир шукає рейси й керує бронюваннями, касир оформлює продажі та розглядає повернення, адміністратор підтримує довідники й переглядає операції.

Застосунок побудований на ASP.NET Core MVC, Razor і Entity Framework Core та працює з SQL Server. Доступ до дій контролерів перевіряється за ролями.

## Можливості

- Пошук рейсів за станціями та датою відправлення.
- Перегляд деталей рейсу й бронювання місця у вагоні.
- Перегляд квитків пасажира та подання заявки на повернення.
- Продаж квитків і ведення касових змін.
- Керування потягами, вагонами, маршрутами, рейсами й обліковими записами працівників.
- Журнал касових операцій і адміністративних змін.
- Генерація демонстраційних даних лише в Development.

## Технології

| Напрям | Інструменти |
| --- | --- |
| Застосунок | ASP.NET Core MVC, .NET 8, C# |
| Дані | Entity Framework Core 8, SQL Server, міграції EF Core |
| Інтерфейс | Razor, Bootstrap, адаптивний CSS |
| Аутентифікація | Cookie authentication, ролі, хешування паролів ASP.NET |

## Запуск локально

### Потрібно

- .NET 8 SDK
- SQL Server або SQL Server LocalDB
- Visual Studio 2022 чи інший редактор із підтримкою .NET

### Кроки

1. Клонуйте репозиторій і відкрийте `RailwayTickets.sln`.
2. Задайте рядок підключення через User Secrets. Для LocalDB виконайте команди з кореня репозиторію:

   ```bash
   dotnet user-secrets init --project RailwayTickets/RailwayTickets.csproj
   dotnet user-secrets set "ConnectionStrings:RailwayConnection" "Server=(localdb)\\MSSQLLocalDB;Database=RailwayTicketsDb;Trusted_Connection=True;TrustServerCertificate=True;" --project RailwayTickets/RailwayTickets.csproj
   ```

3. Запустіть застосунок:

   ```bash
   dotnet run --project RailwayTickets/RailwayTickets.csproj
   ```

Під час запуску застосунок застосовує EF Core міграції. У середовищі Development також створюються демонстраційні записи. За потреби вкажіть рядок підключення для встановленої версії SQL Server.

Перед першим запуском задай змінну середовища `Bootstrap__AdminPassword`. Для демонстраційних записів у Development також задай окремий `Demo__Password` щонайменше з восьми символів.

## Структура

```text
RailwayTickets/
├── RailwayTickets.sln
├── RailwayTickets/
│   ├── Controllers/       MVC дії та перевірки ролей
│   ├── Data/              Контекст EF Core, міграції, Development seed
│   ├── Models/            Сутності предметної області
│   ├── Views/             Razor сторінки
│   └── wwwroot/            CSS, JavaScript і статичні ресурси
```

## Примітки

- Це навчальний проєкт, а не production-сервіс продажу залізничних квитків.
- Для розгортання потрібні керований SQL Server, надійний пароль адміністратора, HTTPS та окрема перевірка вимог до зберігання даних і роботи сервісу.

## Інші проєкти

- [Warehouse Management System](https://github.com/kycile24/wms-api) — ASP.NET Core API для обліку запасів, замовлень і складських процесів.
- [TaskFlow API](https://github.com/kycile24/TaskFlow.Api) — API на .NET 8 із сервісами та репозиторіями для керування завданнями.

## Ліцензія

Відкриту ліцензію ще не обрано. Усі права належать автору, доки до проєкту не буде додано ліцензію.

Render запускає SQLite-демо з вигаданими рейсами та пасажирами. Зміни скидаються при заміні контейнера.
