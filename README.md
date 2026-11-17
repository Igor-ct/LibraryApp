# LibraryApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам.

Проєкти: `Core` (домен, сервіси, звіти), `Infrastructure` (EF Core + SQLite), `Api` (Minimal API), `Cli` (консольний застосунок), тести `Core.Tests`, `Infrastructure.Tests`, `Api.Tests`.

## Запуск

```bash
dotnet build
dotnet test

# консольний застосунок
dotnet run --project src/Cli                              # звіт за замовчуванням (report-top)
dotnet run --project src/Cli -- import [файл.json]        # асинхронний імпорт
dotnet run --project src/Cli -- export [файл.json] --file # експорт колекції в JSON

# HTTP API
dotnet run --project src/Api --urls http://localhost:5080
```

Прапорець `--file` у Cli вмикає файлове сховище. Без нього Cli працює з даними в пам'яті.

## Структура проекту

```
LibraryApp/
├── LibraryApp.sln
├── README.md
├── data/
│   ├── sample.csv
│   ├── sample.json
│   ├── import_valid.json
│   ├── import_invalid.json
│   └── library_catalog.json
├── src/
│   ├── Core/
│   │   ├── Core.csproj
│   │   ├── EnvironmentInfo.cs
│   │   ├── EnvironmentReport.cs
│   │   ├── SyncProgress.cs      # Синхронна реалізація IProgress<T>
│   │   ├── Abstractions/        # Інтерфейси (ILibraryStore, IBookImporter, INotificationSink)
│   │   ├── Configuration/       # AppOptions, StorageOptions, LibraryOptions
│   │   ├── Domain/              # Сутності з поведінкою (BookCopy, Loan, Reader, LoanStatus)
│   │   ├── Dto/                 # Формати даних для імпорту/експорту
│   │   ├── Events/              # Подія Changed та її аргументи
│   │   ├── Exceptions/          # DomainException
│   │   ├── Import/              # Парсери (CSV, JSON, Mixed) та AsyncJsonBookImporter
│   │   ├── Reports/             # ReportService (LINQ-звіти)
│   │   ├── Services/            # Бізнес-логіка (LendingService)
│   │   └── Storage/             # InMemory, File, Caching, Synchronized, CatalogJson, StoreFactory
│   ├── Infrastructure/
│   │   ├── Infrastructure.csproj
│   │   ├── EfLibraryStore.cs    # Реалізація ILibraryStore на EF Core
│   │   ├── Persistence/         # BookRow, CopyRow, AppDbContext
│   │   └── Migrations/          # Міграції (комітяться в git)
│   ├── Api/
│   │   ├── Api.csproj
│   │   ├── Program.cs           # Composition Root для Api
│   │   ├── DomainExceptionHandler.cs
│   │   ├── Api.http
│   │   ├── appsettings.json
│   │   ├── Contracts/           # BookResponse, SaveBookRequest, PagedResponse
│   │   ├── Endpoints/           # BookEndpoints
│   │   ├── Validation/          # SaveBookRequestValidator, ValidationFilter
│   │   └── Properties/launchSettings.json
│   └── Cli/
│       ├── Cli.csproj
│       ├── Program.cs           # Composition Root для Cli
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Sinks/               # ConsoleSink, FileSink
└── tests/
    ├── Core.Tests/
    ├── Infrastructure.Tests/
    └── Api.Tests/
```

### Залежності проекту

```
Cli                  -> Core
Api                  -> Infrastructure -> Core
Api                  -> Core
Core.Tests           -> Core
Infrastructure.Tests -> Infrastructure, Core
Api.Tests            -> Api
```

`Core` не залежить ні від EF Core, ні від ASP.NET Core.

## Архітектура (Сервісний шар та сховища)

Абстракція: `ILibraryStore` — контракт взаємодії зі сховищем (незалежність від інфраструктури).

Реалізації сховища:

