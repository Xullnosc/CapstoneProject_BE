-- 1 Drop FK
ALTER TABLE thesis_histories
DROP FOREIGN KEY thesis_histories_ibfk_1;

-- 2 Modify column type
ALTER TABLE thesis_histories
MODIFY ThesisId CHAR(36) NOT NULL;

-- 3 Recreate FK
ALTER TABLE thesis_histories
ADD CONSTRAINT thesis_histories_ibfk_1
FOREIGN KEY (ThesisId)
REFERENCES thesis(ThesisId);
