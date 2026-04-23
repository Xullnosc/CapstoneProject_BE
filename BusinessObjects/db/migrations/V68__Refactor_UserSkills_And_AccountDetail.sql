-- V68: Refactor UserSkills and AccountDetail
-- 1. Convert SkillTag and SkillLevel from ENUM to VARCHAR to allow freestyle skill entry
ALTER TABLE user_skills MODIFY COLUMN SkillTag VARCHAR(255) NOT NULL;
ALTER TABLE user_skills MODIFY COLUMN SkillLevel VARCHAR(255) DEFAULT 'Beginner';

-- 2. Drop the redundant IsLookingForTeam column from account_detail
ALTER TABLE account_detail DROP COLUMN IsLookingForTeam;
