-- Retirement Planner: schema
CREATE DATABASE IF NOT EXISTS retirement_planner CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE retirement_planner;

CREATE TABLE IF NOT EXISTS Profiles (
    ProfileId  INT AUTO_INCREMENT PRIMARY KEY,
    FirstName  VARCHAR(100) NOT NULL,
    LastName   VARCHAR(100) NOT NULL,
    Age        INT          NOT NULL,
    Gender     VARCHAR(20)  NOT NULL,
    UserName   VARCHAR(100) NOT NULL UNIQUE,
    Password   VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS Goals (
    GoalId              INT AUTO_INCREMENT PRIMARY KEY,
    ProfileId           INT            NOT NULL UNIQUE,   -- one goal per profile
    CurrentAge          INT            NOT NULL,
    RetirementAge       INT            NOT NULL,
    TargetSavings       DECIMAL(18,2)  NOT NULL,
    MonthlyContribution DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CurrentSavings      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CONSTRAINT FK_Goals_Profiles FOREIGN KEY (ProfileId) REFERENCES Profiles(ProfileId)
);

CREATE TABLE IF NOT EXISTS FinancialYearData (
    Id                INT AUTO_INCREMENT PRIMARY KEY,
    GoalId            INT           NOT NULL,
    `Year`            INT           NOT NULL,
    `Month`           INT           NOT NULL,
    MonthlyInvestment DECIMAL(18,2) NOT NULL,
    IsInvested        TINYINT(1)    NOT NULL DEFAULT 0,
    CONSTRAINT FK_FYD_Goals FOREIGN KEY (GoalId) REFERENCES Goals(GoalId),
    CONSTRAINT UQ_FYD_Goal_Period UNIQUE (GoalId, `Year`, `Month`)
);
