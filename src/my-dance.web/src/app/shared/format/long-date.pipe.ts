import { Pipe, PipeTransform } from '@angular/core';
import { formatDate } from '@angular/common';

type DateInput = Date | string | number | null | undefined;

/**
 * Formats a date as a long English date, e.g. `04 June 2026`. Centralizes the
 * app's date format so recording/event/joined dates render consistently.
 */
@Pipe({ name: 'longDate' })
export class LongDatePipe implements PipeTransform {
  transform(value: DateInput): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }
    return formatDate(value, 'dd MMMM yyyy', 'en-US');
  }
}
