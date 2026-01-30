import { getBasePath } from './config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) return '';
  return `Basic ${credentials}`;
}

export function connectSSE(onEvent: (type: string, data: unknown) => void): () => void {
  const authHeader = getAuthHeader();
  if (!authHeader) return () => {};

  const controller = new AbortController();

  (async () => {
    try {
      const response = await fetch(`${getApiBase()}/events/stream`, {
        headers: { Authorization: authHeader },
        signal: controller.signal,
      });

      if (!response.ok || !response.body) return;

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let currentEvent = '';
      let currentData = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('event: ')) {
            currentEvent = line.slice(7).trim();
          } else if (line.startsWith('data: ')) {
            currentData = line.slice(6);
          } else if (line === '') {
            if (currentEvent && currentData) {
              try {
                const parsed = JSON.parse(currentData);
                onEvent(currentEvent, parsed);
              } catch {
                onEvent(currentEvent, currentData);
              }
            }
            currentEvent = '';
            currentData = '';
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name !== 'AbortError') {
        console.error('SSE connection error:', err);
        setTimeout(() => connectSSE(onEvent), 5000);
      }
    }
  })();

  return () => controller.abort();
}
