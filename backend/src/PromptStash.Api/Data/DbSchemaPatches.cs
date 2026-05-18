using Microsoft.EntityFrameworkCore;

namespace PromptStash.Api.Data;

/// <summary>
/// Idempotent DDL when using EnsureCreated (no EF migrations).
/// </summary>
internal static class DbSchemaPatches
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE users
            ADD COLUMN IF NOT EXISTS "Headline" character varying(120) NULL;

            -- Legacy prompt tables → skills
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'prompts')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'skills') THEN
                ALTER TABLE prompts RENAME TO skills;
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'skills' AND column_name = 'TargetModel') THEN
                ALTER TABLE skills RENAME COLUMN "TargetModel" TO "AgentSlug";
              END IF;
            END $$;

            ALTER TABLE IF EXISTS skills ALTER COLUMN "Body" TYPE text;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'prompt_likes')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'skill_likes') THEN
                ALTER TABLE prompt_likes RENAME TO skill_likes;
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'skill_likes' AND column_name = 'PromptId') THEN
                ALTER TABLE skill_likes RENAME COLUMN "PromptId" TO "SkillId";
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'prompt_comments')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'skill_comments') THEN
                ALTER TABLE prompt_comments RENAME TO skill_comments;
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'skill_comments' AND column_name = 'PromptId') THEN
                ALTER TABLE skill_comments RENAME COLUMN "PromptId" TO "SkillId";
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'prompt_bookmarks')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'skill_bookmarks') THEN
                ALTER TABLE prompt_bookmarks RENAME TO skill_bookmarks;
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'skill_bookmarks' AND column_name = 'PromptId') THEN
                ALTER TABLE skill_bookmarks RENAME COLUMN "PromptId" TO "SkillId";
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'skill_bookmarks' AND column_name = 'BookmarkCollectionId') THEN
                ALTER TABLE skill_bookmarks RENAME COLUMN "BookmarkCollectionId" TO "CollectionId";
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'prompt_bookmarks' AND column_name = 'BookmarkCollectionId') THEN
                ALTER TABLE prompt_bookmarks RENAME COLUMN "BookmarkCollectionId" TO "CollectionId";
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trending_prompts')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trending_skills') THEN
                ALTER TABLE trending_prompts RENAME TO trending_skills;
              END IF;
            END $$;

            ALTER TABLE IF EXISTS trending_skills ALTER COLUMN "Body" TYPE text;
            ALTER TABLE IF EXISTS trending_skills ALTER COLUMN "ExternalKey" TYPE character varying(256);

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trending_prompt_bookmarks')
                 AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trending_skill_bookmarks') THEN
                ALTER TABLE trending_prompt_bookmarks RENAME TO trending_skill_bookmarks;
              END IF;
            END $$;

            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'trending_skill_bookmarks' AND column_name = 'TrendingPromptId') THEN
                ALTER TABLE trending_skill_bookmarks RENAME COLUMN "TrendingPromptId" TO "TrendingSkillId";
              END IF;
            END $$;
            """,
            cancellationToken: ct);

        await db.Database.ExecuteSqlRawAsync(
            """
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

            CREATE TABLE IF NOT EXISTS skills (
                "Id" uuid NOT NULL CONSTRAINT skills_pkey PRIMARY KEY,
                "Title" character varying(120) NOT NULL,
                "Body" text NOT NULL,
                "Description" character varying(280) NULL,
                "AgentSlug" character varying(32) NOT NULL DEFAULT 'any',
                "Visibility" integer NOT NULL,
                "CopyCount" integer NOT NULL DEFAULT 0,
                "LikeCount" integer NOT NULL DEFAULT 0,
                "Tags" text[] NOT NULL,
                "AuthorId" uuid NOT NULL CONSTRAINT skills_users_fk REFERENCES users("Id") ON DELETE CASCADE,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedByUserId" uuid NULL,
                "IsDeleted" boolean NOT NULL DEFAULT false,
                "DeletedAtUtc" timestamp with time zone NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_skills_AuthorId" ON skills ("AuthorId");
            CREATE INDEX IF NOT EXISTS "IX_skills_Visibility" ON skills ("Visibility");
            CREATE INDEX IF NOT EXISTS "IX_skills_CreatedAtUtc" ON skills ("CreatedAtUtc");

            CREATE TABLE IF NOT EXISTS skill_bookmarks (
                "Id" uuid NOT NULL CONSTRAINT skill_bookmarks_pkey PRIMARY KEY,
                "UserId" uuid NOT NULL CONSTRAINT skill_bookmarks_users_fk REFERENCES users("Id") ON DELETE CASCADE,
                "SkillId" uuid NOT NULL CONSTRAINT skill_bookmarks_skills_fk REFERENCES skills("Id") ON DELETE CASCADE,
                "CollectionId" uuid NULL CONSTRAINT skill_bookmarks_collections_fk REFERENCES bookmark_collections("Id") ON DELETE SET NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedByUserId" uuid NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_skill_bookmarks_UserId_SkillId"
                ON skill_bookmarks ("UserId", "SkillId");

            CREATE TABLE IF NOT EXISTS skill_comments (
                "Id" uuid NOT NULL CONSTRAINT skill_comments_pkey PRIMARY KEY,
                "SkillId" uuid NOT NULL CONSTRAINT skill_comments_skills_fk REFERENCES skills("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL CONSTRAINT skill_comments_users_fk REFERENCES users("Id") ON DELETE CASCADE,
                "Body" character varying(2000) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedByUserId" uuid NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_skill_comments_SkillId" ON skill_comments ("SkillId");

            CREATE TABLE IF NOT EXISTS skill_likes (
                "Id" uuid NOT NULL CONSTRAINT skill_likes_pkey PRIMARY KEY,
                "SkillId" uuid NOT NULL CONSTRAINT skill_likes_skills_fk REFERENCES skills("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedByUserId" uuid NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_skill_likes_SkillId_UserId"
                ON skill_likes ("SkillId", "UserId");

            CREATE TABLE IF NOT EXISTS processed_messages (
                "Id" uuid NOT NULL CONSTRAINT processed_messages_pkey PRIMARY KEY,
                "MessageId" uuid NOT NULL,
                "EventName" character varying(120) NOT NULL,
                "ProcessedAtUtc" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_processed_messages_MessageId"
                ON processed_messages ("MessageId");

            CREATE TABLE IF NOT EXISTS trending_skills (
                "Id" uuid NOT NULL CONSTRAINT trending_skills_pkey PRIMARY KEY,
                "ExternalKey" character varying(256) NOT NULL,
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

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_trending_skills_ExternalKey"
                ON trending_skills ("ExternalKey");
            CREATE INDEX IF NOT EXISTS "IX_trending_skills_ProviderSlug"
                ON trending_skills ("ProviderSlug");
            CREATE INDEX IF NOT EXISTS "IX_trending_skills_RoleCategory"
                ON trending_skills ("RoleCategory");
            CREATE INDEX IF NOT EXISTS "IX_trending_skills_TrendingScore"
                ON trending_skills ("TrendingScore");

            CREATE TABLE IF NOT EXISTS trending_skill_bookmarks (
                "Id" uuid NOT NULL CONSTRAINT trending_skill_bookmarks_pkey PRIMARY KEY,
                "UserId" uuid NOT NULL CONSTRAINT trending_skill_bookmarks_users_fk REFERENCES users("Id") ON DELETE CASCADE,
                "TrendingSkillId" uuid NOT NULL CONSTRAINT trending_skill_bookmarks_skills_fk REFERENCES trending_skills("Id") ON DELETE CASCADE,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedByUserId" uuid NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_trending_skill_bookmarks_UserId_TrendingSkillId"
                ON trending_skill_bookmarks ("UserId", "TrendingSkillId");
            """,
            cancellationToken: ct);
    }
}
