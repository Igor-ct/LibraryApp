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
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   └── EnvironmentReport.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

### Залежності проекту 

CLI -> CORE 

## Додаткова інформацяія 
|RID	   | Режим              |	Розмір publish |	Потрібен встановлений .NET Runtime |
|win-x64   | Self-contained     |	76,68 MB       | Ні                                    |
|win-x64   | Framework-dependent|	0,19 MB	       | Так                                   |
|linux-x64 | Self-contained     |	78,79 MB	   | Ні                                    | 

