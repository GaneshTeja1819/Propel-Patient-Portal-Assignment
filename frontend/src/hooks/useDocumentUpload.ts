/**
 * useDocumentUpload.ts — Hook for PDF document upload (US_025, AC-001).
 *
 * Sends multipart/form-data to POST /api/v1/documents/upload.
 * Reports upload progress via XMLHttpRequest.upload.onprogress.
 * Auth: relies on __Host-access HttpOnly cookie (credentials: 'include').
 *
 * Returns:
 *   upload(file, documentType) — initiates upload
 *   progress — 0–100 integer, null when idle
 *   status — 'idle' | 'uploading' | 'success' | 'storage-error' | 'error'
 *   errorMessage — human-readable error string when status !== 'success'
 *   documentId — UUID returned by backend on success (used by useExtractionStatus)
 */
import { useState, useCallback } from 'react';

export type UploadStatus = 'idle' | 'uploading' | 'success' | 'storage-error' | 'error';

interface UseDocumentUploadReturn {
  upload: (file: File, documentType: string) => void;
  progress: number | null;
  status: UploadStatus;
  errorMessage: string | null;
  documentId: string | null;
  reset: () => void;
}

export function useDocumentUpload(): UseDocumentUploadReturn {
  const [progress, setProgress] = useState<number | null>(null);
  const [status, setStatus] = useState<UploadStatus>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [documentId, setDocumentId] = useState<string | null>(null);

  const reset = useCallback(() => {
    setProgress(null);
    setStatus('idle');
    setErrorMessage(null);
    setDocumentId(null);
  }, []);

  const upload = useCallback((file: File, documentType: string) => {
    setStatus('uploading');
    setProgress(0);
    setErrorMessage(null);
    setDocumentId(null);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);

    const xhr = new XMLHttpRequest();

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        setProgress(Math.round((event.loaded / event.total) * 100));
      }
    };

    xhr.onload = () => {
      if (xhr.status === 201) {
        try {
          const body = JSON.parse(xhr.responseText) as { documentId?: string };
          setDocumentId(body.documentId ?? null);
        } catch {
          // documentId not critical for success banner
        }
        setProgress(100);
        setStatus('success');
      } else if (xhr.status === 409) {
        setStatus('error');
        setErrorMessage('This document appears to have been uploaded already.');
      } else if (xhr.status === 507) {
        setStatus('storage-error');
        setErrorMessage('Storage limit reached — contact support.');
      } else {
        setStatus('error');
        setErrorMessage('Upload failed. Please try again.');
      }
    };

    xhr.onerror = () => {
      setStatus('error');
      setErrorMessage('Network error — check your connection and try again.');
    };

    xhr.withCredentials = true; // send __Host-access JWT cookie
    xhr.open('POST', '/api/v1/documents/upload');
    xhr.send(formData);
  }, []);

  return { upload, progress, status, errorMessage, documentId, reset };
}
