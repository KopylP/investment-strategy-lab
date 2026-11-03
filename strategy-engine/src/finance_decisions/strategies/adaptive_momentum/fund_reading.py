from __future__ import annotations

from dataclasses import dataclass
from datetime import date

import numpy as np
import pandas as pd

from finance_decisions.messages import msg
from finance_decisions.portfolio_math import TRADING_DAYS_PER_MONTH, TRADING_DAYS_PER_YEAR

MOMENTUM_MONTHS = (3, 6, 12)
VOLATILITY_WINDOW_DAYS = 63
MIN_VOLATILITY_DAYS = 20


@dataclass(frozen=True)
class FundReading:
    key: str
    name: str
    group: str
    price: float | None
    last_date: date | None
    returns: dict[int, float | None]
    momentum: float | None
    volatility: float | None
    trend_average: float | None

    @property
    def above_trend(self) -> bool:
        return self.price is not None and self.trend_average is not None and self.price > self.trend_average

    def failures(self, trend_days: int) -> list[str]:
        if self.price is None:
            return [msg("adaptive_momentum.fund.no_data")]

        failures = []
        if self.momentum is None:
            failures.append(msg("adaptive_momentum.fund.short_history", days=_days_for_momentum()))
        elif self.momentum <= 0:
            failures.append(msg("adaptive_momentum.fund.falling", momentum=self.momentum))
        if self.trend_average is None:
            failures.append(msg("adaptive_momentum.fund.short_history", days=trend_days))
        elif not self.above_trend:
            failures.append(msg("adaptive_momentum.fund.below_trend", days=trend_days))

        return failures


def read_fund(key: str, name: str, group: str, series: pd.Series, trend_days: int) -> FundReading:
    prices = series.dropna()
    if prices.empty:
        return FundReading(key, name, group, None, None, {}, None, None, None)

    returns = {months: _trailing_return(prices, months) for months in MOMENTUM_MONTHS}

    return FundReading(
        key=key,
        name=name,
        group=group,
        price=float(prices.iloc[-1]),
        last_date=prices.index[-1].date(),
        returns=returns,
        momentum=_average_of_known(returns),
        volatility=_annual_volatility(prices.tail(VOLATILITY_WINDOW_DAYS + 1)),
        trend_average=_moving_average(prices, trend_days),
    )


def _trailing_return(prices: pd.Series, months: int) -> float | None:
    days = months * TRADING_DAYS_PER_MONTH
    if len(prices) <= days:
        return None

    return float(prices.iloc[-1] / prices.iloc[-1 - days] - 1)


def _average_of_known(returns: dict[int, float | None]) -> float | None:
    if any(value is None for value in returns.values()):
        return None

    return float(np.mean(list(returns.values())))


def _annual_volatility(prices: pd.Series) -> float | None:
    daily = prices.pct_change().dropna()
    if len(daily) < MIN_VOLATILITY_DAYS:
        return None

    return float(daily.std() * np.sqrt(TRADING_DAYS_PER_YEAR))


def _moving_average(prices: pd.Series, days: int) -> float | None:
    if len(prices) < days:
        return None

    return float(prices.tail(days).mean())


def _days_for_momentum() -> int:
    return max(MOMENTUM_MONTHS) * TRADING_DAYS_PER_MONTH + 1
