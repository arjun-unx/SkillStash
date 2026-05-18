/** Normalized to match stored skill tags (lowercase server-side). */
export const EXPLORE_TAG_FILTERS = [
  'software engineering',
  'frontend',
  'backend',
  'devops',
  'ai/ml',
  'ui/ux',
  'productivity',
  'testing',
  'marketing',
  'design',
  'automation'
] as const;

export type ExploreTag = (typeof EXPLORE_TAG_FILTERS)[number];
