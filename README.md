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

## Інваріанти доменної моделі

- Обов'язкові ключі: Ідентифікатори та ключові рядки (ISBN) не можуть бути порожніми (ArgumentException).
- Коректність значень: Рік (видання/реєстрації) має бути в адекватних межах, лічильники не можуть бути від'ємними (ArgumentOutOfRangeException).
- Захист стану (BookCopy): Неможливо видати вже виданий примірник (InvalidOperationException).
- Хронологія (Loan): Дата повернення не може бути раніше дати видачі (ArgumentOutOfRangeException).
- Машина станів (LoanStatus): Заборонені некоректні переходи (наприклад, із Closed назад у Open) (InvalidOperationException).
- Ліміт видач (Reader): Читач не може взяти більше 5 книг одночасно (InvalidOperationException).

## Додаткова інформацяія

| RID       | Режим               | Розмір publish | Потрібен встановлений .NET Runtime |
|-----------|---------------------|----------------|-------------------------------------|
| win-x64   | Self-contained      | 76,68 MB       | Ні                                  |
| win-x64   | Framework-dependent | 0,19 MB        | Так                                 |
| linux-x64 | Self-contained      | 78,79 MB       | Ні                                  |
