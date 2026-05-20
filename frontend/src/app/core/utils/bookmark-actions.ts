import { SkillDto } from '@core/models/skill.model';
import { TrendingSkillDto } from '@core/models/trending.model';

/** Apply a community-skill bookmark toggle result onto the card model. */
export function applySkillBookmarkToggle(skill: SkillDto, bookmarked: boolean, bookmarkCount: number): void {
  skill.bookmarkedByCurrentUser = bookmarked;
  skill.bookmarkCount = bookmarkCount;
}

/** Apply a trending-skill bookmark toggle result onto the card model. */
export function applyTrendingBookmarkToggle(skill: TrendingSkillDto, bookmarked: boolean, saveCount: number): void {
  skill.bookmarkedByCurrentUser = bookmarked;
  skill.saveCount = saveCount;
}
