# Home Assistant add-on

Один контейнер: PostgreSQL + Python-рушій стратегій + .NET API, який роздає Angular-застосунок.
Доступ лише через Ingress-панель Home Assistant (порт 8098 усередині, назовні жодного порту).

## Встановлення

Образ збирає GitHub Actions (`.github/workflows/addon-release.yml`) після кожного мерджу в `main`, що змінює
`backend`, `frontend`, `strategy-engine` чи add-on, і публікує його як `ghcr.io/kopylp/investlab:<версія>` (лише
aarch64 — Raspberry Pi). Home Assistant його тільки завантажує, нічого не збираючи на пристрої. Нову версію в
`investlab/config.yaml` workflow комітить уже після того, як образ опубліковано. Після успішного релізу в реєстрі
лишаються тільки два найновіші образи (поточний і попередній), старіші видаляються.

1. Один раз: GitHub → профіль → **Packages → investlab → Package settings → Change visibility → Public**
   (після першого запуску workflow), щоб Home Assistant міг завантажити образ без пароля.
2. Home Assistant → Налаштування → Доповнення → Магазин доповнень → ⋮ → **Репозиторії** → додати
   `https://github.com/KopylP/investment-strategy-lab`.
3. У магазині з'явиться розділ «Investment Lab add-ons» → **Investment Lab** → Встановити.
4. Увімкнути «Показувати на бічній панелі», «Watchdog», за бажанням «Автоматичне оновлення», запустити.
5. Відкрити панель «Investment Lab» у бічному меню.

## Оновлення

Нічого робити не треба: Home Assistant сам перевіряє репозиторій і показує оновлення (або встановлює його, якщо
увімкнено автоматичне оновлення). Перевірити одразу — Магазин доповнень → ⋮ → **Перевірити оновлення**. Нова версія
з'являється приблизно через 10 хвилин після мерджу, коли workflow зібрав образ.

Версію не піднімають руками: якщо образ поточної версії вже є в реєстрі, workflow бере наступну patch-версію; якщо
мердж сам поставив нову версію, вона й публікується.

## Перехід із локального add-on

Add-on із репозиторію — окремий add-on зі своїм `/data` (порожня база). Дані зі старого локального add-on переносяться
один раз через спільну папку `/share/investlab` (Samba: `smb://homeassistant.local/share`, або Terminal & SSH):

1. Оновити локальний add-on до версії з цим переносом: `deploy/home-assistant/deploy.sh` (див. нижче).
2. Створити порожній файл `/share/investlab/export` і перезапустити **локальний** add-on: він запише
   `investmentlab.dump` і `keys` у `/share/investlab` і видалить `export`. **Зупинити** локальний add-on.
3. Встановити add-on із репозиторію (див. вище), створити порожній файл `/share/investlab/import` і запустити його:
   він замінить свою базу дампом, візьме ключі й видалить `import` (у лозі — `[investlab] imported`).
4. Перевірити дані в панелі, потім видалити локальний add-on і папку `/share/investlab`.

## Деплой незакомічених змін

Щоб спробувати зміни, яких ще немає в `main`, `deploy.sh` ставить робочу копію як **локальний** add-on, що збирається
на пристрої (з копії прибирається рядок `image:`). Потрібен add-on Terminal & SSH з ключем в `authorized_keys`:

```bash
deploy/home-assistant/deploy.sh
```

Він запускає `build.sh`, копіює папку через `rsync` у `/local_apps` (або `/addons`), перечитує магазин доповнень
і через `ha` CLI встановлює add-on (перший раз) або оновлює його до версії з `investlab/config.yaml` (образ збирається
на пристрої, ~5–10 хв), запускає і показує останні рядки логу. Та сама версія, що вже стоїть, — add-on перебудовується.

| Опція | Що робить |
|---|---|
| `--host`, `--user`, `--port` | SSH-адреса (або `HA_HOST`, `HA_USER`, `HA_PORT`; за замовчуванням `root@homeassistant.local:22`). |
| `--skip-build` | Не запускати `build.sh`, взяти вже зібрані артефакти. |
| `--bump` | Збільшити patch-версію локально (деплой не з `main`; потім закомітити `config.yaml`). |
| `--version X.Y.Z` | Поставити конкретну версію. |
| `--no-start`, `--no-logs` | Не запускати add-on / не показувати лог. |

## Локальна перевірка без Home Assistant

```bash
cd deploy/home-assistant/investlab
docker build --build-arg BUILD_FROM=mcr.microsoft.com/dotnet/aspnet:10.0-noble-arm64v8 -t investlab-addon .
mkdir -p /tmp/investlab && echo '{"timezone":"Europe/Warsaw"}' > /tmp/investlab/options.json
# Без Supervisor-а: пускати на 8098 будь-кого, вхід паролем; відкрити http://localhost:8098/
docker run --rm -v /tmp/investlab:/data -p 8098:8098 -e HomeAssistant__IngressOnly=false investlab-addon
```
