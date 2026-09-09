type CsrfEnvelope = {
  requestToken: string;
  tokenType: 'csrf';
  expiresInSeconds: number;
};

let csrfToken: string | null = null;

export async function getCsrf(): Promise<CsrfEnvelope> {
  const response = await fetch('/api/v1/auth/csrf', {
    method: 'GET',
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
    cache: 'no-store'
  });

  if (!response.ok) {
    throw new Error('CSRF endpoint unavailable');
  }

  const body = (await response.json()) as CsrfEnvelope;
  csrfToken = body.requestToken;
  return body;
}

export function getCsrfTokenFromMemory(): string | null {
  return csrfToken;
}
