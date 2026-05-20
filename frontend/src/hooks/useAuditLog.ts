import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export type ActionCategory =
  | ''
  | 'AUTH'
  | 'ACCOUNT'
  | 'SCHEDULING'
  | 'CLINICAL'
  | 'INTEGRATION'
  | 'SECURITY';

export interface AuditFilters {
  dateFrom: string;
  dateTo: string;
  actionCategory: ActionCategory;
  role: '' | 'Admin' | 'Staff' | 'Patient' | 'System';
  status: '' | 'SUCCESS' | 'FAILURE';
  search: string;
}

export interface AuditEvent {
  id: string;
  correlationId?: string;
  sessionId?: string;
  timestamp: string;
  actorId: string;
  actorName: string;
  actorEmail: string;
  actorRole: string;
  actionType: string;
  actionCategory: ActionCategory;
  resource: string;
  ipAddress: string;
  userAgent?: string;
  status: 'Success' | 'Failure';
  phiAccess: boolean;
  metadata?: string;
}

export interface AuditStats {
  eventsToday: number;
  uniqueUsers: number;
  phiAccessEvents: number;
  failedAttempts: number;
}

interface AuditPageResponse {
  events: AuditEvent[];
  total: number;
}

export interface UseAuditLogReturn {
  filters: AuditFilters;
  events: AuditEvent[];
  total: number;
  page: number;
  pageSize: number;
  stats: AuditStats | null;
  loading: boolean;
  exportLoading: boolean;
  error: string | null;
  toastMessage: string | null;
  updateFilter: <K extends keyof AuditFilters>(key: K, value: AuditFilters[K]) => void;
  resetFilters: () => void;
  goToPage: (p: number) => void;
  exportCSV: () => Promise<void>;
}

const PAGE_SIZE = 15;

function todayDateString(): string {
  return new Date().toISOString().slice(0, 10);
}

function buildQueryParams(filters: AuditFilters, page: number): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.dateFrom) params.set('from', filters.dateFrom);
  if (filters.dateTo) params.set('to', filters.dateTo);
  if (filters.actionCategory) params.set('category', filters.actionCategory);
  if (filters.role) params.set('role', filters.role);
  if (filters.status) params.set('status', filters.status);
  if (filters.search) params.set('q', filters.search);
  params.set('page', String(page));
  params.set('pageSize', String(PAGE_SIZE));
  return params;
}

const defaultFilters: AuditFilters = {
  dateFrom: todayDateString(),
  dateTo: todayDateString(),
  actionCategory: '',
  role: '',
  status: '',
  search: '',
};

export function useAuditLog(): UseAuditLogReturn {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<AuditFilters>(defaultFilters);
  const [page, setPage] = useState(1);
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [total, setTotal] = useState(0);
  const [stats, setStats] = useState<AuditStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleAuthError = useCallback(
    (status: number): boolean => {
      if (status === 401) {
        navigate('/login?redirect=/admin/audit-log');
        return true;
      }
      if (status === 403) {
        navigate('/');
        return true;
      }
      return false;
    },
    [navigate],
  );

  const doFetch = useCallback(
    async (f: AuditFilters, p: number) => {
      setLoading(true);
      setError(null);
      try {
        const params = buildQueryParams(f, p);
        const res = await fetch(`/api/v1/admin/audit-log?${params.toString()}`, {
          credentials: 'include',
        });
        if (handleAuthError(res.status)) return;
        if (!res.ok) {
          setError('Failed to load audit events. Please try again.');
          return;
        }
        const data = (await res.json()) as AuditPageResponse;
        setEvents(data.events ?? []);
        setTotal(data.total ?? 0);
      } catch {
        setError('Failed to load audit events. Please try again.');
      } finally {
        setLoading(false);
      }
    },
    [handleAuthError],
  );

  const fetchStats = useCallback(async () => {
    try {
      const res = await fetch('/api/v1/admin/audit-log/stats', { credentials: 'include' });
      if (res.ok) {
        setStats((await res.json()) as AuditStats);
      }
    } catch {
      // Stats are non-critical — failure is silent.
    }
  }, []);

  useEffect(() => {
    void fetchStats();
    void doFetch(defaultFilters, 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // intentionally empty — run once on mount

  const updateFilter = useCallback(
    <K extends keyof AuditFilters>(key: K, value: AuditFilters[K]) => {
      setFilters(prev => {
        const next = { ...prev, [key]: value };
        setPage(1);
        if (key === 'search') {
          if (debounceRef.current) clearTimeout(debounceRef.current);
          debounceRef.current = setTimeout(() => void doFetch(next, 1), 300);
        } else {
          void doFetch(next, 1);
        }
        return next;
      });
    },
    [doFetch],
  );

  const resetFilters = useCallback(() => {
    const fresh = { ...defaultFilters, dateFrom: todayDateString(), dateTo: todayDateString() };
    setFilters(fresh);
    setPage(1);
    void doFetch(fresh, 1);
  }, [doFetch]);

  const goToPage = useCallback(
    (p: number) => {
      setPage(p);
      void doFetch(filters, p);
    },
    [filters, doFetch],
  );

  const showToast = useCallback((msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  }, []);

  const exportCSV = useCallback(async () => {
    setExportLoading(true);
    try {
      const params = new URLSearchParams();
      if (filters.dateFrom) params.set('from', filters.dateFrom);
      if (filters.dateTo) params.set('to', filters.dateTo);
      if (filters.actionCategory) params.set('category', filters.actionCategory);
      if (filters.role) params.set('role', filters.role);
      if (filters.status) params.set('status', filters.status);
      if (filters.search) params.set('q', filters.search);

      const res = await fetch(`/api/v1/admin/audit-log/export?${params.toString()}`, {
        credentials: 'include',
      });
      if (handleAuthError(res.status)) return;
      if (!res.ok) {
        showToast('Export failed. Please try again.');
        return;
      }

      const blob = await res.blob();
      const blobUrl = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      const date = new Date().toISOString().slice(0, 10);
      const filename = `audit_log_${date}.csv`;
      anchor.href = blobUrl;
      anchor.download = filename;
      document.body.appendChild(anchor);
      anchor.click();
      document.body.removeChild(anchor);
      URL.revokeObjectURL(blobUrl);

      showToast(`✓ Audit log exported — ${filename}`);
    } catch {
      showToast('Export failed. Please try again.');
    } finally {
      setExportLoading(false);
    }
  }, [filters, handleAuthError, showToast]);

  return {
    filters,
    events,
    total,
    page,
    pageSize: PAGE_SIZE,
    stats,
    loading,
    exportLoading,
    error,
    toastMessage,
    updateFilter,
    resetFilters,
    goToPage,
    exportCSV,
  };
}
