from __future__ import annotations

import urllib.request
import urllib.parse


def send_telegram(message: str, token: str | None, chat_id: str | None) -> None:
    if not token or not chat_id:
        return

    url = f"https://api.telegram.org/bot{token}/sendMessage"
    data = urllib.parse.urlencode({"chat_id": chat_id, "text": message}).encode()
    request = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
    )
    try:
        urllib.request.urlopen(request, timeout=20)
    except Exception as error:
        print(f"Telegram notification failed: {error}")
