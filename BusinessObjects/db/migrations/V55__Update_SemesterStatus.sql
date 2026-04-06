UPDATE semesters SET Status = 'Open' WHERE Status IN ('Active', 'Upcoming');
UPDATE semesters SET Status = 'In Progress' WHERE Status IN ('Review Thesis', 'Review Middle Semester');