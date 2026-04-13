# Unity ↔ Backend: архитектура взаимодействия

Документация описывает, как Unity-клиент игры DriftBack (Telegram WebApp, WebGL) взаимодействует с REST-бекендом. Предназначена для агентов-программистов, работающих с кодовой базой.

---

## Оглавление

- [Стек и ограничения](#стек-и-ограничения)
- [Структура файлов](#структура-файлов)
- [Конфигурация Backend URL](#конфигурация-backend-url)
- [API-эндпоинты](#api-эндпоинты)
- [DTO-модели](#dto-модели)
- [Потоки данных (flows)](#потоки-данных-flows)
  - [Аутентификация (InitFlow)](#аутентификация-initflow)
  - [Обновление профиля (RefreshFlow)](#обновление-профиля-refreshflow)
  - [Загрузка гаража (LoadGarageFlow)](#загрузка-гаража-loadgarageflow)
  - [Покупка монет за Stars (BuyCoinsPackFlow)](#покупка-монет-за-stars-buycoinspackflow)
  - [Покупка машины за Race Coins (BuyCarFlow)](#покупка-машины-за-race-coins-buycarflow)
- [Telegram Bridge](#telegram-bridge)
- [Состояние приложения (AppState)](#состояние-приложения-appstate)
- [Локальный кэш (LocalProfileCache)](#локальный-кэш-localprofilecache)
- [Контекст гонки (RaceSessionContext)](#контекст-гонки-racesessioncontext)
- [Паттерн HTTP-вызовов в BackendApi](#паттерн-http-вызовов-в-backendapi)
- [Обработка ошибок](#обработка-ошибок)
- [Неиспользуемые DTO](#неиспользуемые-dto)
- [Известные проблемы и особенности](#известные-проблемы-и-особенности)
- [Правила для агентов](#правила-для-агентов)

---

## Стек и ограничения

| Аспект | Реализация |
|---|---|
| HTTP-клиент | `UnityWebRequest` (единственный) |
| Сериализация | `UnityEngine.JsonUtility` (не Newtonsoft) |
| Асинхронность | Coroutines (`IEnumerator` + `yield return`) с callback-ами (`Action<T>`) |
| Платформа | WebGL (Telegram WebApp). В Editor — моки Telegram API |
| Абстракции | Нет интерфейсов, нет базовых классов для запросов, нет retry/timeout/interceptor |

### Ограничения JsonUtility

- Не поддерживает top-level массивы — требуются wrapper-классы (см. `JsonHelper`/`JsonArrayWrapper<T>` в `LocalProfileCache.cs`)
- Не поддерживает `Dictionary`, nullable-типы, полиморфные типы
- Поля должны быть `public` и помечены `[Serializable]`

---

## Структура файлов

```
Assets/Base/Scripts/
├── Backend/                              # HTTP-клиент и серверные DTO
│   ├── BackendApi.cs                     # ★ Все HTTP-вызовы к бекенду
│   ├── TelegramAuthRequest.cs
│   ├── TelegramAuthResponse.cs
│   ├── TelegramProfile.cs
│   ├── GarageResponse.cs
│   ├── GarageCarDto.cs
│   ├── PriceDto.cs
│   ├── CarPurchaseIntentRequest.cs       # не используется в BackendApi
│   └── CarPurchaseIntentResponse.cs      # не используется в BackendApi
│
├── Core/
│   ├── AppManager.cs                     # ★ Оркестратор всех flow
│   ├── AppState.cs                       # In-memory состояние сессии
│   ├── LocalProfileCache.cs             # PlayerPrefs-кэш профиля
│   └── SelectedCarStorage.cs            # PlayerPrefs для выбранной машины
│
├── Telegram/
│   └── TelegramBridge.cs               # ★ Мост к Telegram WebApp API
│
├── Models/                              # ScriptableObject-каталоги и устаревшие DTO
│   ├── CarDefinition.cs                 # SO: определение машины для UI
│   ├── GarageCatalog.cs                 # SO: массив CarDefinition
│   ├── CurrencyPackDefinition.cs        # SO: определение пакета монет
│   ├── CurrencyPackCatalog.cs           # SO: массив CurrencyPackDefinition
│   ├── TelegramUserData.cs
│   ├── PlayerProfileData.cs             # не используется
│   ├── InitRequest.cs                   # не используется
│   ├── InitResponse.cs                  # не используется
│   ├── EchoNumberRequest.cs             # не используется
│   ├── EchoNumberResponse.cs            # не используется
│   ├── CreateInvoiceRequest.cs          # не используется
│   └── CreateInvoiceResponse.cs         # не используется
│
├── Race/
│   ├── RaceSessionContext.cs            # Статический контекст для гоночной сцены
│   ├── RaceMode.cs
│   ├── RaceFlowManager.cs
│   ├── FinishLineTrigger.cs
│   └── SubmitRaceResultRequest.cs       # не используется (отправка отключена)
│
├── UI/
│   ├── MainScreenView.cs               # Главный экран (панели, статус)
│   ├── GaragePanelView.cs              # Список машин в гараже
│   └── GarageCarItemView.cs            # Один элемент списка машин
│
├── BuyCarRequest.cs                     # DTO: запрос покупки машины
├── BuyCarResponse.cs                    # DTO: ответ покупки машины
├── CreateCoinsPurchaseIntentRequest.cs  # DTO: запрос intent-а на покупку монет
├── CoinsPurchaseIntentResponse.cs       # DTO: ответ intent-а на покупку монет
├── BuyCurrencyPanelView.cs             # UI: панель покупки монет
├── CurrencyPackItemView.cs             # UI: один элемент пакета монет
├── TournamentPanelView.cs              # UI: панель турнира
├── SceneLoader.cs                      # Загрузка сцен + подготовка RaceSessionContext
├── RaceResultSubmitter.cs              # Подписка на финиш (отправка отключена)
├── SimpleActionResponse.cs             # не используется
└── EnterTournamentRequest.cs           # не используется

Assets/Plugins/WebGL/
└── TelegramBridge.jslib                # JS-реализация Telegram WebApp bridge
```

---

## Конфигурация Backend URL

Backend URL задаётся через `[SerializeField]` поле в двух MonoBehaviour:

| Компонент | Файл | Поле |
|---|---|---|
| `AppManager` | `Core/AppManager.cs` | `backendBaseUrl` |
| `SceneLoader` | `SceneLoader.cs` | `backendBaseUrl` |

Значение по умолчанию в коде: `"https://your-backend-url.com"`.
Реальное значение задаётся в сцене `InitScene.unity`: `https://cars-racing-production.up.railway.app`.

> **Внимание:** URL дублируется в двух компонентах, и они должны совпадать. Нет единого ScriptableObject или конфига для URL.

---

## API-эндпоинты

Все вызовы реализованы в `BackendApi.cs`. Каждый метод — отдельная coroutine.

### POST `/v1/auth/telegram`

**Назначение:** Аутентификация через Telegram initData.

| Параметр | Значение |
|---|---|
| Auth header | Нет |
| Content-Type | `application/json` |
| Request body | `TelegramAuthRequest` |
| Response body | `TelegramAuthResponse` |
| Метод в BackendApi | `AuthTelegram()` |
| Вызывается из | `AppManager.InitFlow()` |

### GET `/v1/garage`

**Назначение:** Получение списка машин с ценами, статусом владения и текущего баланса.

| Параметр | Значение |
|---|---|
| Auth header | `Authorization: Bearer {accessToken}` |
| Request body | Нет |
| Response body | `GarageResponse` |
| Метод в BackendApi | `GetGarage()` |
| Вызывается из | `AppManager.LoadGarageFlow()` |

### POST `/v1/purchases/coins-intents`

**Назначение:** Создание intent-а для покупки монет за Telegram Stars. Возвращает `invoiceUrl` для Telegram-оплаты.

| Параметр | Значение |
|---|---|
| Auth header | `Authorization: Bearer {accessToken}` |
| Content-Type | `application/json` |
| Request body | `CreateCoinsPurchaseIntentRequest` |
| Response body | `CoinsPurchaseIntentResponse` |
| Метод в BackendApi | `CreateCoinsPurchaseIntent()` |
| Вызывается из | `AppManager.BuyCoinsPackFlow()` |

### POST `/v1/purchases/buy-car`

**Назначение:** Покупка машины за Race Coins (внутриигровая валюта).

| Параметр | Значение |
|---|---|
| Auth header | `Authorization: Bearer {accessToken}` |
| Content-Type | `application/json` |
| Request body | `BuyCarRequest` |
| Response body | `BuyCarResponse` |
| Метод в BackendApi | `BuyCarWithRaceCoins()` |
| Вызывается из | `AppManager.BuyCarFlow()` |

### GET `/health`

**Назначение:** Проверка доступности бекенда.

| Параметр | Значение |
|---|---|
| Auth header | Нет |
| Response | Строка (plain text) |
| Метод в BackendApi | `Health()` |
| Вызывается из | Нигде (не используется в текущем коде) |

---

## DTO-модели

### Активные (используются в BackendApi)

```
TelegramAuthRequest
├── initData: string                      # Telegram WebApp initData

TelegramAuthResponse
├── accessToken: string                   # JWT-токен для последующих запросов
├── expiresInSec: int                     # Время жизни токена (не используется клиентом)
└── profile: TelegramProfile
    ├── userId: string                    # Внутренний ID игрока (= AppState.PlayerId)
    ├── telegramUserId: string
    ├── firstName: string
    ├── username: string
    ├── ownedCarIds: string[]
    ├── garageRevision: int
    └── raceCoinsBalance: int             # Баланс Race Coins (= AppState.SoftCurrency)

GarageResponse
├── garageRevision: int
├── raceCoinsBalance: int
└── cars: GarageCarDto[]
    ├── carId: string
    ├── title: string
    ├── owned: bool
    ├── canBuy: bool
    └── price: PriceDto
        ├── currency: string              # "RC" для Race Coins
        └── amount: int

CreateCoinsPurchaseIntentRequest
└── bundleId: string                      # ID пакета монет (из CurrencyPackDefinition.productId)

CoinsPurchaseIntentResponse
├── purchaseId: string
├── status: string
├── invoiceUrl: string                    # URL для Telegram Stars invoice
├── expiresAt: string
├── coinsAmount: int
└── price: PriceDto

BuyCarRequest
└── carId: string

BuyCarResponse
├── success: bool
├── carId: string
├── raceCoinsBalance: int                 # Обновлённый баланс
└── garageRevision: int
```

### Локальные модели (не отправляются на бекенд)

```
TelegramUserData                          # Данные из Telegram.WebApp.initDataUnsafe.user
├── id: long
├── username: string
├── first_name: string
├── last_name: string
└── is_premium: bool

CarDefinition (ScriptableObject)          # Локальный каталог машин для UI
├── carId: string
├── displayName: string
├── softCurrencyPrice: int
└── icon: Sprite

CurrencyPackDefinition (ScriptableObject) # Локальный каталог пакетов монет для UI
├── productId: string                     # = bundleId для бекенда
├── displayName: string
├── softCurrencyAmount: int
├── starsPrice: int
└── icon: Sprite
```

---

## Потоки данных (flows)

Все flow реализованы как coroutines в `AppManager.cs`. Общий паттерн: создать request DTO → вызвать `BackendApi` через `StartCoroutine` → в callback обновить `AppState` → обновить UI.

### Аутентификация (InitFlow)

```
AppManager.Start()
  → CollectTelegramData()          // TelegramBridge.GetInitData() и пр.
  → InitFlow()
    → проверка: initData пустой?   // Если да → ошибка, стоп
    → BackendApi.AuthTelegram(initData)
      → POST /v1/auth/telegram
      → TelegramAuthResponse
    → ApplyAuthResponse()          // accessToken, playerId, ownedCarIds,
                                   // garageRevision, raceCoinsBalance → AppState
    → LoadGarageFlow()             // GET /v1/garage
    → SaveProfileCache()           // PlayerPrefs
```

**Ключевые моменты:**
- `initData` — единственное поле, отправляемое на `/v1/auth/telegram`
- В Editor `initData` будет пустым → flow прервётся с ошибкой
- `accessToken` из ответа сохраняется в `AppState.AccessToken` и используется во всех последующих запросах
- `profile.userId` из ответа становится `AppState.PlayerId`
- Токен не имеет механизма автоматического обновления — при истечении `RefreshFlow` просто вызовет `InitFlow` заново

### Обновление профиля (RefreshFlow)

```
RefreshFlow()
  → Есть авторизация и токен?
    → Нет  → InitFlow() (полная повторная авторизация)
    → Да   → LoadGarageFlow()
             → SaveProfileCache()
```

Вызывается из:
- Кнопка "Refresh" в UI
- `OnInvoiceClosed()` — после закрытия окна Stars-оплаты
- `BuyCarFlow` — после успешной покупки машины

### Загрузка гаража (LoadGarageFlow)

```
LoadGarageFlow()
  → BackendApi.GetGarage(accessToken)
    → GET /v1/garage
    → GarageResponse
  → ApplyGarageResponse()
    → garageRevision, raceCoinsBalance → AppState
    → перебор response.cars → owned=true → AppState.OwnedCarIds
    → EnsureSelectedCarValid()
  → RebuildPanels() + RefreshAllViews()
```

### Покупка монет за Stars (BuyCoinsPackFlow)

```
BuyCoinsPackFlow(CurrencyPackDefinition pack)
  → BackendApi.CreateCoinsPurchaseIntent(accessToken, bundleId)
    → POST /v1/purchases/coins-intents
    → CoinsPurchaseIntentResponse { invoiceUrl }
  → invoiceUrl пустой? → ошибка
  → TelegramBridge.OpenInvoice(invoiceUrl, gameObjectName, "OnInvoiceClosed")
    → Telegram WebApp открывает нативное окно оплаты Stars
    → Пользователь платит или отменяет
    → Telegram вызывает callback → Unity получает SendMessage
  → OnInvoiceClosed(status)
    → RefreshFlow() → перезагрузка баланса с бекенда
```

**Важно:** Клиент НЕ начисляет монеты локально. После оплаты бекенд обрабатывает webhook от Telegram и обновляет баланс. Клиент узнаёт о начислении через `RefreshFlow`.

### Покупка машины за Race Coins (BuyCarFlow)

```
BuyCarFlow(CarDefinition car)
  → проверки:
    → авторизован? есть токен?
    → FindGarageCar(carId) — ищем в _lastGarageResponse
    → уже owned? → SelectCar()
    → price.currency == "RC"? → если нет, ошибка
    → достаточно монет? → если нет, перенаправление на BuyCurrencyPanel
  → BackendApi.BuyCarWithRaceCoins(accessToken, carId)
    → POST /v1/purchases/buy-car
    → BuyCarResponse { success, raceCoinsBalance, garageRevision }
  → обновляем SoftCurrency и GarageRevision из ответа
  → RefreshFlow() → полная перезагрузка гаража
```

**Важно:** Перед вызовом API клиент проверяет `price.currency == "RC"`. Если бекенд вернёт другую валюту, покупка будет заблокирована с сообщением "Backend still returns non-RC car pricing".

---

## Telegram Bridge

`TelegramBridge.cs` — обёртка над JavaScript-функциями из `TelegramBridge.jslib`, доступными через `[DllImport("__Internal")]`.

### Методы

| Метод | Описание | WebGL | Editor |
|---|---|---|---|
| `IsAvailable()` | Telegram WebApp доступен? | `Telegram.WebApp` проверка | `false` |
| `GetInitData()` | Строка `initData` для авторизации | `Telegram.WebApp.initData` | `""` |
| `GetUser()` | Данные пользователя Telegram | `initDataUnsafe.user` | Mock-объект |
| `GetStartParam()` | Стартовый параметр deep link | `initDataUnsafe.start_param` | `""` |
| `GetPlatform()` | Платформа | `Telegram.WebApp.platform` | `"editor"` |
| `GetVersion()` | Версия WebApp API | `Telegram.WebApp.version` | `"editor"` |
| `ReadyAndExpand()` | `ready()` + `expand()` | Вызывает JS | Ничего |
| `OpenInvoice(url, go, cb)` | Открывает Stars-инвойс | `Telegram.WebApp.openInvoice` | Log |

### Callback инвойса

`OpenInvoice` принимает `gameObjectName` и `callbackMethodName`. После оплаты/отмены Telegram вызывает `unityInstance.SendMessage(gameObjectName, callbackMethodName, status)`. В текущем коде callback — `AppManager.OnInvoiceClosed(string status)`.

---

## Состояние приложения (AppState)

`AppState` — plain C# класс (не MonoBehaviour), хранит in-memory состояние текущей сессии.

| Поле | Тип | Источник |
|---|---|---|
| `TelegramAvailable` | `bool` | `TelegramBridge.IsAvailable()` |
| `InitData` | `string` | `TelegramBridge.GetInitData()` |
| `TelegramUser` | `TelegramUserData` | `TelegramBridge.GetUser()` |
| `StartParam` | `string` | `TelegramBridge.GetStartParam()` |
| `Platform` | `string` | `TelegramBridge.GetPlatform()` |
| `AppVersion` | `string` | `TelegramBridge.GetVersion()` |
| `IsAuthorized` | `bool` | `true` если получен accessToken и profile |
| `AccessToken` | `string` | Из `TelegramAuthResponse.accessToken` |
| `PlayerId` | `string` | Из `TelegramProfile.userId` |
| `OwnedCarIds` | `string[]` | Из auth или garage response |
| `GarageRevision` | `int` | Из auth или garage response |
| `SoftCurrency` | `int` | Из `raceCoinsBalance` (auth или garage) |
| `IsPremium` | `bool` | Локальный флаг турнирного доступа |
| `SelectedCarId` | `string` | `SelectedCarStorage` (PlayerPrefs) |
| `LastInvoiceStatus` | `string` | Из Telegram invoice callback |
| `TrainingPoints` | `int` | Из `LocalProfileCache` |
| `TournamentPoints` | `int` | Из `LocalProfileCache` |

---

## Локальный кэш (LocalProfileCache)

Сохраняет в `PlayerPrefs`:
- `playerId`
- `ownedCarIds` (через `JsonHelper.ToJson` → `JsonArrayWrapper`)
- `trainingPoints`
- `tournamentPoints`

**Не сохраняет:** `softCurrency` (всегда 0 при загрузке из кэша, актуальное значение приходит с бекенда).

Используется для:
- Быстрого отображения UI до завершения InitFlow
- Передачи `playerId` в `RaceSessionContext` через `SceneLoader`

---

## Контекст гонки (RaceSessionContext)

Статический класс, передающий данные между сценами (из меню в гоночную сцену).

| Поле | Источник |
|---|---|
| `CurrentMode` | `RaceMode.Training` или `RaceMode.Tournament` |
| `PlayerId` | `AppState.PlayerId` или `LocalProfileCache.playerId` |
| `InitData` | `TelegramBridge.GetInitData()` |
| `TelegramUserId` | `TelegramUserData.id` |
| `BackendBaseUrl` | `backendBaseUrl` из `AppManager` или `SceneLoader` |

> **Проблема:** `SceneLoader.PrepareRaceContext()` повторно создаёт `TelegramBridge` и читает `LocalProfileCache`, не используя `AppState`. Это может привести к рассинхронизации, если кэш устарел.

Заполняется двумя путями:
1. `AppManager.OnStartTrainingClicked()` / `OnStartTournamentClicked()` → использует `AppState`
2. `SceneLoader.PrepareRaceContext()` → использует `LocalProfileCache` + новый `TelegramBridge`

---

## Паттерн HTTP-вызовов в BackendApi

Каждый метод в `BackendApi` следует одному и тому же шаблону:

```
IEnumerator MethodName(
    [string accessToken,]
    [RequestDto requestData,]
    Action<ResponseDto> onSuccess,
    Action<string> onError)
{
    1. Формирование URL: _baseUrl + "/v1/..."
    2. Создание UnityWebRequest (POST с JSON body или GET)
    3. Установка заголовков:
       - "Content-Type: application/json" (для POST)
       - "Authorization: Bearer {accessToken}" (для защищённых эндпоинтов)
    4. yield return request.SendWebRequest()
    5. Проверка request.result != Success → onError(request.error + "\n" + body)
    6. Попытка JsonUtility.FromJson<T>(body) → catch → onError
    7. Проверка response == null → onError
    8. onSuccess(response)
}
```

Дублирование этого паттерна — сознательное решение (нет базового класса или generic-обёртки).

---

## Обработка ошибок

- Нет разделения HTTP 4xx/5xx — Unity считает любой 2xx `Result.Success`, остальное — `Result.ConnectionError`/`ProtocolError`
- Тело ответа (`downloadHandler.text`) прикладывается к строке ошибки для дебага
- Нет retry-логики
- Нет таймаутов (используются дефолтные Unity)
- Ошибки отображаются пользователю через `MainScreenView.ShowStatus()` и `TournamentPanelView.ShowStatus()`
- При ошибке auth в `InitFlow` — flow полностью останавливается
- При ошибке garage в `LoadGarageFlow` — UI обновляется с ошибкой, но авторизация сохраняется

---

## Неиспользуемые DTO

Следующие типы определены, но **не используются** ни в `BackendApi`, ни в активных coroutines:

| Файл | Класс | Вероятное назначение |
|---|---|---|
| `Backend/CarPurchaseIntentRequest.cs` | `CarPurchaseIntentRequest` | Заменён на `BuyCarRequest` |
| `Backend/CarPurchaseIntentResponse.cs` | `CarPurchaseIntentResponse` | Заменён на `BuyCarResponse` |
| `Models/InitRequest.cs` | `InitRequest` | Устаревший формат авторизации |
| `Models/InitResponse.cs` | `InitResponse` | Устаревший формат авторизации |
| `Models/EchoNumberRequest.cs` | `EchoNumberRequest` | Отладочный эндпоинт |
| `Models/EchoNumberResponse.cs` | `EchoNumberResponse` | Отладочный эндпоинт |
| `Models/CreateInvoiceRequest.cs` | `CreateInvoiceRequest` | Заменён на `CreateCoinsPurchaseIntentRequest` |
| `Models/CreateInvoiceResponse.cs` | `CreateInvoiceResponse` | Заменён на `CoinsPurchaseIntentResponse` |
| `Models/PlayerProfileData.cs` | `PlayerProfileData` | Не используется |
| `EnterTournamentRequest.cs` | `EnterTournamentRequest` | Ещё не реализован на бекенде |
| `SimpleActionResponse.cs` | `SimpleActionResponse` | Не используется |
| `Race/SubmitRaceResultRequest.cs` | `SubmitRaceResultRequest` | Отправка результатов отключена |

---

## Известные проблемы и особенности

1. **Дублирование `backendBaseUrl`:** URL задаётся отдельно в `AppManager` и `SceneLoader`. При изменении нужно обновлять оба.

2. **Отсутствие обновления токена:** `expiresInSec` из `TelegramAuthResponse` не используется. Если токен истёк, `RefreshFlow` вызовет полный `InitFlow`.

3. **`SceneLoader` читает данные независимо от `AppState`:** `PrepareRaceContext()` создаёт новый `TelegramBridge` и загружает `LocalProfileCache`, игнорируя актуальный `AppState`. Потенциальная рассинхронизация.

4. **`RaceResultSubmitter` отключён:** Подписывается на `RaceFlowManager.RaceFinished`, но только логирует результат. HTTP-вызов для отправки результатов не реализован.

5. **Покупка турнирного доступа — только локальная:** `OnBuyTournamentAccessClicked()` списывает монеты и ставит `IsPremium = true` локально, без отправки на бекенд.

6. **`Health` endpoint не вызывается:** Метод есть в `BackendApi`, но нигде не используется.

7. **Нет обработки конкурентных запросов:** Если пользователь быстро нажмёт несколько кнопок, параллельные coroutines могут конфликтовать при обновлении `AppState`.

---

## Правила для агентов

### При добавлении нового эндпоинта

1. Создать `[Serializable]` request/response DTO в `Assets/Base/Scripts/Backend/` (если DTO относится к серверному контракту) или в корне `Assets/Base/Scripts/` (если это уже устоявшееся место для данного flow)
2. Добавить coroutine-метод в `BackendApi.cs`, следуя существующему паттерну
3. Добавить flow-coroutine в `AppManager.cs`, вызывающую новый метод
4. Обновить `AppState` в callback
5. Вызвать `RebuildPanels()` + `RefreshAllViews()` при изменении данных UI

### При изменении серверного контракта

- Все DTO используют `[Serializable]` + публичные поля
- `JsonUtility` требует точного совпадения имён полей с JSON (camelCase)
- Нет маппинга через атрибуты — имя поля в C# = имя ключа в JSON
- Новые поля в JSON, отсутствующие в DTO, молча игнорируются
- Отсутствующие поля в JSON получают default-значения (`null`, `0`, `false`)

### При работе с Telegram API

- Все Telegram-вызовы проходят через `TelegramBridge.cs`
- В Editor все методы возвращают моки — HTTP-запросы к бекенду не пройдут (пустой `initData`)
- `OpenInvoice` использует `SendMessage` callback — имя GameObject и метода критически важны
- `jslib` файл в `Assets/Plugins/WebGL/TelegramBridge.jslib` — часть WebGL-сборки

### Стиль кода

- Не добавлять комментарии в код
- Следовать существующему стилю: coroutines + callbacks, `Debug.Log` для трассировки
- Не использовать `async/await` — проект полностью на coroutines
- Не использовать приведение к `any`/`unknown` (для TypeScript) или `object` (для C# в контексте DTO)
