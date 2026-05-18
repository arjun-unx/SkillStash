export type TrendingSortMode = 'trending' | 'bookmarked' | 'used' | 'latest' | 'rated';

export interface TrendingProviderDto {
  slug: string;
  name: string;
  shortLabel: string;
  description: string;
  icon: string;
  skillCount: number;
  trendingScore: number;
  lastSyncedAtUtc?: string | null;
}

export interface TrendingSkillDto {
  id: string;
  providerSlug: string;
  providerName: string;
  sourceName: string;
  sourceUrl: string;
  title: string;
  body: string;
  snippet: string;
  roleCategory: string;
  category?: string | null;
  tags: string[];
  trendingScore: number;
  useCount: number;
  saveCount: number;
  rating: number;
  sourceUpdatedAtUtc?: string | null;
  syncedAtUtc: string;
  bookmarkedByCurrentUser: boolean;
}

export interface ToggleTrendingBookmarkResponse {
  bookmarked: boolean;
  saveCount: number;
}

export const TRENDING_ROLE_FILTERS = [
  'Code review',
  'Testing',
  'Documentation',
  'Design',
  'Data',
  'Security',
  'General'
] as const;
