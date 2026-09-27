USE retirement_planner;

DROP PROCEDURE IF EXISTS ValidateLogin;
DROP PROCEDURE IF EXISTS GetGoal;
DROP PROCEDURE IF EXISTS CreateGoal;
DROP PROCEDURE IF EXISTS GetProgress;
DROP PROCEDURE IF EXISTS GetFinancialYearData;
DROP PROCEDURE IF EXISTS CreateFinancialYearData;
DROP PROCEDURE IF EXISTS MarkFinancialInvestment;

DELIMITER $$

-- UserRepository.ValidateCredentialsAsync
CREATE PROCEDURE ValidateLogin(IN p_UserName VARCHAR(100), IN p_Password VARCHAR(255))
BEGIN
    SELECT ProfileId, FirstName, LastName, Age, Gender, UserName
    FROM Profiles
    WHERE UserName = p_UserName AND Password = p_Password;
END$$

-- GoalRepository.GetByProfileIdAsync
CREATE PROCEDURE GetGoal(IN p_ProfileId INT)
BEGIN
    SELECT GoalId, ProfileId, CurrentAge, RetirementAge,
           TargetSavings, MonthlyContribution, CurrentSavings
    FROM Goals
    WHERE ProfileId = p_ProfileId;
END$$

-- GoalRepository.CreateAsync
-- MonthlyContribution is derived: what you must save each month to close the gap by retirement.
CREATE PROCEDURE CreateGoal(
    IN p_ProfileId INT, IN p_CurrentAge INT, IN p_RetirementAge INT,
    IN p_TargetSavings DECIMAL(18,2), IN p_CurrentSavings DECIMAL(18,2))
BEGIN
    INSERT INTO Goals (ProfileId, CurrentAge, RetirementAge, TargetSavings,
                       MonthlyContribution, CurrentSavings)
    VALUES (p_ProfileId, p_CurrentAge, p_RetirementAge, p_TargetSavings,
            ROUND((p_TargetSavings - p_CurrentSavings) / ((p_RetirementAge - p_CurrentAge) * 12), 2),
            p_CurrentSavings);
END$$

-- GoalRepository.GetUserProgressAsync (scalar: total saved so far)
CREATE PROCEDURE GetProgress(IN p_GoalId INT)
BEGIN
    SELECT CurrentSavings FROM Goals WHERE GoalId = p_GoalId;
END$$

-- FinancialYearDataRepo.GetByProfileAndYearAsync
CREATE PROCEDURE GetFinancialYearData(IN p_GoalId INT, IN p_Year INT, IN p_Month INT)
BEGIN
    SELECT Id, GoalId, `Year`, `Month`, MonthlyInvestment, IsInvested
    FROM FinancialYearData
    WHERE GoalId = p_GoalId AND `Year` = p_Year AND `Month` = p_Month;
END$$

-- FinancialYearDataRepo.CreateOrUpdateAsync (returns the row)
CREATE PROCEDURE CreateFinancialYearData(
    IN p_GoalId INT, IN p_Year INT, IN p_Month INT, IN p_Amount DECIMAL(18,2))
BEGIN
    INSERT INTO FinancialYearData (GoalId, `Year`, `Month`, MonthlyInvestment, IsInvested)
    VALUES (p_GoalId, p_Year, p_Month, p_Amount, 0)
    ON DUPLICATE KEY UPDATE
        MonthlyInvestment = IF(IsInvested = 0, VALUES(MonthlyInvestment), MonthlyInvestment);

    SELECT Id, GoalId, `Year`, `Month`, MonthlyInvestment, IsInvested
    FROM FinancialYearData
    WHERE GoalId = p_GoalId AND `Year` = p_Year AND `Month` = p_Month;
END$$

-- FinancialYearDataRepo.MarkAsInvestedAsync
-- Single statement: flags the month and adds it to CurrentSavings atomically.
CREATE PROCEDURE MarkFinancialInvestment(IN p_Id INT)
BEGIN
    UPDATE FinancialYearData f
    JOIN Goals g ON g.GoalId = f.GoalId
    SET f.IsInvested = 1,
        g.CurrentSavings = g.CurrentSavings + f.MonthlyInvestment
    WHERE f.Id = p_Id AND f.IsInvested = 0;
END$$

DELIMITER ;