- `InMemoryLibraryStore` — тимчасове зберігання в оперативній пам'яті.
- `FileLibraryStore` — збереження стану на диск у форматі JSON (атомарний запис).
- `CachingLibraryStore` — декоратор для кешування запитів до диска.
- `SynchronizedLibraryStore` — декоратор, який робить будь-яке сховище потокобезпечним (`lock`).
- `EfLibraryStore` (`Infrastructure`) — зберігання в SQLite через EF Core.

Сервіс: `LendingService` — координує бізнес-операції предметної області, покладаючись виключно на інтерфейс `ILibraryStore`.

DI:

- Cli: ручне впровадження залежностей (Manual DI) у точці входу за допомогою `StoreFactory`.
- Api: `AddDbContext<AppDbContext>`, `AddScoped<ILibraryStore, EfLibraryStore>`, `AddScoped<LendingService>`. `DbContext` не потокобезпечний, тому кожен HTTP-запит має власний екземпляр.

Помилки: `ArgumentException` / `ArgumentOutOfRangeException` для некоректних аргументів, `DomainException` для порушень бізнес-правил, технічні помилки (`IOException`, `JsonException`) не маскуються під доменні.

## Api (Minimal API)

Другий вхід у систему поряд із Cli. Обидва проєкти використовують той самий `Core`, а `Core` не залежить від ASP.NET.

### Запуск

```bash
dotnet run --project src/Api --urls http://localhost:5080
```

Порт зафіксовано в `src/Api/Properties/launchSettings.json` (5080).

| Адреса | Призначення |
|---|---|
| `http://localhost:5080/scalar/v1` | Scalar UI (лише Development) |
| `http://localhost:5080/openapi/v1.json` | OpenAPI-документ (лише Development) |
| `http://localhost:5080/health` | перевірка стану |

### Маршрути

| Метод | Шлях | Тіло | Коди |
|---|---|---|---|
| GET | `/api/books?page=&pageSize=&search=&author=` | немає | 200, 400 |
| GET | `/api/books/{id}` | немає | 200, 404 |
| POST | `/api/books` | `SaveBookRequest` | 201 (+`Location`), 400, 409 |
| PUT | `/api/books/{id}` | `SaveBookRequest` | 200, 400, 404, 409 |
| DELETE | `/api/books/{id}` | немає | 204, 404 |
| POST | `/api/books/{id}/issue` | немає | 200, 404, 409 |
| POST | `/api/books/{id}/return` | немає | 200, 404, 409 |

### Контракт

`SaveBookRequest` (вхід): `{ "isbn", "title", "author", "year" }`.
`BookResponse` (вихід): `{ "id", "isbn", "title", "author", "year", "isIssued" }`.
Список повертає `{ "items": [...], "total": N }`, де `total` це кількість після фільтрації.

Параметри списку: `page` (від 1, за замовчуванням 1), `pageSize` (1-100, за замовчуванням 20), `search` (назва, автор або ISBN), `author`.

### Помилки

| Ситуація | Код | Тіло |
|---|---|---|
| Невалідні поля, кривий JSON, порожнє тіло | 400 | `ValidationProblemDetails`: `errors` — словник «поле → повідомлення» |
| Книгу не знайдено | 404 | `ProblemDetails` |
| Порушення бізнес-правила (повторна видача, повернення невиданого, ISBN уже зареєстровано для іншого видання, порушення обмеження БД) | 409 | `ProblemDetails`, `detail` містить пояснення |
| Неочікувана помилка | 500 | `ProblemDetails` без стектрейсу |

Валідацію виконує FluentValidation (`SaveBookRequestValidator`) через endpoint filter `ValidationFilter<T>`. `DomainException` перетворюється на 409 обробником `DomainExceptionHandler`.

### Приклади curl

