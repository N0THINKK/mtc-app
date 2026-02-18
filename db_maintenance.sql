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
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.actions: ~17 rows (approximately)
DELETE FROM `actions`;
/*!40000 ALTER TABLE `actions` DISABLE KEYS */;
INSERT INTO `actions` (`action_id`, `action_name`) VALUES
	(1, 'Ganti Sparepart'),
	(2, 'Cleaning/Pembersihan'),
	(3, 'Setting/Adjusting'),
	(4, 'Reset Program'),
	(5, 'Inspection Only'),
	(6, 'Adjust Diameter Konduktor'),
	(7, 'Adjust Langkah Terminal'),
	(8, 'Ganti Crimping Dies'),
	(9, 'Ganti Malservo'),
	(10, 'Ganti I/O mesin'),
	(11, 'Ganti Spring Supporting Stopper'),
	(12, 'Ganti CFM'),
	(13, 'Ganti Cutter Blade'),
	(14, 'Ganti Cutting Punch'),
	(15, 'Ganti Wire Holder'),
	(16, 'Jig ulang FH11'),
	(17, 'Ganti Roll Terminal');
/*!40000 ALTER TABLE `actions` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.failures
CREATE TABLE IF NOT EXISTS `failures` (
  `failure_id` int(11) NOT NULL AUTO_INCREMENT,
  `failure_name` varchar(100) NOT NULL,
  PRIMARY KEY (`failure_id`)
) ENGINE=InnoDB AUTO_INCREMENT=38 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.failures: ~37 rows (approximately)
DELETE FROM `failures`;
/*!40000 ALTER TABLE `failures` DISABLE KEYS */;
INSERT INTO `failures` (`failure_id`, `failure_name`) VALUES
	(1, 'Mesin Mati Total'),
	(2, 'Suara Kasar'),
	(3, 'Sensor Error'),
	(4, 'Bocor Oli'),
	(5, 'Lain-lain'),
	(6, 'Bellmouth tidak standart'),
	(7, 'Tergores'),
	(8, 'Servo'),
	(9, 'Fraying Core'),
	(10, 'Stripping NG'),
	(11, 'Tidak Stripping'),
	(12, 'Cacat Crimp sisi A'),
	(13, 'Cacat Crimp sisi B'),
	(14, 'Cacat Strip sisi A'),
	(15, 'Cacat Strip sisi B'),
	(16, 'BDCS'),
	(17, 'Deformasi Terminal'),
	(18, 'Mesin Off'),
	(19, 'Terminal Crack'),
	(20, 'Rear tidak seimbang'),
	(21, 'Insulation Tidak Tercrimping'),
	(22, 'Komputer Mati'),
	(23, 'Insulation Tercrimping'),
	(24, 'CFM mati'),
	(25, 'CFM tidak connect'),
	(26, 'Conveyor tidak berputar'),
	(27, 'Seal error'),
	(28, 'Seal Sobek'),
	(29, 'Seal Maju Mundur'),
	(30, 'Seal tidak Insert'),
	(31, 'Jalur Chipping Buntu'),
	(32, 'Tekanan Udara NG'),
	(33, 'Wire Terbelit'),
	(34, 'Damage Insulation'),
	(35, 'Kanban Tidak Bisa diBarcode'),
	(36, 'Flash'),
	(37, 'Cross section NG');
