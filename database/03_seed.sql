USE retirement_planner;
-- Demo login: demo / demo123
INSERT INTO Profiles (FirstName, LastName, Age, Gender, UserName, Password)
VALUES ('Demo', 'User', 30, 'Male', 'demo', 'demo123')
ON DUPLICATE KEY UPDATE UserName = UserName;
