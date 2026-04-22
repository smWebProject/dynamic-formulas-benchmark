import { Injectable, signal } from '@angular/core';
import { ReportData } from '../models/report.model';

@Injectable({ providedIn: 'root' })
export class ReportService {
  readonly data = signal<ReportData | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async loadReport(): Promise<void> {
    try {
      this.loading.set(true);
      this.error.set(null);
      const response = await fetch('assets/results.json');
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const json: ReportData = await response.json();
      this.data.set(json);
    } catch (e) {
      this.error.set(
        'לא ניתן לטעון את קובץ התוצאות. יש להריץ קודם את תוכנית ה-.NET.'
      );
    } finally {
      this.loading.set(false);
    }
  }
}