/*!40000 ALTER TABLE `failures` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.failure_causes
CREATE TABLE IF NOT EXISTS `failure_causes` (
  `cause_id` int(11) NOT NULL AUTO_INCREMENT,
  `cause_name` varchar(100) NOT NULL,
  PRIMARY KEY (`cause_id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping data for table db_maintenance.failure_causes: ~11 rows (approximately)
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
	(11, 'Terminal tidak center');
/*!40000 ALTER TABLE `failure_causes` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.machines
CREATE TABLE IF NOT EXISTS `machines` (
  `machine_id` int(11) NOT NULL AUTO_INCREMENT,
  `type_id` int(11) DEFAULT NULL,
  `area_id` int(11) DEFAULT NULL,
  `machine_number` varchar(10) DEFAULT NULL,
  `current_status_id` int(11) DEFAULT 1,
  PRIMARY KEY (`machine_id`),
  KEY `current_status_id` (`current_status_id`),
  KEY `machines_fk_type` (`type_id`),
  KEY `machines_fk_area` (`area_id`),
  CONSTRAINT `machines_fk_area` FOREIGN KEY (`area_id`) REFERENCES `machine_areas` (`area_id`),
  CONSTRAINT `machines_fk_type` FOREIGN KEY (`type_id`) REFERENCES `machine_types` (`type_id`),
  CONSTRAINT `machines_ibfk_1` FOREIGN KEY (`current_status_id`) REFERENCES `machine_statuses` (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.machine_areas
CREATE TABLE IF NOT EXISTS `machine_areas` (
  `area_id` int(11) NOT NULL AUTO_INCREMENT,
  `area_name` varchar(50) NOT NULL,
  PRIMARY KEY (`area_id`),
  UNIQUE KEY `area_name` (`area_name`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.machine_process_logs
CREATE TABLE IF NOT EXISTS `machine_process_logs` (
  `log_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `machine_id` int(11) NOT NULL,
  `produced_lots` bigint(20) DEFAULT 0,
  `produced_pieces` bigint(20) DEFAULT 0,
  `auto_time` double DEFAULT 0,
  `monitor_time` double DEFAULT 0,
  `created_at` datetime DEFAULT current_timestamp(),
  PRIMARY KEY (`log_id`),
  KEY `idx_machine_log_time` (`machine_id`,`created_at`),
  CONSTRAINT `1` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`machine_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.machine_statuses
CREATE TABLE IF NOT EXISTS `machine_statuses` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `status_name` varchar(50) NOT NULL,
  PRIMARY KEY (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.machine_types
CREATE TABLE IF NOT EXISTS `machine_types` (
  `type_id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL,
  PRIMARY KEY (`type_id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.parts
CREATE TABLE IF NOT EXISTS `parts` (
  `part_id` int(11) NOT NULL AUTO_INCREMENT,
  `part_code` varchar(50) DEFAULT NULL,
  `part_name` varchar(100) DEFAULT NULL,
  `stock_qty` int(11) DEFAULT 0,
  PRIMARY KEY (`part_id`),
  UNIQUE KEY `part_code` (`part_code`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

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
  CONSTRAINT `part_requests_ibfk_1` FOREIGN KEY (`ticket_id`) REFERENCES `tickets` (`ticket_id`) ON DELETE CASCADE,
  CONSTRAINT `part_requests_ibfk_2` FOREIGN KEY (`part_id`) REFERENCES `parts` (`part_id`) ON DELETE SET NULL,
  CONSTRAINT `part_requests_ibfk_3` FOREIGN KEY (`status_id`) REFERENCES `request_statuses` (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.problem_types
CREATE TABLE IF NOT EXISTS `problem_types` (
  `type_id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(100) NOT NULL,
  PRIMARY KEY (`type_id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.request_statuses
CREATE TABLE IF NOT EXISTS `request_statuses` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `status_name` varchar(50) NOT NULL,
  PRIMARY KEY (`status_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.roles
CREATE TABLE IF NOT EXISTS `roles` (
  `role_id` int(11) NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) NOT NULL,
  PRIMARY KEY (`role_id`),
  UNIQUE KEY `role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.shifts
CREATE TABLE IF NOT EXISTS `shifts` (
  `shift_id` int(11) NOT NULL AUTO_INCREMENT,
  `shift_name` varchar(10) NOT NULL,
  PRIMARY KEY (`shift_id`),
  UNIQUE KEY `shift_name` (`shift_name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.tickets
CREATE TABLE IF NOT EXISTS `tickets` (
  `ticket_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ticket_uuid` char(36) NOT NULL,
  `ticket_display_code` varchar(20) DEFAULT NULL,
  `machine_id` int(11) NOT NULL,
  `shift_id` int(11) DEFAULT NULL,
  `operator_id` int(11) NOT NULL,
  `technician_id` int(11) DEFAULT NULL,
  `applicator_code` varchar(50) DEFAULT NULL,
  `counter_stroke` int(11) DEFAULT NULL,
  `status_id` int(11) DEFAULT 1,
  `is_machine_running` tinyint(1) NOT NULL DEFAULT 0 COMMENT '0=Stop (Machine Down), 1=Run (Production Running)',
  `created_at` datetime DEFAULT current_timestamp(),
  `started_at` datetime DEFAULT NULL,
  `technician_finished_at` datetime DEFAULT NULL,
  `production_resumed_at` datetime DEFAULT NULL,
  `gl_validated_at` datetime DEFAULT NULL,
  `gl_rating_score` int(11) DEFAULT NULL,
  `gl_rating_note` text DEFAULT NULL,
  `tech_rating_score` int(11) DEFAULT NULL,
  `tech_rating_note` text DEFAULT NULL,
  `is_4m` tinyint(1) DEFAULT 0 COMMENT '1=Yes (Checked), 0=No (Unchecked)',
  `arrival_elapsed_seconds` int(11) NOT NULL DEFAULT 0 COMMENT 'Accumulated seconds for arrival timer (form open)',
  `repair_elapsed_seconds` int(11) NOT NULL DEFAULT 0 COMMENT 'Accumulated seconds for repair timer (form open)',
  `run_elapsed_seconds` int(11) NOT NULL DEFAULT 0 COMMENT 'Accumulated seconds for machine run duration',
  PRIMARY KEY (`ticket_id`),
  UNIQUE KEY `ticket_uuid` (`ticket_uuid`),
  KEY `machine_id` (`machine_id`),
  KEY `tickets_ibfk_shift` (`shift_id`),
  CONSTRAINT `tickets_ibfk_shift` FOREIGN KEY (`shift_id`) REFERENCES `shifts` (`shift_id`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.ticket_problems
CREATE TABLE IF NOT EXISTS `ticket_problems` (
  `problem_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ticket_id` bigint(20) NOT NULL,
  `problem_type_id` int(11) DEFAULT NULL,
  `problem_type_remarks` varchar(255) DEFAULT NULL,
  `failure_id` int(11) DEFAULT NULL,
  `failure_remarks` varchar(255) DEFAULT NULL,
  `root_cause_id` int(11) DEFAULT NULL,
  `root_cause_remarks` varchar(255) DEFAULT NULL,
  `action_id` int(11) DEFAULT NULL,
  `action_details_manual` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`problem_id`),
  KEY `ticket_id` (`ticket_id`),
  CONSTRAINT `ticket_problems_ibfk_1` FOREIGN KEY (`ticket_id`) REFERENCES `tickets` (`ticket_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

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
	(3, 'COMPLETED', 'Perbaikan Selesai');
/*!40000 ALTER TABLE `ticket_statuses` ENABLE KEYS */;

