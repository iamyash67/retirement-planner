CREATE TABLE Users (
    Id           INT          NOT NULL AUTO_INCREMENT,
    Email        VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%')
);

-- 1:1 with Users: the primary key is also the foreign key, so a user can have at most one profile.
CREATE TABLE Profiles (
    UserId      INT          NOT NULL,
    FirstName   VARCHAR(100) NOT NULL,
    LastName    VARCHAR(100) NOT NULL,
    DateOfBirth DATE         NOT NULL,
    Gender      VARCHAR(30)  NULL,
    CONSTRAINT PK_Profiles PRIMARY KEY (UserId),
    CONSTRAINT FK_Profiles_Users FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
);
