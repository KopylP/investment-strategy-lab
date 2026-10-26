# Home Assistant add-on

Один контейнер: PostgreSQL + Python-рушій стратегій + .NET API, який роздає Angular-застосунок.
Доступ лише через Ingress-панель Home Assistant (порт 8098 усередині, назовні жодного порту).

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
5. Відкрити панель «Investment Lab» у бічному меню.

## Оновлення

`build.sh` → скопіювати папку знову → збільшити `version` у `investlab/config.yaml` → «Перебудувати» в add-on.

## Локальна перевірка без Home Assistant

```bash
cd deploy/home-assistant/investlab
docker build --build-arg BUILD_FROM=mcr.microsoft.com/dotnet/aspnet:10.0-noble-arm64v8 -t investlab-addon .
mkdir -p /tmp/investlab && echo '{"timezone":"Europe/Warsaw"}' > /tmp/investlab/options.json
# Без Supervisor-а: пускати на 8098 будь-кого, вхід паролем; відкрити http://localhost:8098/
docker run --rm -v /tmp/investlab:/data -p 8098:8098 -e HomeAssistant__IngressOnly=false investlab-addon
```
