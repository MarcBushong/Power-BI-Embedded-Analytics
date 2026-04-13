

export class TenantTheme {
  primary: string;
  secondary: string;
  tertiary: string;se
  logoSymbol: string;
  tagline: string;
}

export class Report {
  id: string;
  name: string;
  datasetId: string;
  embedUrl: string;
  webUrl: string;
  reportType: string;
}

export class Dataset {
  id: string;
  name: string;
  createReportEmbedURL: string;
}

export class ViewModel {
  tenantName: string;
  reports: Report[];
  datasets: Dataset[];
  embedToken: string;
  embedTokenId: string;
  embedTokenExpiration: string;
  user: string;
  userCanEdit: boolean;
  userCanCreate: boolean;
  theme: TenantTheme;
}

export class ActivityLogEntry {
  CorrelationId: string;
  EmbedTokenId: string;
  LoginId: string;
  Activity: string;
  Tenant: string;
  Dataset: string;
  DatasetId: string;
  Report: string;
  ReportId: string;
  OriginalReportId: string;
  LoadDuration: number;
  RenderDuration: number;
}

export class User {
  LoginId: string;
  UserName: string;
}