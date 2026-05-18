export enum SkillVisibility {
  Private = 'Private',
  Public = 'Public',
  Unlisted = 'Unlisted'
}

export interface SkillDto {
  id: string;
  title: string;
  body: string;
  description?: string | null;
  agentSlug: string;
  visibility: SkillVisibility;
  tags: string[];
  copyCount: number;
  likeCount: number;
  bookmarkCount: number;
  commentCount: number;
  likedByCurrentUser: boolean;
  bookmarkedByCurrentUser: boolean;
  authorId: string;
  authorDisplayName: string;
  authorUserName: string;
  authorHeadline?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CreateSkillRequest {
  title: string;
  body: string;
  description?: string | null;
  agentSlug: string;
  visibility: SkillVisibility;
  tags: string[];
}

export type UpdateSkillRequest = CreateSkillRequest & { id: string };

export interface ToggleLikeResponse {
  liked: boolean;
  likeCount: number;
}

export interface ToggleBookmarkResponse {
  bookmarked: boolean;
  bookmarkCount: number;
}

export interface TrackCopyResponse {
  copyCount: number;
}

export interface SkillCommentDto {
  id: string;
  body: string;
  authorDisplayName: string;
  authorUserName: string;
  createdAtUtc: string;
}

export interface BookmarkCollectionDto {
  id: string;
  name: string;
  itemCount: number;
}

export interface UserProfileDto {
  id: string;
  userName: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  followersCount: number;
  followingCount: number;
  publicSkillsCount: number;
  isFollowedByCurrentUser: boolean;
}

export interface ToggleFollowResponse {
  isFollowing: boolean;
  followersCount: number;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
