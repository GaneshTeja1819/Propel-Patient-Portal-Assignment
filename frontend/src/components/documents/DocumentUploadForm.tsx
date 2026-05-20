/**
 * DocumentUploadForm.tsx — SCR-010 upload form (US_025, AC-001–AC-003).
 *
 * Validates file MIME type and extension before any upload attempt (AC-002).
 * Rejects files > 10 MB client-side with no server request (AC-003).
 * "Upload" CTA disabled until both file and document type are selected (AC-001).
 * Supports drag-and-drop (wireframe-SCR-010 drop-zone pattern).
 * PHI notice displayed per wireframe UXR-402 note.
 */
import { useState, useRef, useCallback } from 'react';

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB

const DOCUMENT_TYPES = [
  { value: 'Historical', label: 'Historical record' },
  { value: 'Post-visit', label: 'Post-visit summary' },
  { value: 'Lab', label: 'Lab results' },
  { value: 'Imaging', label: 'Imaging report' },
  { value: 'Insurance', label: 'Insurance / EOB' },
];

function isPdf(file: File): boolean {
  return (
    (file.type === 'application/pdf' || file.type === 'application/x-pdf') &&
    file.name.toLowerCase().endsWith('.pdf')
  );
}

interface DocumentUploadFormProps {
  onUpload: (file: File, documentType: string) => void;
  isUploading: boolean;
}

export function DocumentUploadForm({ onUpload, isUploading }: DocumentUploadFormProps): JSX.Element {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState('');
  const [fileError, setFileError] = useState<string | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateAndSetFile = useCallback((file: File) => {
    setFileError(null);
    if (!isPdf(file)) {
      setFileError('Only PDF files are accepted.');
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setFileError('File exceeds maximum size of 10 MB.');
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }
    setSelectedFile(file);
  }, []);

  const handleFileChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file) validateAndSetFile(file);
    },
    [validateAndSetFile],
  );

  const handleDrop = useCallback(
    (e: React.DragEvent<HTMLDivElement>) => {
      e.preventDefault();
      setIsDragOver(false);
      const file = e.dataTransfer.files[0];
      if (file) validateAndSetFile(file);
    },
    [validateAndSetFile],
  );

  const handleDragOver = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(true);
  }, []);

  const handleDragLeave = useCallback(() => setIsDragOver(false), []);

  const handleDropZoneKey = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      fileInputRef.current?.click();
    }
  }, []);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      if (!selectedFile || !documentType) return;
      onUpload(selectedFile, documentType);
    },
    [selectedFile, documentType, onUpload],
  );

  const canUpload = selectedFile !== null && documentType !== '' && !isUploading;

  return (
    <form onSubmit={handleSubmit} noValidate aria-label="Upload clinical document">
      {/* Document type selector */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)', marginBottom: 'var(--space-4)' }}>
        <label
          htmlFor="doc-type"
          style={{
            fontSize: 'var(--font-size-body-sm)',
            fontWeight: 600,
            color: 'var(--color-text-secondary)',
            textTransform: 'uppercase',
            letterSpacing: '0.05em',
          }}
        >
          Document type <span aria-hidden="true">*</span>
        </label>
        <select
          id="doc-type"
          value={documentType}
          onChange={(e) => setDocumentType(e.target.value)}
          required
          disabled={isUploading}
          aria-required="true"
          style={{
            height: '44px',
            padding: '0 var(--space-3)',
            border: '1.5px solid var(--color-border-default)',
            borderRadius: 'var(--radius-sm)',
            fontFamily: 'var(--font-family-default)',
            fontSize: 'var(--font-size-body-md)',
            color: 'var(--color-text-primary)',
            background: 'var(--color-surface-default)',
          }}
        >
          <option value="" disabled>Select type…</option>
          {DOCUMENT_TYPES.map((t) => (
            <option key={t.value} value={t.value}>{t.label}</option>
          ))}
        </select>
      </div>

      {/* Drop zone */}
      <div
        role="button"
        tabIndex={0}
        aria-label="Drop zone: drag and drop a PDF or click to browse"
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onKeyDown={handleDropZoneKey}
        onClick={() => fileInputRef.current?.click()}
        style={{
          border: `2px dashed ${isDragOver ? 'var(--color-brand-primary)' : 'var(--color-border-default)'}`,
          borderRadius: 'var(--radius-lg)',
          padding: 'var(--space-8)',
          textAlign: 'center',
          cursor: 'pointer',
          background: isDragOver ? 'var(--color-brand-primary-light)' : 'transparent',
          transition: 'border-color 0.15s, background 0.15s',
          position: 'relative',
          marginBottom: 'var(--space-3)',
        }}
      >
        <input
          ref={fileInputRef}
          type="file"
          id="file-input"
          accept=".pdf,application/pdf"
          onChange={handleFileChange}
          disabled={isUploading}
          aria-label="Select a PDF file to upload"
          style={{ position: 'absolute', inset: 0, opacity: 0, cursor: 'pointer', width: '100%', height: '100%' }}
          onClick={(e) => e.stopPropagation()}
        />
        <div style={{ fontSize: '40px', marginBottom: 'var(--space-2)' }} aria-hidden="true">📄</div>
        <div style={{ fontSize: 'var(--font-size-heading-sm)', fontWeight: 600, marginBottom: 'var(--space-1)' }}>
          {selectedFile ? selectedFile.name : 'Drop PDF here or click to browse'}
        </div>
        <div style={{ fontSize: 'var(--font-size-body-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)' }}>
          Supported format: PDF only
        </div>
        <div style={{ fontSize: 'var(--font-size-caption)', color: 'var(--color-text-disabled)' }}>
          Max file size: 10 MB
        </div>
      </div>

      {/* File validation error (AC-002, AC-003) */}
      {fileError && (
        <p
          role="alert"
          aria-live="assertive"
          style={{
            color: 'var(--color-danger-text)',
            background: 'var(--color-danger-bg)',
            border: '1px solid var(--color-danger)',
            borderRadius: 'var(--radius-sm)',
            padding: 'var(--space-2) var(--space-3)',
            fontSize: 'var(--font-size-body-sm)',
            marginBottom: 'var(--space-3)',
          }}
        >
          {fileError}
        </p>
      )}

      {/* PHI notice (wireframe phi-note) */}
      <div
        role="note"
        aria-label="PHI protection notice"
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-1)',
          background: 'var(--color-surface-phi)',
          padding: 'var(--space-2) var(--space-3)',
          borderRadius: 'var(--radius-sm)',
          fontSize: 'var(--font-size-caption)',
          color: 'var(--color-brand-primary)',
          marginBottom: 'var(--space-4)',
        }}
      >
        <span aria-hidden="true">🔒</span> Uploaded documents are encrypted at rest and only accessible to your care team.
      </div>

      {/* Upload CTA */}
      <button
        type="submit"
        disabled={!canUpload}
        aria-disabled={!canUpload}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 'var(--space-2)',
          padding: '0 var(--space-4)',
          height: '44px',
          borderRadius: 'var(--radius-sm)',
          fontFamily: 'var(--font-family-default)',
          fontSize: 'var(--font-size-body-md)',
          fontWeight: 600,
          border: '1.5px solid transparent',
          cursor: canUpload ? 'pointer' : 'not-allowed',
          background: canUpload ? 'var(--color-brand-primary)' : 'var(--color-interactive-disabled)',
          color: 'var(--color-text-on-primary)',
          transition: 'background 0.15s',
          width: '100%',
        }}
      >
        {isUploading ? 'Uploading…' : '📤 Upload document'}
      </button>
    </form>
  );
}