```bash
# створити (201 + Location)
curl -i -X POST http://localhost:5080/api/books -H "Content-Type: application/json" \
  -d '{"isbn":"978-966-03-0001-1","title":"Кобзар","author":"Тарас Шевченко","year":1840}'

# прочитати, оновити, видалити
curl -i http://localhost:5080/api/books/<id>
curl -i -X PUT http://localhost:5080/api/books/<id> -H "Content-Type: application/json" \
  -d '{"isbn":"978-966-03-0001-1","title":"Кобзар 2","author":"Тарас Шевченко","year":1860}'
curl -i -X DELETE http://localhost:5080/api/books/<id>

# пагінація й пошук
curl -i "http://localhost:5080/api/books?page=1&pageSize=2"
curl -i "http://localhost:5080/api/books?search=Кобзар"

# помилка валідації (400)
curl -i -X POST http://localhost:5080/api/books -H "Content-Type: application/json" \
  -d '{"isbn":"","title":"X","year":3000}'

# повторна видача (409)
curl -i -X POST http://localhost:5080/api/books/<id>/issue
curl -i -X POST http://localhost:5080/api/books/<id>/issue
```

У PowerShell використовуйте `curl.exe`. Тіло краще передавати з файлу: `--data-binary "@body.json"`.

Готові запити для середовища розробки лежать у `src/Api/Api.http`.

## База даних (EF Core + SQLite)

Api зберігає дані у SQLite (один файл, сервер не потрібен). `Core` не залежить від EF Core: залежності `Api → Infrastructure → Core`.

| Ключ | За замовчуванням | Призначення |
|---|---|---|
| `ConnectionStrings:AppDb` | `Data Source=data/app.db` | рядок підключення; відносний шлях рахується від каталогу проєкту Api |
| `CROSSAPP_ConnectionStrings__AppDb` | не задано | перевизначення змінною середовища |

Файл за замовчуванням: `src/Api/data/app.db`. Api не засіває початкові дані: база спочатку порожня, записи додаються через `POST`.

### Схема

`Books` 1:N `Copies` (каскадне видалення).

| Таблиця | Поле | Тип | Обмеження |
|---|---|---|---|
| `Books` | `Isbn` | TEXT(20) | PK (унікальний) |
| | `Title` | TEXT(200) | NOT NULL |
| | `Author` | TEXT(100) | NULL |
| | `Year` | INTEGER | NOT NULL |
| `Copies` | `Id` | TEXT(32) | PK |
| | `BookIsbn` | TEXT(20) | NOT NULL, FK → `Books.Isbn`, `ON DELETE CASCADE` |
| | `IsIssued` | INTEGER (bool) | NOT NULL |

Особливості моделі:

- Дані видання (назва, автор, рік) зберігаються один раз на ISBN. Один ISBN це одне видання.
- `POST` примірника з наявним ISBN і тими самими даними видання додає ще один примірник. З іншими даними дає 409.
- `PUT` змінює дані видання для всіх примірників цього ISBN. Якщо в `PUT` змінити ISBN, примірник переходить до іншого видання, а видання без примірників видаляється.
- Id примірників порівнюються з урахуванням регістру.

### Міграції

```bash
dotnet tool install --global dotnet-ef --version "10.*"

dotnet ef migrations add <Назва> --project src/Infrastructure --startup-project src/Api
dotnet ef database update          --project src/Infrastructure --startup-project src/Api
dotnet ef migrations list          --project src/Infrastructure --startup-project src/Api
dotnet ef migrations script        --project src/Infrastructure --startup-project src/Api
```

У Development міграції застосовуються автоматично під час старту Api, у Production командою `database update`.

### Скинути базу

```bash
dotnet ef database drop -f --project src/Infrastructure --startup-project src/Api
dotnet ef database update   --project src/Infrastructure --startup-project src/Api
```

або видалити `src/Api/data/app.db*` і запустити Api знову (Development).

Файли `*.db`, `*.db-shm`, `*.db-wal` у git не додаються, міграції додаються.

## Конфігурація (Cli)

