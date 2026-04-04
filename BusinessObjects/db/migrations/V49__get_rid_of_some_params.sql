-- Remove system parameters that are hardcoded in business logic and should not be admin-configurable
DELETE FROM SystemParameters WHERE `Key` IN ('MAX_TEAM_SIZE', 'MIN_TEAM_SIZE');