-- Dumping structure for table db_maintenance.ticket_technician_sessions
CREATE TABLE IF NOT EXISTS `ticket_technician_sessions` (
  `session_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ticket_id` bigint(20) NOT NULL,
  `technician_id` int(11) NOT NULL,
  `shift_id` int(11) DEFAULT NULL,
  `started_at` datetime NOT NULL DEFAULT current_timestamp(),
  `ended_at` datetime DEFAULT NULL,
  `elapsed_seconds` int(11) DEFAULT 0,
  `session_notes` text DEFAULT NULL,
  `is_completing_session` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`session_id`),
  KEY `ticket_id` (`ticket_id`),
  KEY `technician_id` (`technician_id`),
  KEY `shift_id` (`shift_id`),
  CONSTRAINT `1` FOREIGN KEY (`ticket_id`) REFERENCES `tickets` (`ticket_id`),
  CONSTRAINT `2` FOREIGN KEY (`technician_id`) REFERENCES `users` (`user_id`),
  CONSTRAINT `3` FOREIGN KEY (`shift_id`) REFERENCES `shifts` (`shift_id`)
) ENGINE=InnoDB AUTO_INCREMENT=46 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dumping structure for table db_maintenance.users
CREATE TABLE IF NOT EXISTS `users` (
  `user_id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password` varchar(255) NOT NULL,
  `full_name` varchar(100) DEFAULT NULL,
  `role_id` int(11) NOT NULL,
  `nik` varchar(10) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`role_id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- --------------------------------------------------------
-- Migration: Add is_machine_running and run_elapsed_seconds columns
-- Run this on existing databases that don't have these columns yet
-- --------------------------------------------------------
-- ALTER TABLE tickets ADD COLUMN is_machine_running TINYINT(1) NOT NULL DEFAULT 0 COMMENT '0=Stop (Machine Down), 1=Run (Production Running)';
-- ALTER TABLE tickets ADD COLUMN run_elapsed_seconds INT NOT NULL DEFAULT 0 COMMENT 'Accumulated seconds for machine run duration';

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
