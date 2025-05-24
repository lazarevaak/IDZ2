# IDZ2 – Text Scanner Microservice System 🧠🗂️

Микросервисное приложение для анализа текстовых файлов: определение статистики, выявление дубликатов и генерация облака слов.

## 🧩 Состав проекта

Система построена на архитектуре **микросервисов** и включает:

- 📦 **FileStoringService** — хранение файлов, загрузка и отдача;
- 📊 **FileAnalisysService** — анализ текста, вычисление хеша, статистики, генерация облака слов;
- 🌐 **APIGateway** — маршрутизация запросов ко всем сервисам;
- 🧪 **Test** — юнит-тесты сервисов (на `xUnit`, без использования Moq).

## ⚙️ Технологии

- .NET 7  
- ASP.NET Core Web API  
- Docker, Docker Compose  
- PostgreSQL  
- Entity Framework Core  
- HTTP-клиенты (`HttpClientFactory`)  
- QuickChart API — для генерации облака слов  
- xUnit — для тестирования

## 🚀 Как запустить

> Убедитесь, что установлены Docker и .NET 7.

1. **Клонируйте репозиторий**  
   ```bash
   git clone https://github.com/lazarevaak/IDZ2.git
   cd IDZ2
   ```

2. **Запустите систему в Docker**  
   ```bash
   docker-compose up --build
   ```

3. Откройте браузер и перейдите к Swagger UI:

   - 📁 [FileStoringService](http://localhost:6010/swagger/index.html)
   - 📊 [FileAnalisysService](http://localhost:6011/swagger/index.html)
   - 🌐 [APIGateway (единая точка входа)](http://localhost:6015/swagger/index.html)

## 📌 Основные возможности

### 📥 Загрузка файлов

Через `/files/upload` можно загрузить `.txt`-файл. В ответе вернётся `id`.

### 🔎 Анализ текста

- `POST /scan/{id}` — анализ текста по `id` файла:
  - Количество абзацев, слов, символов
  - Хеш SHA256
  - Проверка на дубликаты по хешу

### ☁️ Генерация облака слов

- `GET /scan/{id}/cloud` — PNG-изображение с визуализацией слов в тексте

## 🧪 Тестирование

Проект `Test/` содержит юнит-тесты для `FilesController` и `AnalysisController`.

```bash
cd Test
dotnet test
```

Тесты не используют Moq, работают на `InMemoryDatabase`.

## 🗃️ Структура репозитория

```
IDZ2/
├── FileStoringService/
├── FileAnalisysService/
├── APIGateway/
├── Test/
└── docker-compose.yml
```
