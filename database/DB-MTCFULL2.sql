-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               12.1.2-MariaDB - MariaDB Server
-- Server OS:                    Win64
-- HeidiSQL Version:             11.3.0.6295
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for db_maintenance
CREATE DATABASE IF NOT EXISTS `db_maintenance` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_uca1400_ai_ci */;
USE `db_maintenance`;

-- Dumping structure for table db_maintenance.actions
CREATE TABLE IF NOT EXISTS `actions` (
  `action_id` int(11) NOT NULL AUTO_INCREMENT,
  `action_name` varchar(100) NOT NULL,
  PRIMARY KEY (`action_id`)
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.actions: ~22 rows (approximately)
DELETE FROM `actions`;
/*!40000 ALTER TABLE `actions` DISABLE KEYS */;
INSERT INTO `actions` (`action_id`, `action_name`) VALUES
	(1, 'Ganti Sparepart'),
	(2, 'Cleaning/Pembersihan'),
	(3, 'Setting/Adjusting'),
	(4, 'Reset Program'),
	(5, 'Inspection Only'),
	(18, 'Adjust Diameter Konduktor'),
	(19, 'Adjust Langkah Terminal'),
	(20, 'Cleaning/Pembersihan'),
	(21, 'Ganti CFM'),
	(22, 'Ganti Crimping Dies'),
	(23, 'Ganti Cutter Blade'),
	(24, 'Ganti Cutting Punch'),
	(25, 'Ganti I/O mesin'),
	(26, 'Ganti Malservo'),
	(27, 'Ganti Roll Terminal'),
	(28, 'Ganti Sparepart'),
	(29, 'Ganti Spring Supporting Stopper'),
	(30, 'Ganti Wire Holder'),
	(31, 'Inspection Only'),
	(32, 'Jig ulang FH11'),
	(33, 'Reset Program'),
	(34, 'Setting/Adjusting');
/*!40000 ALTER TABLE `actions` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.failures
CREATE TABLE IF NOT EXISTS `failures` (
  `failure_id` int(11) NOT NULL AUTO_INCREMENT,
  `failure_name` varchar(100) NOT NULL,
  PRIMARY KEY (`failure_id`)
) ENGINE=InnoDB AUTO_INCREMENT=75 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.failures: ~42 rows (approximately)
DELETE FROM `failures`;
/*!40000 ALTER TABLE `failures` DISABLE KEYS */;
INSERT INTO `failures` (`failure_id`, `failure_name`) VALUES
	(1, 'Mesin Mati Total'),
	(2, 'Suara Kasar'),
	(3, 'Sensor Error'),
	(4, 'Bocor Oli'),
	(5, 'Lain-lain'),
	(38, 'BDCS'),
	(39, 'Bellmouth tidak standart'),
	(40, 'Bocor Oli'),
	(41, 'Cacat Crimp sisi A'),
	(42, 'Cacat Crimp sisi B'),
	(43, 'Cacat Strip sisi A'),
	(44, 'Cacat Strip sisi B'),
	(45, 'CFM mati'),
	(46, 'CFM tidak connect'),
	(47, 'Conveyor tidak berputar'),
	(48, 'Cross section NG'),
	(49, 'Damage Insulation'),
	(50, 'Deformasi Terminal'),
	(51, 'Flash'),
	(52, 'Fraying Core'),
	(53, 'Insulation Tercrimping'),
	(54, 'Insulation Tidak Tercrimping'),
	(55, 'Jalur Chipping Buntu'),
	(56, 'Kanban Tidak Bisa diBarcode'),
	(57, 'Komputer Mati'),
	(58, 'Lain-lain'),
	(59, 'Mesin Mati Total'),
	(60, 'Mesin Off'),
	(61, 'Rear tidak seimbang'),
	(62, 'Seal error'),
	(63, 'Seal Maju Mundur'),
	(64, 'Seal Sobek'),
	(65, 'Seal tidak Insert'),
	(66, 'Sensor Error'),
	(67, 'Servo'),
	(68, 'Stripping NG'),
	(69, 'Suara Kasar'),
	(70, 'Tekanan Udara NG'),
	(71, 'Tergores'),
	(72, 'Terminal Crack'),
	(73, 'Tidak Stripping'),
	(74, 'Wire Terbelit');
/*!40000 ALTER TABLE `failures` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.failure_causes
CREATE TABLE IF NOT EXISTS `failure_causes` (
  `cause_id` int(11) NOT NULL AUTO_INCREMENT,
  `cause_name` varchar(100) NOT NULL,
  PRIMARY KEY (`cause_id`)
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.failure_causes: ~22 rows (approximately)
DELETE FROM `failure_causes`;
/*!40000 ALTER TABLE `failure_causes` DISABLE KEYS */;
INSERT INTO `failure_causes` (`cause_id`, `cause_name`) VALUES
	(1, 'Baut pengunci kendor'),
	(2, 'Crimping Dies Aus'),
	(3, 'Cutter Blade Kotor'),
	(4, 'Langkah tidak Stabil'),
	(5, 'LM Guide Aus'),
	(6, 'Malservo Error'),
	(7, 'Roll Terminal NG'),
	(8, 'Sensor Kotor'),
	(9, 'Spring Aus'),
	(10, 'Spring Patah'),
	(11, 'Terminal tidak center'),
	(12, 'Baut pengunci kendor'),
	(13, 'Crimping Dies Aus'),
	(14, 'Cutter Blade Kotor'),
	(15, 'Langkah tidak Stabil'),
	(16, 'LM Guide Aus'),
	(17, 'Malservo Error'),
	(18, 'Roll Terminal NG'),
	(19, 'Sensor Kotor'),
	(20, 'Spring Aus'),
	(21, 'Spring Patah'),
	(22, 'Terminal tidak center');
/*!40000 ALTER TABLE `failure_causes` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.machines
CREATE TABLE IF NOT EXISTS `machines` (
  `machine_id` int(11) NOT NULL AUTO_INCREMENT,
  `machine_code` varchar(20) DEFAULT NULL,
  `machine_name` varchar(100) DEFAULT NULL,
  `location` varchar(50) DEFAULT NULL,
  `current_status_id` int(11) DEFAULT 1,
  PRIMARY KEY (`machine_id`),
  UNIQUE KEY `machine_code` (`machine_code`),
  KEY `current_status_id` (`current_status_id`),
  CONSTRAINT `1` FOREIGN KEY (`current_status_id`) REFERENCES `machine_statuses` (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.machines: ~2 rows (approximately)
DELETE FROM `machines`;
/*!40000 ALTER TABLE `machines` DISABLE KEYS */;
INSERT INTO `machines` (`machine_id`, `machine_code`, `machine_name`, `location`, `current_status_id`) VALUES
	(1, 'MC-01', 'CNC Milling A', 'Line 1', 1),
	(2, 'MC-02', 'Conveyor B', 'Line 2', 1);
/*!40000 ALTER TABLE `machines` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.machine_statuses
CREATE TABLE IF NOT EXISTS `machine_statuses` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `status_name` varchar(50) NOT NULL,
  PRIMARY KEY (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.machine_statuses: ~2 rows (approximately)
DELETE FROM `machine_statuses`;
/*!40000 ALTER TABLE `machine_statuses` DISABLE KEYS */;
INSERT INTO `machine_statuses` (`status_id`, `status_name`) VALUES
	(1, 'RUNNING'),
	(2, 'DOWN');
/*!40000 ALTER TABLE `machine_statuses` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.parts
CREATE TABLE IF NOT EXISTS `parts` (
  `part_id` int(11) NOT NULL AUTO_INCREMENT,
  `part_code` varchar(50) DEFAULT NULL,
  `part_name` varchar(100) DEFAULT NULL,
  `stock_qty` int(11) DEFAULT 0,
  PRIMARY KEY (`part_id`),
  UNIQUE KEY `part_code` (`part_code`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.parts: ~10 rows (approximately)
DELETE FROM `parts`;
/*!40000 ALTER TABLE `parts` DISABLE KEYS */;
INSERT INTO `parts` (`part_id`, `part_code`, `part_name`, `stock_qty`) VALUES
	(1, 'P-001', 'Sensor Proximity', 10),
	(2, 'P-002', 'V-Belt A20', 5),
	(3, NULL, 'Sensor Proximity', 10),
	(4, NULL, 'Baut M5', 100),
	(5, NULL, 'Kabel Ties', 500),
	(6, NULL, 'Solenoid Valve', 5),
	(7, NULL, 'Sensor Proximity', 10),
	(8, NULL, 'Baut M5', 100),
	(9, NULL, 'Kabel Ties', 500),
	(10, NULL, 'Solenoid Valve', 5);
/*!40000 ALTER TABLE `parts` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.part_requests
CREATE TABLE IF NOT EXISTS `part_requests` (
  `request_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ticket_id` bigint(20) NOT NULL,
  `part_id` int(11) DEFAULT NULL,
  `part_name_manual` varchar(255) DEFAULT NULL,
  `qty` int(11) DEFAULT 1,
  `status_id` int(11) DEFAULT 1,
  `requested_at` datetime DEFAULT current_timestamp(),
  `ready_at` datetime DEFAULT NULL,
  PRIMARY KEY (`request_id`),
  KEY `ticket_id` (`ticket_id`),
  KEY `part_id` (`part_id`),
  KEY `status_id` (`status_id`),
  CONSTRAINT `1` FOREIGN KEY (`ticket_id`) REFERENCES `tickets` (`ticket_id`) ON DELETE CASCADE,
  CONSTRAINT `2` FOREIGN KEY (`part_id`) REFERENCES `parts` (`part_id`) ON DELETE SET NULL,
  CONSTRAINT `3` FOREIGN KEY (`status_id`) REFERENCES `request_statuses` (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.part_requests: ~0 rows (approximately)
DELETE FROM `part_requests`;
/*!40000 ALTER TABLE `part_requests` DISABLE KEYS */;
/*!40000 ALTER TABLE `part_requests` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.problem_types
CREATE TABLE IF NOT EXISTS `problem_types` (
  `type_id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(100) NOT NULL,
  PRIMARY KEY (`type_id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.problem_types: ~7 rows (approximately)
DELETE FROM `problem_types`;
/*!40000 ALTER TABLE `problem_types` DISABLE KEYS */;
INSERT INTO `problem_types` (`type_id`, `type_name`) VALUES
	(1, 'Aplikator'),
	(6, 'CFM error'),
	(5, 'CPU / Monitor problem'),
	(3, 'Cutting / Stripping NG'),
	(7, 'lainnya'),
	(4, 'Rubber Seal'),
	(2, 'Servo');
/*!40000 ALTER TABLE `problem_types` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.request_statuses
CREATE TABLE IF NOT EXISTS `request_statuses` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `status_name` varchar(50) NOT NULL,
  PRIMARY KEY (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.request_statuses: ~4 rows (approximately)
DELETE FROM `request_statuses`;
/*!40000 ALTER TABLE `request_statuses` DISABLE KEYS */;
INSERT INTO `request_statuses` (`status_id`, `status_name`) VALUES
	(1, 'PENDING'),
	(2, 'READY'),
	(3, 'TAKEN'),
	(4, 'REJECTED');
/*!40000 ALTER TABLE `request_statuses` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.roles
CREATE TABLE IF NOT EXISTS `roles` (
  `role_id` int(11) NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) NOT NULL,
  PRIMARY KEY (`role_id`),
  UNIQUE KEY `role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.roles: ~5 rows (approximately)
DELETE FROM `roles`;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` (`role_id`, `role_name`) VALUES
	(4, 'ADMIN'),
	(5, 'GL_PRODUCTION'),
	(1, 'OPERATOR'),
	(3, 'STOCK_CONTROL'),
	(2, 'TECHNICIAN');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.tickets
CREATE TABLE IF NOT EXISTS `tickets` (
  `ticket_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ticket_uuid` char(36) NOT NULL,
  `ticket_display_code` varchar(20) DEFAULT NULL,
  `machine_id` int(11) NOT NULL,
  `operator_id` int(11) NOT NULL,
  `technician_id` int(11) DEFAULT NULL,
  `problem_type_id` int(11) DEFAULT NULL COMMENT 'The Category: Servo, Aplikator, etc.',
  `failure_id` int(11) DEFAULT NULL,
  `failure_remarks` varchar(255) DEFAULT NULL COMMENT 'Manual input for complex problems',
  `applicator_code` varchar(50) DEFAULT NULL,
  `root_cause_id` int(11) DEFAULT NULL,
  `root_cause_remarks` varchar(255) DEFAULT NULL,
  `action_id` int(11) DEFAULT NULL,
  `action_details_manual` varchar(255) DEFAULT NULL COMMENT 'Manual input for complex actions',
  `counter_stroke` int(11) DEFAULT NULL,
  `status_id` int(11) DEFAULT 1,
  `created_at` datetime DEFAULT current_timestamp(),
  `started_at` datetime DEFAULT NULL,
  `technician_finished_at` datetime DEFAULT NULL,
  `gl_validated_at` datetime DEFAULT NULL,
  `gl_rating_score` int(11) DEFAULT NULL,
  `gl_rating_note` text DEFAULT NULL,
  `tech_rating_score` int(11) DEFAULT NULL,
  `tech_rating_note` text DEFAULT NULL,
  PRIMARY KEY (`ticket_id`),
  UNIQUE KEY `ticket_uuid` (`ticket_uuid`),
  KEY `machine_id` (`machine_id`),
  KEY `problem_type_id` (`problem_type_id`),
  KEY `fk_ticket_failure` (`failure_id`),
  KEY `fk_ticket_cause` (`root_cause_id`),
  KEY `fk_ticket_action` (`action_id`),
  CONSTRAINT `fk_ticket_action` FOREIGN KEY (`action_id`) REFERENCES `actions` (`action_id`),
  CONSTRAINT `fk_ticket_cause` FOREIGN KEY (`root_cause_id`) REFERENCES `failure_causes` (`cause_id`),
  CONSTRAINT `fk_ticket_failure` FOREIGN KEY (`failure_id`) REFERENCES `failures` (`failure_id`),
  CONSTRAINT `fk_ticket_problem_type` FOREIGN KEY (`problem_type_id`) REFERENCES `problem_types` (`type_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.tickets: ~0 rows (approximately)
DELETE FROM `tickets`;
/*!40000 ALTER TABLE `tickets` DISABLE KEYS */;
/*!40000 ALTER TABLE `tickets` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.ticket_statuses
CREATE TABLE IF NOT EXISTS `ticket_statuses` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `status_name` varchar(50) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.ticket_statuses: ~3 rows (approximately)
DELETE FROM `ticket_statuses`;
/*!40000 ALTER TABLE `ticket_statuses` DISABLE KEYS */;
INSERT INTO `ticket_statuses` (`status_id`, `status_name`, `description`) VALUES
	(1, 'WAITING', 'Operator sudah lapor, Menunggu Teknisi'),
	(2, 'REPAIRING', 'Teknisi sedang memperbaiki (Timer Repair Jalan)'),
	(3, 'COMPLETED', 'Perbaikan Selesai, Mesin Jalan (Menunggu Rating GL)');
/*!40000 ALTER TABLE `ticket_statuses` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.users
CREATE TABLE IF NOT EXISTS `users` (
  `user_id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password` varchar(255) NOT NULL,
  `full_name` varchar(100) DEFAULT NULL,
  `role_id` int(11) NOT NULL,
  `pin_code` varchar(10) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`role_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.users: ~5 rows (approximately)
DELETE FROM `users`;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` (`user_id`, `username`, `password`, `full_name`, `role_id`, `pin_code`, `is_active`) VALUES
	(1, 'op1', '123', 'Budi Operator', 1, NULL, 1),
	(2, 'tek1', '123', 'Agus Teknisi', 2, '123456', 1),
	(3, 'stock1', '123', 'Siti Gudang', 3, NULL, 1),
	(4, 'admin', '123', 'Admin Pabrik', 4, NULL, 1),
	(5, 'gl1', '123', 'Pak Eko Foreman', 5, NULL, 1);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;

-- Dumping structure for view db_maintenance.view_admin_report
-- Creating temporary table to overcome VIEW dependency errors
CREATE TABLE `view_admin_report` (
	`No Tiket` BIGINT(20) NOT NULL,
	`Waktu Lapor` DATETIME NULL,
	`Nama Mesin` VARCHAR(100) NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Operator` VARCHAR(100) NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Detail Masalah` TEXT NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Status` VARCHAR(50) NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Teknisi` VARCHAR(100) NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Penyebab` VARCHAR(358) NOT NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Tindakan` VARCHAR(358) NOT NULL COLLATE 'utf8mb4_uca1400_ai_ci',
	`Counter` INT(11) NULL,
	`Total Downtime` TIME NULL
) ENGINE=MyISAM;

-- Dumping structure for view db_maintenance.view_admin_report
-- Removing temporary table and create final VIEW structure
DROP TABLE IF EXISTS `view_admin_report`;
CREATE ALGORITHM=UNDEFINED SQL SECURITY DEFINER VIEW `view_admin_report` AS SELECT 
    t.ticket_id AS 'No Tiket',
    t.created_at AS 'Waktu Lapor',
    m.machine_name AS 'Nama Mesin',
    u_op.full_name AS 'Operator',
    
    -- DISPLAY: [Problem Type] Failure Name - Manual Note (App: 123)
    CONCAT(
        IF(pt.type_name IS NOT NULL, CONCAT('[', pt.type_name, '] '), ''), 
        
        -- Logic: If Failure ID exists, use name. If not, check manual text.
        CASE 
            WHEN f.failure_name IS NOT NULL THEN f.failure_name
            WHEN t.failure_remarks IS NOT NULL THEN 'Manual Problem'
            ELSE 'Belum Diisi'
        END,
        
        -- Add manual remarks if they exist
        IF(t.failure_remarks IS NOT NULL, CONCAT(' - ', t.failure_remarks), ''),
        
        -- Add Applicator Code
        IF(t.applicator_code IS NOT NULL, CONCAT(' (App: ', t.applicator_code, ')'), '')
    ) AS 'Detail Masalah',
    
    ts.status_name AS 'Status',
    u_tech.full_name AS 'Teknisi',
    
    -- DISPLAY: Cause Name - Manual Cause
    CONCAT(
        IFNULL(fc.cause_name, ''),
        IF(t.root_cause_remarks IS NOT NULL AND fc.cause_name IS NOT NULL, ' - ', ''),
        IFNULL(t.root_cause_remarks, '-')
    ) AS 'Penyebab',

    -- DISPLAY: Action Name - Manual Action
    CONCAT(
        IFNULL(a.action_name, ''),
        IF(t.action_details_manual IS NOT NULL AND a.action_name IS NOT NULL, ' - ', ''),
        IFNULL(t.action_details_manual, '-')
    ) AS 'Tindakan',
    
    t.counter_stroke AS 'Counter',
    TIMEDIFF(t.technician_finished_at, t.created_at) AS 'Total Downtime'

FROM tickets t
LEFT JOIN machines m ON t.machine_id = m.machine_id
LEFT JOIN users u_op ON t.operator_id = u_op.user_id
LEFT JOIN users u_tech ON t.technician_id = u_tech.user_id
LEFT JOIN ticket_statuses ts ON t.status_id = ts.status_id 
-- Updated Join to problem_types
LEFT JOIN problem_types pt ON t.problem_type_id = pt.type_id
LEFT JOIN failures f ON t.failure_id = f.failure_id
LEFT JOIN failure_causes fc ON t.root_cause_id = fc.cause_id
LEFT JOIN actions a ON t.action_id = a.action_id ;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
