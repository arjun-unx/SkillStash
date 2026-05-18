-- Idempotent schema alignment for PromptStash when not using EF migrations (EnsureCreated + model changes).
-- Fixes: missing Headline, prompt_bookmarks, prompt_comments, bookmark_collections, processed_messages.

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS "Headline" character varying(120) NULL;

CREATE TABLE IF NOT EXISTS bookmark_collections (
    "Id" uuid NOT NULL CONSTRAINT bookmark_collections_pkey PRIMARY KEY,
    "UserId" uuid NOT NULL CONSTRAINT bookmark_collections_users_fk REFERENCES users("Id") ON DELETE CASCADE,
    "Name" character varying(80) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL
);

CREATE INDEX IF NOT EXISTS "IX_bookmark_collections_UserId_Name"
    ON bookmark_collections ("UserId", "Name");

CREATE TABLE IF NOT EXISTS prompt_bookmarks (
    "Id" uuid NOT NULL CONSTRAINT prompt_bookmarks_pkey PRIMARY KEY,
    "UserId" uuid NOT NULL CONSTRAINT prompt_bookmarks_users_fk REFERENCES users("Id") ON DELETE CASCADE,
    "PromptId" uuid NOT NULL CONSTRAINT prompt_bookmarks_prompts_fk REFERENCES prompts("Id") ON DELETE CASCADE,
    "CollectionId" uuid NULL CONSTRAINT prompt_bookmarks_collections_fk REFERENCES bookmark_collections("Id") ON DELETE SET NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_prompt_bookmarks_UserId_PromptId"
    ON prompt_bookmarks ("UserId", "PromptId");

CREATE TABLE IF NOT EXISTS prompt_comments (
    "Id" uuid NOT NULL CONSTRAINT prompt_comments_pkey PRIMARY KEY,
    "PromptId" uuid NOT NULL CONSTRAINT prompt_comments_prompts_fk REFERENCES prompts("Id") ON DELETE CASCADE,
    "UserId" uuid NOT NULL CONSTRAINT prompt_comments_users_fk REFERENCES users("Id") ON DELETE CASCADE,
    "Body" character varying(2000) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL
);

CREATE INDEX IF NOT EXISTS "IX_prompt_comments_PromptId"
    ON prompt_comments ("PromptId");

CREATE TABLE IF NOT EXISTS processed_messages (
    "Id" uuid NOT NULL CONSTRAINT processed_messages_pkey PRIMARY KEY,
    "MessageId" uuid NOT NULL,
    "EventName" character varying(120) NOT NULL,
    "ProcessedAtUtc" timestamp with time zone NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_processed_messages_MessageId"
    ON processed_messages ("MessageId");

-- Trending (public prompts from external sources). Body is unlimited text — cookbooks can exceed 16k chars.
CREATE TABLE IF NOT EXISTS trending_prompts (
    "Id" uuid NOT NULL CONSTRAINT trending_prompts_pkey PRIMARY KEY,
    "ExternalKey" character varying(128) NOT NULL,
    "ProviderSlug" character varying(32) NOT NULL,
    "SourceName" character varying(120) NOT NULL,
    "SourceUrl" character varying(512) NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Body" text NOT NULL,
    "Snippet" character varying(400) NULL,
    "RoleCategory" character varying(80) NOT NULL,
    "Category" character varying(60) NULL,
    "Tags" text[] NOT NULL,
    "TrendingScore" integer NOT NULL,
    "UseCount" integer NOT NULL,
    "SaveCount" integer NOT NULL,
    "Rating" double precision NOT NULL,
    "SourceUpdatedAtUtc" timestamp with time zone NULL,
    "SyncedAtUtc" timestamp with time zone NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_trending_prompts_ExternalKey"
    ON trending_prompts ("ExternalKey");
CREATE INDEX IF NOT EXISTS "IX_trending_prompts_ProviderSlug"
    ON trending_prompts ("ProviderSlug");
CREATE INDEX IF NOT EXISTS "IX_trending_prompts_RoleCategory"
    ON trending_prompts ("RoleCategory");
CREATE INDEX IF NOT EXISTS "IX_trending_prompts_TrendingScore"
    ON trending_prompts ("TrendingScore");

CREATE TABLE IF NOT EXISTS trending_prompt_bookmarks (
    "Id" uuid NOT NULL CONSTRAINT trending_prompt_bookmarks_pkey PRIMARY KEY,
    "UserId" uuid NOT NULL CONSTRAINT trending_prompt_bookmarks_users_fk REFERENCES users("Id") ON DELETE CASCADE,
    "TrendingPromptId" uuid NOT NULL CONSTRAINT trending_prompt_bookmarks_prompts_fk REFERENCES trending_prompts("Id") ON DELETE CASCADE,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_trending_prompt_bookmarks_UserId_TrendingPromptId"
    ON trending_prompt_bookmarks ("UserId", "TrendingPromptId");

ALTER TABLE IF EXISTS trending_prompts
    ALTER COLUMN "Body" TYPE text;
