import { Pipe, PipeTransform } from '@angular/core';
import { formatDate } from '@angular/common';

type DateInput = Date | string | number | null | undefined;

function monthYear(value: DateInput): string | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }
  return formatDate(value, 'MMM yyyy', 'en-US');
}

/**
 * Formats a group's season with month granularity, e.g. `Sep 2024 – Aug 2025`.
 * Falls back to a single end when one side is missing or both land in the same month.
 */
@Pipe({ name: 'seasonRange' })
export class SeasonRangePipe implements PipeTransform {
  transform(start: DateInput, end: DateInput): string {
    const startLabel = monthYear(start);
    const endLabel = monthYear(end);

    if (startLabel === null) {
      return endLabel ?? '';
    }
    if (endLabel === null || endLabel === startLabel) {
      return startLabel;
    }
    return `${startLabel} – ${endLabel}`;
  }
}
