BEGIN TRANSACTION;
DROP TABLE IF EXISTS `ProcessCounters`;
CREATE TABLE IF NOT EXISTS `ProcessCounters` (
	`Time`	INTEGER NOT NULL,
	`Name`	TEXT NOT NULL,
	`Path`	TEXT NOT NULL,
	`ProcessorLoadPercent`	REAL NOT NULL
);
DROP TABLE IF EXISTS `Notifications`;
CREATE TABLE IF NOT EXISTS `Notifications` (
	`Time`	INTEGER,
	`ApplicationName`	TEXT,
	`ApplicationPath`	TEXT,
	`Type`	INTEGER,
	`NotActivated`	INTEGER
);
DROP TABLE IF EXISTS `NetworkTraffic`;
CREATE TABLE IF NOT EXISTS `NetworkTraffic` (
	`Time`	INTEGER NOT NULL,
	`ApplicationName`	TEXT NOT NULL,
	`ProcessName`	TEXT NOT NULL,
	`FilePath`	TEXT NOT NULL,
	`Sent`	INTEGER NOT NULL,
	`Recv`	INTEGER NOT NULL,
	`SourceAddr`	TEXT NOT NULL,
	`SourcePort`	INTEGER NOT NULL,
	`DestAddr`	TEXT NOT NULL,
	`DestPort`	INTEGER NOT NULL,
	`Type`	INTEGER NOT NULL,
	`ProcessID`	INTEGER
);
DROP TABLE IF EXISTS `CpuSuspendWhitelist`;
CREATE TABLE IF NOT EXISTS `CpuSuspendWhitelist` (
	`Name`	TEXT NOT NULL,
	`Path`	TEXT NOT NULL UNIQUE
);
DROP INDEX IF EXISTS `processor_time_index`;
CREATE INDEX IF NOT EXISTS `processor_time_index` ON `ProcessCounters` (
	`time`	DESC
);
DROP INDEX IF EXISTS `network_time_path_index`;
CREATE INDEX IF NOT EXISTS `network_time_path_index` ON `NetworkTraffic` (
	`time`	DESC,
	`filepath`,
	`ApplicationName`
);
DROP INDEX IF EXISTS `network_time_index`;
CREATE INDEX IF NOT EXISTS `network_time_index` ON `NetworkTraffic` (
	`time`	DESC
);
DROP INDEX IF EXISTS `network_applicationname_index`;
CREATE INDEX IF NOT EXISTS `network_applicationname_index` ON `NetworkTraffic` (
	`ApplicationName`
);
COMMIT;
