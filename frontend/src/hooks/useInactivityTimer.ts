import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

export type InactivityPhase = 'active' | 'warning' | 'expired';

interface UseInactivityTimerOptions {
  timeoutMs: number;
  warningMs: number;
  enabled: boolean;
}

interface UseInactivityTimerResult {
  phase: InactivityPhase;
  secondsRemaining: number;
  resetTimer: () => void;
}

export function useInactivityTimer({
  timeoutMs,
  warningMs,
  enabled,
}: UseInactivityTimerOptions): UseInactivityTimerResult {
  const [phase, setPhase] = useState<InactivityPhase>('active');
  const [secondsRemaining, setSecondsRemaining] = useState(Math.ceil(timeoutMs / 1000));

  const lastActivityAtRef = useRef(performance.now());
  const rafIdRef = useRef<number | null>(null);

  const resetTimer = useCallback(() => {
    lastActivityAtRef.current = performance.now();
    setPhase('active');
    setSecondsRemaining(Math.ceil(timeoutMs / 1000));
  }, [timeoutMs]);

  const eventNames = useMemo<(keyof WindowEventMap)[]>(
    () => ['click', 'keydown', 'scroll'],
    []
  );

  useEffect(() => {
    if (!enabled) {
      if (rafIdRef.current) {
        cancelAnimationFrame(rafIdRef.current);
        rafIdRef.current = null;
      }
      setPhase('active');
      setSecondsRemaining(Math.ceil(timeoutMs / 1000));
      return;
    }

    resetTimer();

    const tick = () => {
      const now = performance.now();
      const inactiveMs = now - lastActivityAtRef.current;
      const remainingMs = Math.max(0, timeoutMs - inactiveMs);

      setSecondsRemaining(Math.ceil(remainingMs / 1000));

      if (remainingMs === 0) {
        setPhase('expired');
      } else if (inactiveMs >= warningMs) {
        setPhase('warning');
      } else {
        setPhase('active');
      }

      rafIdRef.current = requestAnimationFrame(tick);
    };

    const onActivity = () => {
      lastActivityAtRef.current = performance.now();
      setPhase('active');
    };

    eventNames.forEach((eventName) => {
      window.addEventListener(eventName, onActivity, { passive: true });
    });

    rafIdRef.current = requestAnimationFrame(tick);

    return () => {
      eventNames.forEach((eventName) => {
        window.removeEventListener(eventName, onActivity);
      });

      if (rafIdRef.current) {
        cancelAnimationFrame(rafIdRef.current);
        rafIdRef.current = null;
      }
    };
  }, [enabled, eventNames, resetTimer, timeoutMs, warningMs]);

  return {
    phase,
    secondsRemaining,
    resetTimer,
  };
}
