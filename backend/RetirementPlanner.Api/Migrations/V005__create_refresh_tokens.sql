-- Rotating refresh tokens. Only a SHA-256 hash of each token is stored, so a database leak does not
-- expose usable tokens. Every token issued from one login shares a FamilyId; presenting a revoked
-- token again (reuse) revokes the whole family.
CREATE TABLE RefreshTokens (
    Id                INT          NOT NULL AUTO_INCREMENT,
    UserId            INT          NOT NULL,
    FamilyId          CHAR(36)     NOT NULL,
    TokenHash         CHAR(64)     NOT NULL,
    CreatedAt         DATETIME(6)  NOT NULL,
    ExpiresAt         DATETIME(6)  NOT NULL,
    RevokedAt         DATETIME(6)  NULL,
    -- The token that replaced this one on refresh. Deliberately not a foreign key: rows are only ever
    -- deleted together with their user, and a self-reference would complicate that cascade.
    ReplacedByTokenId INT          NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
    INDEX IX_RefreshTokens_FamilyId (FamilyId),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_RefreshTokens_Expiry CHECK (ExpiresAt > CreatedAt)
);
