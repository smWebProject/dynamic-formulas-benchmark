import { Component, input } from '@angular/core';
import { MethodSummary } from '../../models/report.model';

@Component({
  selector: 'app-summary-cards',
  standalone: true,
  templateUrl: './summary-cards.html',
  styleUrl: './summary-cards.css',
})
export class SummaryCards {
  summaries = input.required<MethodSummary[]>();
  dataCount = input.required<number>();
  generatedAt = input.required<string>();

  getMethodClass(method: string): string {
    if (method.includes('DataTable')) return 'datatable';
    if (method.includes('Roslyn')) return 'roslyn';
    if (method.includes('SQLite')) return 'sqlite';
    return '';
  }
}
