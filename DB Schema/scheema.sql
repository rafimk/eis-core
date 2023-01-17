CREATE TABLE EIS_COMPETING_CONSUMER_GROUP
(
    ID                      VARCHAR(50) NOT NULL CONSTRAINT EIS_ROUTE_LOCK_PK PRIMARY KEY,
    HOST_IP_ADDRESS         VARCHAR(255),
    LAST_ACCESSED_TIMESTAMP DATETIME,
    GROUP_KEY               VARCHAR(50) CONSTRAINT EIS_COMPETING_CONSUMER_GROUP_UK UNIQUE
);

-- ALTER TABLE EIS_COMPETING_CONSUMER_GROUP 
--    ADD CONSTRAINT EIS_COMPETING_CONSUMER_GROUP_UK UNIQUE (GROUP_KEY);

CREATE TABLE EIS_EVENT_INBOX_OUTBOX
(
    ID                      VARCHAR(50) NOT NULL EIS_EVENT_INBOX_OUTBOX_PK PRIMARY KEY,
    EVENT_ID                VARCHAR(255),
    TOPIC_QUEUE_NAME        VARCHAR(255),
    EIS_EVENT               VARCHAR(MAX),
    EVENT_TIMESTAMP         DATETIME,
    IS_EVENT_PROCESSED      VARCHAR(50),
    IN_OUT                  VARCHAR(3)
);

ALTER TABLE EIS_EVENT_INBOX_OUTBOX
    ADD CONSTRAINT EIS_EVENT_INBOX_OUTBOX_UK UNIQUE (EVENT_ID, TOPIC_QUEUE_NAME);