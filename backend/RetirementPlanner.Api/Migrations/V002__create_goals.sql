-- Rates are fractions: 0.0600 means 6 % a year.
-- CurrentSavings is the amount saved when the goal was created; recorded contributions are added on read.
CREATE TABLE Goals (
    Id                          INT           NOT NULL AUTO_INCREMENT,
    UserId                      INT           NOT NULL,
    Name                        VARCHAR(100)  NOT NULL,
    CurrentAge                  TINYINT UNSIGNED NOT NULL,
    RetirementAge               TINYINT UNSIGNED NOT NULL,
    TargetAmount                DECIMAL(18,2) NOT NULL,
    CurrentSavings              DECIMAL(18,2) NOT NULL DEFAULT 0,
    ExpectedAnnualReturn        DECIMAL(6,4)  NOT NULL,
    ReturnVolatility            DECIMAL(6,4)  NOT NULL,
    InflationRate               DECIMAL(6,4)  NOT NULL,
    AnnualContributionIncrease  DECIMAL(6,4)  NOT NULL DEFAULT 0,
    PlannedMonthlyContribution  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt                   DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT PK_Goals PRIMARY KEY (Id),
    INDEX IX_Goals_UserId_CreatedAt (UserId, CreatedAt),
    CONSTRAINT FK_Goals_Users FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_Goals_CurrentAge CHECK (CurrentAge BETWEEN 1 AND 120),
    CONSTRAINT CK_Goals_RetirementAge CHECK (RetirementAge > CurrentAge AND RetirementAge <= 120),
    CONSTRAINT CK_Goals_TargetAmount CHECK (TargetAmount > 0),
    CONSTRAINT CK_Goals_CurrentSavings CHECK (CurrentSavings >= 0),
    CONSTRAINT CK_Goals_ExpectedAnnualReturn CHECK (ExpectedAnnualReturn BETWEEN -1 AND 1),
    CONSTRAINT CK_Goals_ReturnVolatility CHECK (ReturnVolatility BETWEEN 0 AND 1),
    CONSTRAINT CK_Goals_InflationRate CHECK (InflationRate BETWEEN -0.5 AND 1),
    CONSTRAINT CK_Goals_AnnualContributionIncrease CHECK (AnnualContributionIncrease BETWEEN 0 AND 1),
    CONSTRAINT CK_Goals_PlannedMonthlyContribution CHECK (PlannedMonthlyContribution >= 0)
);
