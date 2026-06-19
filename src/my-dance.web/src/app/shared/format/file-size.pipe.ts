import { Pipe, PipeTransform } from '@angular/core';

type SizeInput = number | null | undefined;

const UNITS = ['B', 'KB', 'MB', 'GB', 'TB'] as const;

/**
 * Formats a byte count as a human-readable size, e.g. `1.5 MB`. Centralizes how
 * transfer/recording sizes render across the app.
 */
@Pipe({ name: 'fileSize' })
export class FileSizePipe implements PipeTransform {
  transform(value: SizeInput): string {
    if (value === null || value === undefined || !Number.isFinite(value) || value <= 0) {
      return '0 B';
    }

    let size = value;
    let unitIndex = 0;
    while (size >= 1024 && unitIndex < UNITS.length - 1) {
      size /= 1024;
      unitIndex++;
    }

    // Whole numbers for bytes; one decimal for larger units.
    const formatted = unitIndex === 0 ? String(Math.round(size)) : size.toFixed(1);
    return `${formatted} ${UNITS[unitIndex]}`;
  }
}