Ключі `Storage:*`, `Library:*`, `Integration:*` читає Cli. Api використовує `ConnectionStrings:AppDb` (див. розділ про базу даних).

Джерела читаються по черзі, кожне наступне перекриває попереднє:

`appsettings.json` → `appsettings.{CROSSAPP_ENVIRONMENT}.json` → user-secrets → змінні середовища `CROSSAPP_*`

| Ключ | Змінна середовища | За замовчуванням | Призначення |
|---|---|---|---|
| `Storage:DataPath` | `CROSSAPP_Storage__DataPath` | `data` | каталог даних (відносний рахується від каталогу застосунку) |
| `Storage:FileName` | `CROSSAPP_Storage__FileName` | `library_catalog.json` | файл сховища |
| `Library:LoanDays` | `CROSSAPP_Library__LoanDays` | `14` | термін видачі, днів |
| `Logging:MinimumLevel` | `CROSSAPP_Logging__MinimumLevel` | `Information` | рівень логу |
| `EnvironmentName` | `CROSSAPP_EnvironmentName` | `Production` | інформаційна назва середовища |
| `Integration:ApiKey` | `CROSSAPP_Integration__ApiKey` | не задано | секрет: лише user-secrets або env |
| не ключ конфігурації | `CROSSAPP_DATA_PATH` | не задано | проста змінна, перекриває `Storage:DataPath` |
| не ключ конфігурації | `CROSSAPP_ENVIRONMENT` | `Development` | вибір профілю |

Вкладений ключ у змінній середовища записується з подвійним підкресленням замість двокрапки (`Storage:DataPath` → `Storage__DataPath`).

### Профілі

| Профіль | Вмикається | Лог | Дані |
|---|---|---|---|
| Production | `CROSSAPP_ENVIRONMENT=Production` (значення базового `appsettings.json`) | `Information` | `data/` |
| Development | за замовчуванням (`appsettings.Development.json`) | `Debug` | `data-dev/` |

### Приклади змінних

PowerShell:

```powershell
$env:CROSSAPP_DATA_PATH = "$env:TEMP\libdata"
$env:CROSSAPP_ENVIRONMENT = "Production"
$env:CROSSAPP_Integration__ApiKey = "from-env"
dotnet run --project src/Cli -- export --file
```

bash:

```bash
CROSSAPP_DATA_PATH=/tmp/libdata dotnet run --project src/Cli -- export --file
CROSSAPP_ENVIRONMENT=Production CROSSAPP_Integration__ApiKey=from-env \
  dotnet run --project src/Cli -- export --file
```

### Secrets

Значення ключа в репозиторії немає, комітиться лише `UserSecretsId` у `Cli.csproj`.

```bash
dotnet user-secrets init --project src/Cli
dotnet user-secrets set "Integration:ApiKey" "<ваш-ключ>" --project src/Cli
dotnet user-secrets list --project src/Cli
```

Файл `secrets.json` лежить поза репозиторієм:

- Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

User-secrets призначені для локальної розробки. Для розгорнутого застосунку використовуйте змінні середовища або сховище секретів. У консоль друкується лише «присутній, довжина N».

### Шляхи

Усі шляхи будуються через `Path.Combine`, базовий каталог це `AppContext.BaseDirectory`, а не поточний робочий каталог. Тому з кореня solution і з `src/Cli` застосунок працює з тим самим файлом даних. Абсолютний `Storage:DataPath` використовується як є.

## Імпорт та експорт JSON

- `import` без аргументу читає `data/sample.json` поруч зі скомпільованим застосунком. З аргументом (`import data/import_valid.json`) відносний шлях рахується від поточного каталогу.
- `export` записує всю колекцію у `export.json` у каталозі даних або у вказаний файл.
- Запис атомарний: дані пишуться у `*.tmp`, потім `File.Move(..., overwrite: true)`.
- JSON у camelCase, кирилиця не екранується (`UnsafeRelaxedJsonEscaping`).
- Пошкоджений файл дає зрозуміле повідомлення без стектрейсу.

