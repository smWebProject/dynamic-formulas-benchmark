import { Component, OnInit, inject } from '@angular/core';
import { ReportService } from './services/report.service';
import { SummaryCards } from './components/summary-cards/summary-cards';
import { PerformanceChart } from './components/performance-chart/performance-chart';
import { ResultsTable } from './components/results-table/results-table';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [SummaryCards, PerformanceChart, ResultsTable],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly report = inject(ReportService);

  ngOnInit(): void {
    this.report.loadReport();
  }
}
