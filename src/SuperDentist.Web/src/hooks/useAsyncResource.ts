import { useCallback, useEffect, useState } from 'react';

export interface AsyncResource<T> {
  data: T | undefined;
  error: Error | undefined;
  isLoading: boolean;
  retry: () => void;
}

interface ResourceResult<T> {
  loader: (signal: AbortSignal) => Promise<T>;
  attempt: number;
  data?: T;
  error?: Error;
}

export function useAsyncResource<T>(
  loader: (signal: AbortSignal) => Promise<T>,
): AsyncResource<T> {
  const [result, setResult] = useState<ResourceResult<T>>();
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    void loader(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult({ loader, attempt, data });
        }
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted || (reason instanceof Error && reason.name === 'AbortError')) {
          return;
        }

        setResult({
          loader,
          attempt,
          error: reason instanceof Error ? reason : new Error('An unexpected error occurred.'),
        });
      });

    return () => controller.abort();
  }, [attempt, loader]);

  const retry = useCallback(() => setAttempt((value) => value + 1), []);
  const isCurrentResult = result?.loader === loader && result.attempt === attempt;
  return {
    data: isCurrentResult ? result.data : undefined,
    error: isCurrentResult ? result.error : undefined,
    isLoading: !isCurrentResult,
    retry,
  };
}