Приклади вхідних даних: `data/import_valid.json` (успішний імпорт), `data/import_invalid.json` (запис 3 із порожнім ISBN: «Дані відхилено: Запис 3: ...»).

## Перевірка скасування

```bash
dotnet run --project src/Cli -- import
```

Під час імпорту натисніть **Ctrl+C**: «Імпорт скасовано користувачем (Ctrl+C).». Для таймауту тимчасово змініть `FromSeconds(30)` на `FromSeconds(2)` у `case "import"`.

## Аналітичні звіти (LINQ to Objects)

У проєкті реалізовано генерацію аналітичних звітів з використанням технології LINQ to Objects. Усі запити ізольовані в `Core` (клас `ReportService`) і повертають спеціалізовані DTO (`record`), щоб не розкривати внутрішню структуру доменної моделі.

### Підтримувані підкоманди Cli

- `report-top` — Топ-5 найновіших книг (стабільне сортування `OrderByDescending` + `ThenBy`). Використовує власний метод-розширення `TopBy`.
- `report-old` — Фільтрація книг до заданого року (`Where`).
- `report-groups` — Групування за авторами (`GroupBy` / `from ... group ... into`).
- `report-sum` — Агрегатні функції (`Count`, `Average`, `Max`).
- `report-join` — Зведення колекцій книг та видач (`Join`).
- `demo-deferred` — Демонстрація відкладеного виконання.
- `report-perf` — Тест продуктивності (вплив проміжного `ToList()`).
- `import` — Асинхронний потоковий імпорт нових записів із JSON-файлу з відображенням прогресу та підтримкою скасування (Ctrl+C або таймаут).
- `export` — Експорт усієї колекції у JSON.

### Відкладене виконання (Deferred Execution)

LINQ-запити (наприклад, `Where` чи `Select`) не виконуються в момент їх оголошення. Вони формують лише набір інструкцій. Виконання починається тільки під час перебирання результату (наприклад, викликом `Count()`, `ToList()` або у циклі `foreach`).
У сценарії `demo-deferred` створено запит на пошук сучасних книг. Перший виклик `query.Count()` повернув **10**. Після цього до вихідної колекції було додано нову книгу. Оскільки запит ще не був матеріалізований (через `ToList()`), другий виклик `query.Count()` знову проітерував колекцію і повернув уже **11**. Тільки виклик `ToList()` зафіксував дані в пам'яті як статичний знімок (snapshot).

### Синтаксис запитів (Query Syntax) vs Методний синтаксис (Method Syntax)

У звіті `report-groups` (`ByAuthor`) використано синтаксис запитів (`from ... let ... group ... into g ... select`). Для складних операцій групування він значно читабельніший за методний ланцюжок (`.GroupBy().Select()`), оскільки нагадує SQL, дозволяє легко вводити проміжні змінні (`let`) і природніше працює з ключами та колекціями підгруп. Натомість для простих фільтрів (`Where`) методний синтаксис залишається більш компактним.

### LINQ to Entities vs LINQ to Objects

У `EfLibraryStore` запити до `DbSet` (фільтри, сортування, `Any`) перетворюються на SQL. Метод `Find(Func<BookCopy, bool>)` приймає делегат, який не можна перекласти на SQL, тому спершу читається список (`List()`), а фільтр виконується в пам'яті.

## Тести

Запуск усіх тестів:

```bash
dotnet test
```

Запуск окремого проєкту:

```bash
dotnet test tests/Core.Tests
dotnet test tests/Infrastructure.Tests
dotnet test tests/Api.Tests
```

Покриття коду:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Список тестів без запуску:

```bash
dotnet test --list-tests
```

Тестові проєкти не посилаються на `Cli`. Дані `data/` тести не читають.

### Core.Tests (xUnit, посилається лише на Core)

