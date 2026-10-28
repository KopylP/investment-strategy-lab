# Home Assistant add-on

Один контейнер: PostgreSQL + Python-рушій стратегій + .NET API, який роздає Angular-застосунок.
Порт 8099 (`/investlab`, http або https) і Ingress-панель Home Assistant (8098, лише всередині).

## Встановлення як локального add-on

1. Зібрати артефакти (на Mac, потрібні .NET 10 SDK і Node):
   ```bash
   deploy/home-assistant/build.sh
   ```
2. Скопіювати папку `deploy/home-assistant/investlab` у спільну папку `addons` Home Assistant:
   - через add-on **Samba share**: Finder → `smb://homeassistant.local/addons` → перетягнути `investlab`;
   - або через **Terminal & SSH** (ключ у `authorized_keys`, порт 22): `rsync -az --delete deploy/home-assistant/investlab root@homeassistant.local:/local_apps/`
     (у нових версіях HA спільна папка `addons` усередині SSH-доповнення називається `/local_apps`, у старих — `/addons`).
3. Home Assistant → Налаштування → Доповнення → Магазин доповнень → ⋮ → **Перевірити оновлення** → розділ «Local add-ons» → **Investment Lab** → Встановити (образ збирається на пристрої, ~5–10 хв на Raspberry Pi).
4. Увімкнути «Показувати на бічній панелі», «Watchdog», запустити.
5. Відкрити `http://homeassistant.local:8099/investlab` або панель «Investment Lab».

## Деплой одним скриптом

Коли add-on Terminal & SSH уже налаштований (ключ у `authorized_keys`), усе робить один скрипт:

```bash
deploy/home-assistant/deploy.sh
```

Він запускає `build.sh`, копіює папку через `rsync` у `/local_apps` (або `/addons`), перечитує магазин доповнень
і через `ha` CLI встановлює add-on (перший раз) або оновлює його до версії з `investlab/config.yaml` (образ збирається
на пристрої), запускає і показує останні рядки логу. Та сама версія, що вже стоїть, — add-on перебудовується.

Версію піднімає сам `main`: кожен мердж, що змінює `backend`, `frontend`, `strategy-engine` чи `investlab`,
збільшує patch-версію окремим комітом GitHub Actions (`.github/workflows/addon-version.yml`); якщо мердж уже
міняє рядок `version`, він лишається як є. Тому перед деплоєм — `git pull`.

| Опція | Що робить |
|---|---|
| `--host`, `--user`, `--port` | SSH-адреса (або `HA_HOST`, `HA_USER`, `HA_PORT`; за замовчуванням `root@homeassistant.local:22`). |
| `--skip-build` | Не запускати `build.sh`, взяти вже зібрані артефакти. |
| `--bump` | Збільшити patch-версію локально (деплой не з `main`; потім закомітити `config.yaml`). |
| `--version X.Y.Z` | Поставити конкретну версію. |
| `--no-start`, `--no-logs` | Не запускати add-on / не показувати лог. |

## Оновлення вручну

`git pull` (версію вже підняв `main`) → `build.sh` → скопіювати папку знову → «Оновити» в add-on.

## Локальна перевірка без Home Assistant

```bash
cd deploy/home-assistant/investlab
docker build --build-arg BUILD_FROM=mcr.microsoft.com/dotnet/aspnet:10.0-noble-arm64v8 -t investlab-addon .
mkdir -p /tmp/investlab && echo '{"ssl":false,"certfile":"fullchain.pem","keyfile":"privkey.pem","timezone":"Europe/Warsaw"}' > /tmp/investlab/options.json
docker run --rm -v /tmp/investlab:/data -p 8099:8099 investlab-addon
```
