/**
 * AIIntakeChat.tsx — AC-001, AC-002, AC-005: message-thread AI intake UI (US_018).
 *
 * Renders the Gemini-driven conversation as alternating bot / patient message
 * bubbles. A `ManualFieldFallback` is rendered inline when Gemini fails
 * (rate-limit or schema-invalid response — edge cases).
 *
 * PHI fields are annotated with a 🔒 note and `color-surface-phi` styling (UXR-402).
 * Progress bar is rendered by the parent `IntakePage`; this component owns only
 * the chat scroll area and the input row.
 */
import { useRef, useEffect, useState, useCallback } from 'react';
import { ChatMessage } from '../../types/intake';
import styles from './AIIntakeChat.module.css';

// ─── QuickOptions (H-001) ─────────────────────────────────────────────

interface QuickOptionsProps {
  options: string[];
  onSelect: (value: string) => void;
}

function QuickOptions({ options, onSelect }: QuickOptionsProps): JSX.Element {
  return (
    <div className={styles.quickOptions} role="group" aria-label="Quick answer options">
      {options.map((opt) => (
        <button
          key={opt}
          className={styles.quickOption}
          onClick={() => onSelect(opt)}
          aria-label={`Quick answer: ${opt}`}
          type="button"
        >
          {opt}
        </button>
      ))}
    </div>
  );
}

// ─── ManualFieldFallback ───────────────────────────────────────────────────

interface ManualFieldFallbackProps {
  fieldKey: string;
  onSubmit: (value: string) => void;
}

function ManualFieldFallback({ fieldKey, onSubmit }: ManualFieldFallbackProps): JSX.Element {
  const [value, setValue] = useState('');
  const inputId = `fallback-${fieldKey}`;

  function handleSubmit(): void {
    if (!value.trim()) return;
    onSubmit(value.trim());
  }

  return (
    <div className={styles.fallbackWrap} data-uxr="UXR-402">
      <p className={styles.fallbackLabel}>⚠ Please type your answer for this field:</p>
      <label htmlFor={inputId} className="sr-only">
        Manual input for {fieldKey}
      </label>
      <input
        id={inputId}
        type="text"
        className={styles.fallbackInput}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') handleSubmit();
        }}
        placeholder="Type your answer…"
        aria-label={`Manual answer for ${fieldKey}`}
      />
      <button className={styles.btnFallbackSubmit} onClick={handleSubmit}>
        Submit
      </button>
    </div>
  );
}

// ─── AiBubble / UserBubble ─────────────────────────────────────────────────

interface AiBubbleProps {
  message: ChatMessage;
  onFallbackSubmit: (fieldKey: string, value: string) => void;
  onQuickSelect: (value: string) => void;
}

function AiBubble({ message, onFallbackSubmit, onQuickSelect }: AiBubbleProps): JSX.Element {
  return (
    <div className={`${styles.bubble} ${styles.bubbleAi}`} data-uxr="UXR-502">
      <div className={styles.bubbleHeader}>
        <span aria-hidden="true">🤖</span> AI Health Assistant · Powered by Gemini
      </div>
      {/* M-002: PHI note inside the AI bubble before the question text */}
      {message.isPhiField && (
        <p className={styles.phiNote} data-uxr="UXR-402" aria-label="Protected health information">
          <span aria-hidden="true">🔒</span> Your response is encrypted and HIPAA-protected.
        </p>
      )}
      <p>{message.text}</p>
      {message.fallbackRequired && message.fieldKey && (
        <ManualFieldFallback
          fieldKey={message.fieldKey}
          onSubmit={(value) => onFallbackSubmit(message.fieldKey!, value)}
        />
      )}
      {/* H-001: quick-select chips below the question */}
      {message.quickOptions && message.quickOptions.length > 0 && !message.fallbackRequired && (
        <QuickOptions options={message.quickOptions} onSelect={onQuickSelect} />
      )}
    </div>
  );
}