Тести не залежать від EF Core і ASP.NET. Файлову систему використовують лише тести, що працюють із тимчасовими файлами й каталогами, вони видаляються після виконання.

| Файл | Тест | Що перевіряє |
|---|---|---|
| `LendingServiceTests` | `AddBook_RaisesChangedExactlyOnce` | додавання піднімає рівно одну подію `BookAdded`, книга є в сховищі |
| | `AddBook_NotifiesAllSinks` | сповіщення доходить до підписників (sinks) |
| | `AddBook_EmptyTitle_ThrowsArgumentException_AndStoresNothing` | некоректний аргумент дає `ArgumentException`, сховище порожнє |
| | `IssueCopy_MarksCopyAndRaisesEvent` | видача ставить `IsIssued` і піднімає `CopyIssued` |
| | `IssueCopy_Twice_ThrowsDomainException_AndRaisesNoSecondEvent` | повторна видача дає `DomainException`, зайвої події немає |
| | `IssueCopy_UnknownId_ThrowsDomainException` | видача неіснуючого примірника |
| | `ReturnCopy_AfterIssue_RaisesCopyReturned` | повернення знімає `IsIssued` і піднімає `CopyReturned` |
| | `ReturnCopy_NotIssued_ThrowsDomainException` | повернення невиданого примірника |
| | `ReturnCopy_UnknownId_ThrowsDomainException` | повернення неіснуючого примірника |
| `ImportTests` | `ImportAsync_WhenAlreadyCancelled_ThrowsOperationCanceledException` | заздалегідь скасований токен |
| | `ImportAsync_WithLinkedToken_ThrowsOnTimeout` | зв'язані токени: спрацював таймаут, а не користувач |
| | `ImportAsync_AddsNewBooks_ReturnsCount` | імпорт додає нові записи і повертає їх кількість |
| | `ImportAsync_SkipsDuplicates` | ідемпотентність: повторний імпорт додає 0 |
| | `ImportAsync_ReportsProgressUpTo100_Monotonically` | прогрес не спадає і закінчується на 100% |
| | `ImportAsync_RaisesChangedForEachAddedBook` | подія для кожного доданого запису |
| | `Importer_InvalidRecord_ThrowsDomainException_WithRecordNumber` | тимчасовий файл із поганим записом: `DomainException` з номером запису та `InnerException` |
| `DomainTests` | `BorrowBook_OverLimit_ThrowsDomainException_AndKeepsState` | ліміт 5 видач, стан не змінюється |
| | `BorrowBook_InvalidLoanId_ThrowsArgumentException_AndKeepsState` | помилка аргументу не псує стан примірника і читача |
| | `ReturnBook_NotIssuedCopy_ThrowsDomainException_AndKeepsLoanOpen` | видача лишається відкритою при відмові |
| | `Loan_CloseTwice_ThrowsDomainException` | недопустимий перехід статусу |
| | `Loan_CloseBeforeIssue_ThrowsArgumentOutOfRange` | дата повернення раніше дати видачі |
| `ReportServiceTests` | `OldBooks_UsesStrictlyLessThanThreshold` (Theory, 5 наборів) | фільтр «строго менше року», граничні значення |
| | `OldBooks_EmptySource_ReturnsEmpty` | порожнє джерело |
| | `OldBooks_SortedByYear_ThenById` | порядок: рік, потім Id |
| | `OldBooks_MapsStatusAndAuthor` | статус «Видано/Доступно», автор «Невідомо» |
| | `TopNewest_ReturnsNewestFirst_AndRespectsN` | топ-N за роком |
| | `TopNewest_IsDeterministic_RegardlessOfInputOrder` | результат не залежить від порядку вхідних даних |
| | `TopNewest_NGreaterThanCount_ReturnsAll` | N більше за кількість записів |
| | `TopNewest_MapsStatusAndAuthor` | відображення полів |
| | `ByAuthor_GroupsCountsAndOldestYear_OrderedByCountDescending` | групування, кількість, найстаріший рік |
| | `ByAuthor_EmptySource_ReturnsEmpty` | порожнє джерело |
| | `Totals_EmptySource_ReturnsZeros` | нульові підсумки |
| | `Totals_ComputesAggregates` | `Count`, `Average`, `Max`, доступні |
| | `Totals_AllIssued_AvailableIsZero` | усі видані |
| | `GetActiveLendings_JoinsOnlyMatchingBooks_AndMapsFields` | `Join` лише за збігом Id |
| | `GetActiveLendings_NoMatches_ReturnsEmpty` | немає збігів |
| `StorageTests` | `ResolveFullPath_Relative_UsesBaseDir_AndCreatesDirectory` | відносний шлях рахується від базового каталогу, каталог створюється |
| | `ResolveFullPath_Absolute_IgnoresBaseDir` | абсолютний шлях не змінюється |
| | `FileStore_PersistsBetweenInstances_AndCreatesMissingDirectory` | запис переживає перезапуск, відсутній каталог створюється |
| | `FileStore_WritesReadableCyrillic_CamelCase_AndLeavesNoTmp` | кирилиця без `\u`, camelCase, `.tmp` не лишається |
| | `FileStore_CorruptedJson_ThrowsInvalidDataException` | зіпсований JSON дає `InvalidDataException` |
| | `FileStore_InvalidRecord_ThrowsInvalidDataException_WithRecordNumber` | поганий запис у файлі: номер запису в повідомленні |
| | `FileStore_UpdateAndRemove_ArePersisted` | `Update` і `Remove` зберігаються |
| | `WriteAtomic_OverwritesExistingFile_AndLeavesNoTmp` | перезапис наявного файлу |
| | `FileStore_DuplicateId_ThrowsAndKeepsFileUnchanged` | дублікат не змінює файл |
| `SynchronizedStoreTests` | `ParallelAdds_AreAllStored` | 500 паралельних `Add` дають 500 записів (потокобезпечність) |

