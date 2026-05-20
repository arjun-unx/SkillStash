import { ToggleBookmarkResponse } from '@core/models/skill.model';
import { ToggleTrendingBookmarkResponse } from '@core/models/trending.model';

function readBool(value: unknown): boolean {
  return value === true || value === 'true';
}

/** Maps API JSON whether camelCase or PascalCase. */
export function normalizeToggleBookmark(res: unknown): ToggleBookmarkResponse {
  const o = res as Record<string, unknown>;
  return {
    bookmarked: readBool(o['bookmarked'] ?? o['Bookmarked']),
    bookmarkCount: Number(o['bookmarkCount'] ?? o['BookmarkCount'] ?? 0)
  };
}

export function normalizeToggleTrendingBookmark(res: unknown): ToggleTrendingBookmarkResponse {
  const o = res as Record<string, unknown>;
  return {
    bookmarked: readBool(o['bookmarked'] ?? o['Bookmarked']),
    saveCount: Number(o['saveCount'] ?? o['SaveCount'] ?? 0)
  };
}