function UserBubble({ text }: { text: string }): JSX.Element {
  return (
    <div className={`${styles.bubble} ${styles.bubbleUser}`} aria-label="Your response">
      {text}
    </div>
  );
}

function TypingIndicator(): JSX.Element {
  return (
    <div
      className={styles.typingBubble}
      role="status"
      aria-label="AI is composing a response"
    >
      <div className={styles.dot} />
      <div className={styles.dot} />
      <div className={styles.dot} />
    </div>
  );
}

// ─── AIIntakeChat ─────────────────────────────────────────────────────────

interface AIIntakeChatProps {
  messages: ChatMessage[];
  isLoading: boolean;
  isPhiField: boolean;
  onSubmitAnswer: (answer: string) => void;
  onFallbackSubmit: (fieldKey: string, value: string) => void;
  disabled?: boolean;
  /** M-001: callback for the "Save & continue later" affordance. */
  onSaveLater?: () => void;
}

export function AIIntakeChat({
  messages,
  isLoading,
  isPhiField,
  onSubmitAnswer,
  onFallbackSubmit,
  disabled = false,
  onSaveLater,
}: AIIntakeChatProps): JSX.Element {
  const [draft, setDraft] = useState('');
  const scrollRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages, isLoading]);

  const handleSend = useCallback((): void => {
    const trimmed = draft.trim();
    if (!trimmed || disabled || isLoading) return;
    onSubmitAnswer(trimmed);
    setDraft('');
    textareaRef.current?.focus();
  }, [draft, disabled, isLoading, onSubmitAnswer]);

  /** H-001: quick-select chip selects the option and sends immediately. */
  const handleQuickSelect = useCallback((value: string): void => {
    if (disabled || isLoading) return;
    onSubmitAnswer(value);
    setDraft('');
    textareaRef.current?.focus();
  }, [disabled, isLoading, onSubmitAnswer]);

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  return (
    <>
      {/* Chat scroll area — N-002: role=log already implies aria-live=polite; explicit attr removed */}
      <div
        ref={scrollRef}
        className={styles.chatArea}
        aria-label="Intake conversation"
        role="log"
      >
        {messages.map((msg) =>
          msg.role === 'ai' ? (
            <AiBubble
              key={msg.id}
              message={msg}
              onFallbackSubmit={onFallbackSubmit}
              onQuickSelect={handleQuickSelect}
            />
          ) : (
            <UserBubble key={msg.id} text={msg.text} />
          ),
        )}
        {isLoading && <TypingIndicator />}
      </div>

      {/* Input row */}
      <div className={styles.inputArea} role="region" aria-label="Your response">
        <div className={styles.inputInner}>
          <div className={styles.inputRow}>
            <div className={styles.inputField}>
              {isPhiField && (
                <p
                  className={styles.phiNoteInput}
                  data-uxr="UXR-402"
                  aria-label="Protected health information field"
                >
                  <span aria-hidden="true">🔒</span> Health information — encrypted
                </p>
              )}
              <textarea
                ref={textareaRef}
                id="chat-input"
                className={`${styles.textarea}${isPhiField ? ` ${styles.phiInput}` : ''}`}
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Type your answer… (Enter to send, Shift+Enter for new line)"
                rows={2}
                aria-label="Your response to the AI health assistant"
                aria-required="true"
                disabled={disabled || isLoading}
              />
            </div>
            <button
              className={styles.btnSend}
              onClick={handleSend}
              aria-label="Send response"
              disabled={!draft.trim() || disabled || isLoading}
            >
              ➤
            </button>
          </div>
          <div className={styles.inputFooter}>
            <span className={styles.inputHint}>
              Your data is encrypted and only shared with your care team.
            </span>
            {/* M-001: explicit save & continue affordance */}
            {onSaveLater && (
              <button
                className={styles.btnSaveLater}
                onClick={onSaveLater}
                type="button"
                aria-label="Save progress and continue later"
              >
                Save &amp; continue later
              </button>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
