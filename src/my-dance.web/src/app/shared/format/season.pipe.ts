import { Pipe, PipeTransform } from '@angular/core';

type DateInput = Date | string | number | null | undefined;

function year(value: DateInput): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.getFullYear();
}

/**
 * Formats a group's season as years only, e.g. `2023 – 2024`. Falls back to a
 * single year when the two ends fall in the same year or one end is missing.
 */
@Pipe({ name: 'season' })
export class SeasonPipe implements PipeTransform {
  transform(start: DateInput, end: DateInput): string {
    const startYear = year(start);
    const endYear = year(end);

    if (startYear === null) {
      return endYear === null ? '' : `${endYear}`;
    }
    if (endYear === null || endYear === startYear) {
      return `${startYear}`;
    }
    return `${startYear} – ${endYear}`;
  }
}
