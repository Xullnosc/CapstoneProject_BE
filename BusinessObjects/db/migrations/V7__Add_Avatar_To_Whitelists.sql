-- Add Avatar to Whitelist (Singular)
ALTER TABLE Whitelist ADD Avatar TEXT NULL;

-- Add Avatar to archived_whitelists (Snake case)
ALTER TABLE archived_whitelists ADD Avatar TEXT NULL;
