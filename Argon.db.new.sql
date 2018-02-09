BEGIN TRANSACTION;
DROP TABLE IF EXISTS `ProcessCounters`;
CREATE TABLE IF NOT EXISTS `ProcessCounters` (
	`Time`	INTEGER NOT NULL,
	`ApplicationId`	INTEGER NOT NULL,
	`ProcessorLoadPercent`	REAL NOT NULL,
	FOREIGN KEY(`ApplicationId`) REFERENCES `Applications`(`Id`)
);
DROP TABLE IF EXISTS `Notifications`;
CREATE TABLE IF NOT EXISTS `Notifications` (
	`Time`	INTEGER NOT NULL,
	`ApplicationId`	INTEGER NOT NULL,
	`Type`	INTEGER NOT NULL,
	`NotActivated`	INTEGER NOT NULL,
	FOREIGN KEY(`ApplicationId`) REFERENCES `Applications`(`Id`)
);
DROP TABLE IF EXISTS `NetworkTraffic`;
CREATE TABLE IF NOT EXISTS `NetworkTraffic` (
	`Time`	INTEGER NOT NULL,
	`ApplicationId`	INTEGER NOT NULL,
	`SentBytes`	INTEGER NOT NULL,
	`RecvBytes`	INTEGER NOT NULL,
	`SourceHostId`	INTEGER NOT NULL,
	`SourcePort`	INTEGER NOT NULL,
	`DestHostId`	INTEGER NOT NULL,
	`DestPort`	INTEGER NOT NULL,
	`Type`	INTEGER NOT NULL,
	FOREIGN KEY(`ApplicationId`) REFERENCES `Applications`(`Id`),
	FOREIGN KEY(`SourceHostId`) REFERENCES `Hosts`(`Id`),
	FOREIGN KEY(`DestHostId`) REFERENCES `Hosts`(`Id`)
);
DROP TABLE IF EXISTS `Hosts`;
CREATE TABLE IF NOT EXISTS `Hosts` (
	`Id`	INTEGER,
	`IP`	TEXT NOT NULL,
	`Hostname`	TEXT,
	PRIMARY KEY(`Id`)
);
DROP TABLE IF EXISTS `CpuSuspendWhitelist`;
CREATE TABLE IF NOT EXISTS `CpuSuspendWhitelist` (
	`ApplicationId`	INTEGER NOT NULL,
	FOREIGN KEY(`ApplicationId`) REFERENCES `Applications`(`Id`)
);
DROP TABLE IF EXISTS `Config`;
CREATE TABLE IF NOT EXISTS `Config` (
	`Name`	TEXT,
	`Value`	INTEGER
);
INSERT INTO `Config` VALUES ('NotifyNewApplication',1);
INSERT INTO `Config` VALUES ('BlockNewConnections',1);
INSERT INTO `Config` VALUES ('SuspendHighCpu',1);
INSERT INTO `Config` VALUES ('NotifyHighCpu',1);
INSERT INTO `Config` VALUES ('HighCpuThreshold',30);
DROP TABLE IF EXISTS `Applications`;
CREATE TABLE IF NOT EXISTS `Applications` (
	`Id`	INTEGER,
	`Name`	TEXT NOT NULL,
	`Path`	TEXT NOT NULL,
	PRIMARY KEY(`Id`)
);
DROP INDEX IF EXISTS `processor_time_index`;
CREATE INDEX IF NOT EXISTS `processor_time_index` ON `ProcessCounters` (
	`time`	DESC
);
DROP INDEX IF EXISTS `network_time_path_index`;
CREATE INDEX IF NOT EXISTS `network_time_path_index` ON `NetworkTraffic` (
	`time`	DESC,
	`ApplicationId`
);
DROP INDEX IF EXISTS `network_time_index`;
CREATE INDEX IF NOT EXISTS `network_time_index` ON `NetworkTraffic` (
	`time`	DESC
);
DROP INDEX IF EXISTS `network_applicationname_index`;
CREATE INDEX IF NOT EXISTS `network_applicationname_index` ON `NetworkTraffic` (
	`ApplicationId`
);
COMMIT;
