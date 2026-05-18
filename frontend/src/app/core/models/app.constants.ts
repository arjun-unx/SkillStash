export const API_ROUTES = {
  authRegister: '/auth/register',
  authLogin: '/auth/login',
  authMe: '/auth/me',
  skills: '/skills',
  skillsFeed: '/skills',
  skillsFollowing: '/skills/following',
  skillsMine: '/skills/mine',
  library: '/library',
  users: '/users'
} as const;

export const STORAGE_KEYS = {
  accessToken: 'ps.accessToken',
  expiresAtUtc: 'ps.expiresAt',
  user: 'ps.user'
} as const;

export const AGENT_SLUGS = [
  { id: 'claude', label: 'Claude' },
  { id: 'openai', label: 'ChatGPT / OpenAI' },
  { id: 'gemini', label: 'Gemini' },
  { id: 'cursor', label: 'Cursor' },
  { id: 'grok', label: 'Grok' },
  { id: 'any', label: 'Any agent' }
] as const;
