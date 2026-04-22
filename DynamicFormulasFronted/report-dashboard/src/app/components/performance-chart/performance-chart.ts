import {
  Component,
  input,
  effect,
  ElementRef,
  viewChild,
  AfterViewInit,
} from '@angular/core';
import { PerformanceLog } from '../../models/report.model';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-performance-chart',
  standalone: true,
  templateUrl: './performance-chart.html',
  styleUrl: './performance-chart.css',
})
export class PerformanceChart implements AfterViewInit {
  logs = input.required<PerformanceLog[]>();

  barCanvas = viewChild.required<ElementRef<HTMLCanvasElement>>('barChart');
  totalCanvas = viewChild.required<ElementRef<HTMLCanvasElement>>('totalChart');

  private barChart: Chart | null = null;
  private totalChart: Chart | null = null;
  private viewReady = false;

  constructor() {
    effect(() => {
      const data = this.logs();
      if (this.viewReady && data.length > 0) {
        this.renderBarChart(data);
        this.renderTotalChart(data);
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    const data = this.logs();
    if (data.length > 0) {
      this.renderBarChart(data);
      this.renderTotalChart(data);
    }
  }

  private readonly methodColors: Record<string, string> = {
    'DataTable.Compute': '#3b82f6',
    Roslyn: '#8b5cf6',
    SQLite: '#10b981',
  };

  /** גרף עמודות - זמן ריצה לכל נוסחה לפי שיטה */
  private renderBarChart(logs: PerformanceLog[]): void {
    this.barChart?.destroy();

    const formulas = [...new Set(logs.map((l) => l.formulaName))];
    const methods = [...new Set(logs.map((l) => l.method))];

    const datasets = methods.map((method) => ({
      label: method,
      data: formulas.map(
        (f) => logs.find((l) => l.method === method && l.formulaName === f)?.runTimeSeconds ?? 0
      ),
      backgroundColor: this.methodColors[method] ?? '#94a3b8',
      borderRadius: 4,
    }));

    this.barChart = new Chart(this.barCanvas().nativeElement, {
      type: 'bar',
      data: { labels: formulas, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          title: { display: true, text: 'זמן ריצה לכל נוסחה (שניות)', font: { size: 16 } },
          legend: { position: 'top' },
        },
        scales: {
          y: { beginAtZero: true, title: { display: true, text: 'שניות' } },
          x: { ticks: { maxRotation: 45, minRotation: 25 } },
        },
      },
    });
  }

  /** גרף עוגה - סה"כ זמן ריצה לפי שיטה */
  private renderTotalChart(logs: PerformanceLog[]): void {
    this.totalChart?.destroy();

    const methods = [...new Set(logs.map((l) => l.method))];
    const totals = methods.map((m) =>
      logs.filter((l) => l.method === m).reduce((sum, l) => sum + l.runTimeSeconds, 0)
    );

    this.totalChart = new Chart(this.totalCanvas().nativeElement, {
      type: 'doughnut',
      data: {
        labels: methods,
        datasets: [
          {
            data: totals,
            backgroundColor: methods.map((m) => this.methodColors[m] ?? '#94a3b8'),
            borderWidth: 2,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          title: { display: true, text: 'סה"כ זמן ריצה לפי שיטה', font: { size: 16 } },
          legend: { position: 'bottom' },
        },
      },
    });
  }
}
