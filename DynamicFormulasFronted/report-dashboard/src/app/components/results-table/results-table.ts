import { Component, input, computed } from '@angular/core';
import { PerformanceLog, Formula } from '../../models/report.model';

@Component({
  selector: 'app-results-table',
  standalone: true,
  templateUrl: './results-table.html',
  styleUrl: './results-table.css',
})
export class ResultsTable {
  logs = input.required<PerformanceLog[]>();
  formulas = input.required<Formula[]>();

  /** טבלה מסודרת: שורה לכל נוסחה, עמודה לכל שיטה */
  tableRows = computed(() => {
    const formulas = this.formulas();
    const logs = this.logs();
    const methods = [...new Set(logs.map((l) => l.method))];

    return formulas.map((f) => {
      const times: Record<string, number> = {};
      for (const m of methods) {
        const log = logs.find((l) => l.formulaId === f.id && l.method === m);
        times[m] = log?.runTimeSeconds ?? -1;
      }
      // מציאת השיטה המהירה ביותר
      const validTimes = Object.entries(times).filter(([, t]) => t >= 0);
      const fastest = validTimes.length > 0
        ? validTimes.reduce((a, b) => (a[1] < b[1] ? a : b))[0]
        : '';

      return { formula: f, times, fastest };
    });
  });

  methods = computed(() => [...new Set(this.logs().map((l) => l.method))]);

  getTypeLabel(type: string): string {
    switch (type) {
      case 'simple': return 'פשוטה';
      case 'complex': return 'מורכבת';
      case 'conditional': return 'תנאי';
      default: return type;
    }
  }

  getTypeBadgeClass(type: string): string {
    return 'badge badge-' + type;
  }
}
