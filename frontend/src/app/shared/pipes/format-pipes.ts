import { Pipe, PipeTransform } from '@angular/core';
import { formatDate, formatDateTime, formatMoney, formatPercent, formatPeriod, formatQuantity, formatSignedPercent } from './format';

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(amount: number | null | undefined, currency: string, digits = 2): string {
    return formatMoney(amount, currency, digits);
  }
}

@Pipe({ name: 'quantity' })
export class QuantityPipe implements PipeTransform {
  transform(quantity: number | null | undefined): string {
    return formatQuantity(quantity);
  }
}

@Pipe({ name: 'percent' })
export class PercentPipe implements PipeTransform {
  transform(fraction: number | null | undefined, digits = 1): string {
    return formatPercent(fraction, digits);
  }
}

@Pipe({ name: 'signedPercent' })
export class SignedPercentPipe implements PipeTransform {
  transform(fraction: number | null | undefined, digits = 1): string {
    return formatSignedPercent(fraction, digits);
  }
}

@Pipe({ name: 'date' })
export class DatePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDate(value);
  }
}

@Pipe({ name: 'dateTime' })
export class DateTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDateTime(value);
  }
}

@Pipe({ name: 'period' })
export class PeriodPipe implements PipeTransform {
  transform(period: string): string {
    return formatPeriod(period);
  }
}