### Infrastructure.Tests (EF Core + SQLite)

Тести працюють із SQLite in-memory (диск не використовується). Єдиний тест із файлом перевіряє міграції на порожній базі, тимчасовий файл видаляється у `finally`.

| Файл | Тест | Що перевіряє |
|---|---|---|
| `EfLibraryStoreTests` | `Add_ThenGetById_ReturnsEquivalentDomainObject` | запис і читання через інший контекст, мапінг Row → `BookCopy` |
| | `GetById_Unknown_ReturnsNull` | неіснуючий id |
| | `Add_TwoCopiesOfSameEdition_ShareOneBookRow` | два примірники одного видання дають один рядок у `Books` і два в `Copies` |
| | `Add_SameIsbnDifferentData_ThrowsDomainException_AndStoresNothingNew` | той самий ISBN з іншими даними: `DomainException` |
| | `Add_DuplicateId_ThrowsDomainException` | дублікат id примірника |
| | `Update_IssuedState_IsPersisted` | стан видачі зберігається в БД |
| | `Update_EditionData_AppliesToAllCopiesOfThatIsbn` | зміна назви торкається всіх примірників видання |
| | `Update_ToNewIsbn_MovesCopy_AndRemovesEmptyBook` | зміна ISBN переносить примірник, порожнє видання видаляється |
| | `Update_Unknown_ThrowsDomainException` | оновлення неіснуючого примірника |
| | `Remove_LastCopy_RemovesBookRow` | видалення останнього примірника видаляє видання |
| | `Remove_OneOfTwoCopies_KeepsBookRow` | видання лишається, поки є примірники |
| | `Remove_Unknown_ReturnsFalse` | видалення неіснуючого id |
| | `List_IsOrderedById` | детермінований порядок |
| | `Find_FiltersInMemory_ByDomainPredicate` | фільтр за доменним предикатом |
| | `Migrations_ApplyOnEmptyDatabase` | міграції застосовуються на порожній БД і збігаються з моделлю |

