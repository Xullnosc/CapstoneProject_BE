# Repair flyway schema history
.\flyway-maintenance.ps1 -Action repair

# Revert using undo (if available)
.\flyway-maintenance.ps1 -Action revert -RevertMode undo -TargetVersion 27

# Revert using clean + migrate target (destructive, requires -Force)
.\flyway-maintenance.ps1 -Action revert -RevertMode clean-and-migrate-target -TargetVersion 27 -Force

# Run flyway out of order for versions
.\run-flyway.ps1 -command migrate -OutOfOrder