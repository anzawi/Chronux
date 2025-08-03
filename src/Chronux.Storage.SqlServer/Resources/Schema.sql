CREATE TABLE Chronux_Jobs
(
    JobId              NVARCHAR(100) PRIMARY KEY,
    HandlerType        NVARCHAR(400) NOT NULL,
    ContextType        NVARCHAR(400) NOT NULL,
    Description        NVARCHAR(500),
    Tags               NVARCHAR(MAX),
    Metadata           NVARCHAR(MAX),
    TimeoutSec         INT NULL,
    UseDistributedLock BIT NOT NULL DEFAULT 0,
    LockKey            NVARCHAR(200) NULL
);


CREATE TABLE Chronux_Triggers
(
    JobId         NVARCHAR(100) PRIMARY KEY,
    TriggerId     NVARCHAR(100) NOT NULL,
    Type          NVARCHAR(50) NOT NULL, -- Cron, Interval, Delay, etc.
    Expression    NVARCHAR(200) NULL,
    IntervalSec   INT NULL,
    DelaySec      INT NULL,
    TimeZone      NVARCHAR(100),
    EnableMisfire BIT NOT NULL DEFAULT 0
);


CREATE TABLE Chronux_TriggerStates
(
    JobId       NVARCHAR(100) PRIMARY KEY,
    LastFiredAt DATETIMEOFFSET NULL,
    NextDueAt   DATETIMEOFFSET NULL
);


CREATE TABLE Chronux_ExecutionLogs
(
    LogId              UNIQUEIDENTIFIER PRIMARY KEY,
    JobId              NVARCHAR(100),
    ExecutedAt         DATETIMEOFFSET,
    Success            BIT,
    Message            NVARCHAR(MAX),
    Exception          NVARCHAR(MAX),
    DurationMs         INT,
    RetryAttempt       INT,
    RetryCount         INT,
    MaxAttemptsReached BIT,
    TriggerId          NVARCHAR(100),
    InstanceId         NVARCHAR(100),
    Tags               NVARCHAR(MAX),
    CorrelationId      NVARCHAR(100),
    TriggerSource      NVARCHAR(100),
    UserId             NVARCHAR(100),
    Output             NVARCHAR(MAX)
);


CREATE TABLE Chronux_DeadLetters
(
    Id            UNIQUEIDENTIFIER PRIMARY KEY,
    JobId         NVARCHAR(100),
    FailedAt      DATETIMEOFFSET,
    Input         NVARCHAR(MAX),
    ErrorMessage  NVARCHAR(MAX),
    Exception     NVARCHAR(MAX),
    RetryAttempt  INT,
    MaxAttempts   INT,
    TriggerId     NVARCHAR(100),
    InstanceId    NVARCHAR(100),
    Tags          NVARCHAR(MAX),
    CorrelationId NVARCHAR(100),
    TriggerSource NVARCHAR(100),
    UserId        NVARCHAR(100)
);


CREATE TABLE Chronux_JobQueue
(
    Id         UNIQUEIDENTIFIER PRIMARY KEY,
    JobId      NVARCHAR(100),
    Input      NVARCHAR(MAX),
    EnqueuedAt DATETIMEOFFSET
);

CREATE TABLE Chronux_ClusterLocks
(
    LockKey    NVARCHAR(200) PRIMARY KEY,
    AcquiredBy NVARCHAR(200),
    AcquiredAt DATETIMEOFFSET,
    ExpiresAt  DATETIMEOFFSET
);

CREATE TABLE Chronux_ClusterLocks
(
    LockKey    NVARCHAR(200) PRIMARY KEY,
    AcquiredBy NVARCHAR(200) NOT NULL,
    AcquiredAt DATETIMEOFFSET NOT NULL,
    ExpiresAt  DATETIMEOFFSET NOT NULL
);