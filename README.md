# LibraryApp

 Наскрізний проєкт з крос-платформного програмування.
 Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
 Призначення: облік видач примірників книг читачам.

## Запуск

1. build:

```bash
 dotnet build
```

2. Запуск консольного застосунку:

```bash
 dotnet run --project src/Cli
```

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

## Структура проекту

```
LibraryApp/
├── LibraryApp.sln
├── README.md
├── data/
│   ├── sample.csv
│   └── library_catalog.json
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   ├── EnvironmentReport.cs
    │   ├── Abstractions/     # Інтерфейси (ILibraryStore)
    │   ├── Domain/           # Сутності з поведінкою (BookCopy, Loan, Reader, LoanStatus)
    │   ├── Dto/              # Формати даних для імпорту/експорту
    │   ├── Import/           # Парсери (CSV, JSON, Mixed)
    │   ├── Services/         # Бізнес-логіка (LendingService)
    │   └── Storage/          # Реалізації сховищ (InMemory, File, Caching, StoreFactory)
    └── Cli/
        ├── Cli.csproj
        └── Program.cs        # Composition Root
```

### Залежності проекту

CLI -> CORE

## Архітектура (Сервісний шар та сховища)

Абстракція: ILibraryStore — контракт взаємодії зі сховищем (незалежність від інфраструктури).

Реалізації сховища:

- InMemoryLibraryStore — тимчасове зберігання в оперативній пам'яті.
- FileLibraryStore — збереження стану на диск у форматі JSON.
- CachingLibraryStore — декоратор для кешування запитів до диска.

Сервіс: LendingService — координує бізнес-операції предметної області, покладаючись виключно на інтерфейс ILibraryStore.

- DI: Ручне впровадження залежностей (Manual DI) налаштовано в точці входу за допомогою StoreFactory.

## Аналітичні звіти (LINQ to Objects)

У проєкті реалізовано генерацію аналітичних звітів з використанням технології LINQ to Objects. Усі запити ізольовані в `Core` (клас `ReportService`) і повертають спеціалізовані DTO (`record`), щоб не розкривати внутрішню структуру доменної моделі.

### Підтримувані підкоманди:

- `report-top` — Топ-5 найновіших книг (стабільне сортування `OrderByDescending` + `ThenBy`). Використовує власний метод-розширення `TopBy`.
- `report-old` — Фільтрація книг до заданого року (`Where`).
- `report-groups` — Групування за авторами (`GroupBy` / `from ... group ... into`).
- `report-sum` — Агрегатні функції (`Count`, `Average`, `Max`).
- `report-join` — Зведення колекцій книг та видач (`Join`).
- `demo-deferred` — Демонстрація відкладеного виконання.
- `report-perf` — Тест продуктивності (вплив проміжного `ToList()`).
- `import` — Асинхронний потоковий імпорт нових записів із JSON-файлу з відображенням прогресу та підтримкою скасування (Ctrl+C або таймаут).

## Тести

Тестовий проєкт `tests/Core.Tests` (xUnit) посилається лише на `Core`, на `Cli` не посилається. Тести не залежать від файлової системи, крім одного, який створює тимчасовий файл і видаляє його у `finally`. Дані `data/` вони не читають.

Запуск усіх тестів:

```bash
dotnet test
```

Покриття коду:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Список тестів без запуску:

```bash
dotnet test --list-tests
```

### Наявні тести

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

### Відкладене виконання (Deferred Execution)

LINQ-запити (наприклад, `Where` чи `Select`) не виконуються в момент їх оголошення. Вони формують лише набір інструкцій. Виконання починається тільки під час перебирання результату (наприклад, викликом `Count()`, `ToList()` або у циклі `foreach`).
У сценарії `demo-deferred` створено запит на пошук сучасних книг. Перший виклик `query.Count()` повернув **10**. Після цього до вихідної колекції було додано нову книгу. Оскільки запит ще не був матеріалізований (через `ToList()`), другий виклик `query.Count()` знову проітерував колекцію і повернув уже **11**. Тільки виклик `ToList()` зафіксував дані в пам'яті як статичний знімок (snapshot).

### Синтаксис запитів (Query Syntax) vs Методний синтаксис (Method Syntax)

У звіті `report-groups` (`ByAuthor`) використано синтаксис запитів (`from ... let ... group ... into g ... select`). Для складних операцій групування він значно читабельніший за методний ланцюжок (`.GroupBy().Select()`), оскільки нагадує SQL, дозволяє легко вводити проміжні змінні (`let`) і природніше працює з ключами та колекціями підгруп. Натомість для простих фільтрів (`Where`) методний синтаксис залишається більш компактним.

## Інваріанти доменної моделі

- Обов'язкові ключі: Ідентифікатори та ключові рядки (ISBN) не можуть бути порожніми (`ArgumentException`).
- Коректність значень: Рік (видання/реєстрації) має бути в адекватних межах, лічильники не можуть бути від'ємними (`ArgumentOutOfRangeException`).
- Захист стану (BookCopy): Неможливо видати вже виданий примірник (`DomainException`).
- Хронологія (Loan): Дата повернення не може бути раніше дати видачі (`ArgumentOutOfRangeException`).
- Машина станів (LoanStatus): Заборонені некоректні переходи (наприклад, із Closed назад у Open) (`DomainException`).
- Ліміт видач (Reader): Читач не може взяти більше 5 книг одночасно (`DomainException`).

## Додаткова інформацяія

| RID       | Режим               | Розмір publish | Потрібен встановлений .NET Runtime |
|-----------|---------------------|----------------|-------------------------------------|
| win-x64   | Self-contained      | 76,68 MB       | Ні                                  |
| win-x64   | Framework-dependent | 0,19 MB        | Так                                 |
| linux-x64 | Self-contained      | 78,79 MB       | Ні                                  |
