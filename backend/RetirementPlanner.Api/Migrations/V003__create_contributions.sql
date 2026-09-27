-- One row per goal and calendar month; the unique key blocks recording the same month twice.
-- The unique key also serves lookups by GoalId, so no separate index is needed.
CREATE TABLE Contributions (
    Id         INT               NOT NULL AUTO_INCREMENT,
    GoalId     INT               NOT NULL,
    `Year`     SMALLINT UNSIGNED NOT NULL,
    `Month`    TINYINT UNSIGNED  NOT NULL,
    Amount     DECIMAL(18,2)     NOT NULL,
    RecordedAt DATETIME(6)       NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT PK_Contributions PRIMARY KEY (Id),
    CONSTRAINT FK_Contributions_Goals FOREIGN KEY (GoalId) REFERENCES Goals (Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Contributions_Goal_Period UNIQUE (GoalId, `Year`, `Month`),
    CONSTRAINT CK_Contributions_Month CHECK (`Month` BETWEEN 1 AND 12),
    CONSTRAINT CK_Contributions_Year CHECK (`Year` BETWEEN 1900 AND 2200),
    CONSTRAINT CK_Contributions_Amount CHECK (Amount > 0)
);