### Api.Tests (інтеграційні, `WebApplicationFactory`)

Кожен тест працює з власною тимчасовою SQLite-базою, яка видаляється після виконання.

| Файл | Тест | Що перевіряє |
|---|---|---|
| `BooksApiTests` | `Post_Returns201_WithLocation_AndGetByLocationReturns200` | створення дає 201 із `Location`, GET за ним 200 |
| | `FullCycle_Put_Delete_ThenGetReturns404` | замінити, видалити, після `DELETE` GET дає 404 |
| | `Post_InvalidBody_Returns400_WithFieldErrors` | у відповіді 400 є ключі `Isbn` і `Year` |
| | `Post_WrongFieldType_Returns400_NotServerError` | `"year":"abc"` дає 400, а не 500 |
| | `Post_EmptyBody_Returns400` | порожнє тіло дає 400 |
| | `Issue_Twice_Returns409_WithProblemDetails` | друга видача дає 409, слова `Exception` у відповіді немає |
| | `Get_UnknownId_Returns404` | неіснуючий id дає 404 |
| | `List_Paging_ReturnsPageAndTotal` | сторінка з двох елементів, `total = 5` |
| | `List_InvalidPageSize_Returns400` | `pageSize=0` дає 400 |
| | `Data_Survives_ApplicationRestart` | запис доступний після зупинки й повторного запуску застосунку |
| | `Post_SameEditionTwice_CreatesTwoCopies` | два `POST` з тим самим виданням дають два примірники |
| | `Post_SameIsbnDifferentEdition_Returns409` | той самий ISBN з іншою назвою дає 409 |
| `ValidatorTests` | `ValidRequest_Passes` | коректний запит проходить |
| | `AuthorIsOptional` | автор необов'язковий |
| | `InvalidField_ReportsThatField` (Theory, 5 наборів) | кожне невалідне поле дає помилку саме для себе |
| | `TooLongTitle_Fails` | назва понад 200 символів |
| | `MultipleErrors_AreAllReported` | кілька помилок повертаються разом |

## Інваріанти доменної моделі

- Обов'язкові ключі: Ідентифікатори та ключові рядки (ISBN) не можуть бути порожніми (`ArgumentException`).
- Коректність значень: Рік (видання/реєстрації) має бути в адекватних межах, лічильники не можуть бути від'ємними (`ArgumentOutOfRangeException`).
- Захист стану (BookCopy): Неможливо видати вже виданий примірник і повернути невиданий (`DomainException`).
- Хронологія (Loan): Дата повернення не може бути раніше дати видачі (`ArgumentOutOfRangeException`).
- Машина станів (LoanStatus): Заборонені некоректні переходи (наприклад, із Closed назад у Open) (`DomainException`).
- Ліміт видач (Reader): Читач не може взяти більше 5 книг одночасно (`DomainException`).
- Збереження (EfLibraryStore): ISBN унікальний; примірники одного ISBN мають однакові назву, автора й рік, інакше `DomainException` (409 в Api). Дублікат id примірника і порушення обмежень БД теж дають `DomainException`.

## Публікація

### Self-contained

Публікація містить застосунок, його залежності та .NET Runtime. Встановлення .NET на цільовій машині не потрібне.

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
```

### Framework-dependent

Публікація містить код застосунку та його залежності, але не містить .NET Runtime. На цільовій машині має бути встановлений сумісний .NET Runtime.

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

## Середовище

.NET SDK 10.0, Windows 11 x64 / Ubuntu 26.04 x64

## Додаткова інформація

| RID       | Режим               | Розмір publish | Потрібен встановлений .NET Runtime  |
|-----------|---------------------|----------------|-------------------------------------|
| win-x64   | Self-contained      | 76,68 MB       | Ні                                  |
| win-x64   | Framework-dependent | 0,19 MB        | Так                                 |
| linux-x64 | Self-contained      | 78,79 MB       | Ні                                  |
