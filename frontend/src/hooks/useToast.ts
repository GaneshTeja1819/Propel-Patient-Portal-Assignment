import { useCallback, useState } from 'react';

export interface ToastMessage {
  id: string;
  message: string;
  type: 'error' | 'warning' | 'success' | 'info';
  duration?: number;
}

// Toast state management hook
export const useToast = () => {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const addToast = useCallback(
    (message: string, type: 'error' | 'warning' | 'success' | 'info' = 'info', duration = 5000): string => {
      const id = `toast-${Date.now()}-${Math.random()}`;
      const newToast: ToastMessage = { id, message, type, duration };
      setToasts((prev) => [...prev, newToast]);
      return id;
    },
    []
  );

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return { toasts, addToast, removeToast };
};
