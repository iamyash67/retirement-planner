-- Results of Monte Carlo runs for a goal. Parameters and percentile bands are stored as JSON
-- because their shape belongs to the simulation service and is always read as a whole.
CREATE TABLE SimulationRuns (
    Id                  INT          NOT NULL AUTO_INCREMENT,
    GoalId              INT          NOT NULL,
    ParametersJson      JSON         NOT NULL,
    SuccessProbability  DECIMAL(5,4) NOT NULL,
    PercentileBandsJson JSON         NOT NULL,
    CreatedAt           DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT PK_SimulationRuns PRIMARY KEY (Id),
    INDEX IX_SimulationRuns_GoalId_CreatedAt (GoalId, CreatedAt),
    CONSTRAINT FK_SimulationRuns_Goals FOREIGN KEY (GoalId) REFERENCES Goals (Id) ON DELETE CASCADE,
    CONSTRAINT CK_SimulationRuns_SuccessProbability CHECK (SuccessProbability BETWEEN 0 AND 1)
);
