BEGIN TRANSACTION;
DROP TABLE IF EXISTS `Config`;
CREATE TABLE IF NOT EXISTS `Config` (
	`Name`	TEXT,
	`Value`	INTEGER
);
INSERT INTO `Config` VALUES ('NotifyNewApplication',1);
INSERT INTO `Config` VALUES ('BlockNewConnections',1);
INSERT INTO `Config` VALUES ('SuspendHighCpu',1);
INSERT INTO `Config` VALUES ('NotifyHighCpu',1);
INSERT INTO `Config` VALUES ('HighCpuThreshold',35);
COMMIT;
