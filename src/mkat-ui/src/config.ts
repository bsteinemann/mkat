// src/mkat-ui/src/config.ts

declare global {
  interface Window {
    __MKAT_BASE_PATH__?: string;
  }
}

export function getBasePath(): string {
  const base = window.__MKAT_BASE_PATH__ ?? '';
  // Ensure no trailing slash (TanStack Router expects "/mkat" not "/mkat/")
  return base.endsWith('/') ? base.slice(0, -1) : base;
}
