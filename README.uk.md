[English](README.md) · **Українська**

[![RailwayTickets — бронювання квитків](docs/cover.svg)](https://railwaytickets-kycile24.onrender.com/Trips)

Система бронювання залізничних квитків на ASP.NET Core MVC. Пасажир обирає рейс і місце, касир оформлює квиток, а адміністратор перевіряє операції закритої зміни.

**[Відкрити розклад](https://railwaytickets-kycile24.onrender.com/Trips)**

## Спробувати демо

Вибери **УКР** або **English** у шапці. Кнопка **Поміняти станції місцями** допоможе знайти зворотний рейс. Відкрий рейс Київ → Львів і переглянь вагони, місця та вартість. Для бронювання створи демоакаунт із вигаданими даними. Публічне демо працює на SQLite. При заміні контейнера Render відновлює початкові рейси та квитки.

| Роль | Можливості |
| --- | --- |
| Пасажир | Пошук рейсів, бронювання місця, перегляд квитків, заявка на повернення |
| Касир | Оформлення квитків, розгляд повернень, закриття касової зміни |
| Адміністратор | Керування розкладом і працівниками, затвердження касових звітів, журнал змін |

## Як захищено бронювання

Унікальний індекс дозволяє мати лише один активний квиток на конкретне місце у вагоні на рейсі. Записи про квиток та пов’язану касову операцію фіксуються в транзакції. Сервер перевіряє роль користувача перед виконанням дії.

[Модель та обмеження БД](RailwayTickets/Data/RailwayDbContext.cs) · [Транзакції](RailwayTickets/Services/AtomicWrite.cs) · [Бронювання](RailwayTickets/Controllers/TicketsController.cs)

## Локальний запуск

З кореня репозиторію виконай команди нижче в Bash із встановленим Docker. Задай власний пароль першого адміністратора. Розклад буде доступний за адресою http://localhost:8080/Trips.

```bash
docker build -t railwaytickets .
read -rs -p 'Admin password: ' Bootstrap__AdminPassword; echo
export Bootstrap__AdminPassword
docker run --rm -p 8080:10000 -e Bootstrap__AdminPassword railwaytickets
```

**Технології** · C# / .NET 10 · Razor MVC · EF Core · SQL Server / SQLite · Docker · Render

Інший проєкт · [Warehouse Management API](https://github.com/kycile24/wms-api/blob/master/README.uk.md)
