Import Template & Usage
=======================

This document explains the expected Excel template for importing whitelist entries and how the import behaves.

Location
--------
- The import code is in `Services.Helpers.ImportHelper` and returns an `ImportResult<WhitelistImportDTO>` with `Items` and `Errors`.

Template layout
---------------
- Header row: row 3
- Headers must start at column B (Excel column 2). Column A may be used for notes or left blank.
- Data rows start at row 4 and continue downward.

Required column headers (exact, case-insensitive)
-------------------------------------------------
- `Email`
- `StudentCode`
- `FullName`
- `RoleId`
- `Campus`
- `SemesterId`

These names come from `BusinessObjects.CampusConstants.WhitelistImportColumns` — do not rename them.

Sample minimal worksheet (showing rows/cols)
-------------------------------------------

Row 1: (optional title)
Row 2: (optional instructions)
Row 3 (header, starting at column B): |  Email  | StudentCode | FullName | RoleId | Campus | SemesterId |
Row 4 (first data row):                | a@x.com | 123456      | Alice    | 2      | FU-Hòa Lạc | 2026 |
Row 5: next entry...

Validation rules
----------------
- `Email`: required, must be a valid email format. Rows with empty or invalid emails are skipped and reported as errors.
- `StudentCode`: optional; empty values will be stored as null.
- `FullName`: used as-is (trimmed). Consider keeping this non-empty.
- `RoleId`: required, must parse to a positive integer. Invalid values are reported and that row is skipped.
- `Campus`: required; prefer one of the campus names defined in `CampusConstants.All` (e.g. `FU-Hòa Lạc`, `FU-Hồ Chí Minh`, `FU-Đà Nẵng`, `FU-Cần Thơ`, `FU-Quy Nhơn`).
- `SemesterId`: required, must parse to a positive integer.

Behavior
--------
- The importer collects all valid rows into `ImportResult.Items`.
- Parsing/validation problems are recorded in `ImportResult.Errors` as `ImportError { Row, Column, Message }`.
- The import does not stop on first error — it continues and returns all found errors and valid items.
- EPPlus license context is set in `Program.cs` (LicenseContext.NonCommercial) so no extra setup is needed for the import helper.

Performance & safety
--------------------
- Avoid extremely large files; there is no streaming DB import in the helper currently.
- Consider splitting large imports into smaller sheets/batches.

Tips for users
--------------
- Keep the header names exact (case-insensitive) and start them at column B on row 3.
- Pre-validate role IDs and semester IDs against your system data so imports map correctly.
- If you need to accept localized numbers, modify `ImportHelper` to use the appropriate `CultureInfo`.

Developer notes
---------------
- The helper uses `headerRow = 3` and `dataStartRow = 4`; change these constants in `ImportHelper` if you want a different template.
- Callers must be updated to inspect `ImportResult.Errors` and decide whether to proceed with persisting `Items`.

Contact
-------
If you want, I can update the UI/ImportService to surface `ImportResult.Errors` back to users. Let me know if you want that implemented.
