// מודלים עבור נתוני הדוח מה-JSON

export interface Formula {
  id: number;
  formula: string;
  condition: string | null;
  falseFormula: string | null;
  type: 'simple' | 'complex' | 'conditional';
}

export interface PerformanceLog {
  formulaId: number;
  formulaName: string;
  method: string;
  runTimeSeconds: number;
}

export interface MethodSummary {
  method: string;
  totalTime: number;
  avgTime: number;
  minTime: number;
  maxTime: number;
  formulaCount: number;
}

export interface SampleResult {
  dataId: number;
  formulaId: number;
  method: string;
  result: number | null;
}

export interface ReportData {
  generatedAt: string;
  dataCount: number;
  formulas: Formula[];
  performanceLogs: PerformanceLog[];
  summary: MethodSummary[];
  sampleResults: SampleResult[];
}
