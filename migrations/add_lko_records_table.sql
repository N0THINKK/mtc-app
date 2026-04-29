-- Migration: Recreate lko_records table with complete data fields
-- Includes: all operator input, terminal, seal, kombinasi, front/rear crimp data

DROP TABLE IF EXISTS `lko_records`;

CREATE TABLE `lko_records` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `waktu_simpan` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `no_mesin` VARCHAR(50) NOT NULL,
  `shift_name` VARCHAR(50) NOT NULL,
  `nik` VARCHAR(50) NOT NULL,

  -- Sequence & Kanban
  `sequen` VARCHAR(50) NOT NULL,
  `urutan_kanban` VARCHAR(50) DEFAULT NULL,

  -- Produksi
  `qty_product` INT(11) DEFAULT 0,
  `qty_defect_mesin` INT(11) DEFAULT 0,
  `qty_defect_operator` INT(11) DEFAULT 0,
  `kode_defect` VARCHAR(100) DEFAULT NULL,
  `lot_id_wire` VARCHAR(100) DEFAULT NULL,
  `cut_length` VARCHAR(50) DEFAULT NULL,

  -- Master data (prdmst)
  `kombinasi_wire` VARCHAR(200) DEFAULT NULL,
  `terminal_a` VARCHAR(100) DEFAULT NULL,
  `terminal_b` VARCHAR(100) DEFAULT NULL,
  `seal_a` VARCHAR(100) DEFAULT NULL,
  `seal_b` VARCHAR(100) DEFAULT NULL,
  `qty_master` VARCHAR(50) DEFAULT NULL,

  -- Jissk data — Front/Rear Crimp Height & Width (Sisi A)
  `front_ch_a` VARCHAR(20) DEFAULT '0',
  `front_cw_a` VARCHAR(20) DEFAULT '0',
  `rear_ch_a` VARCHAR(20) DEFAULT '0',
  `rear_cw_a` VARCHAR(20) DEFAULT '0',

  -- Jissk data — Front/Rear Crimp Height & Width (Sisi B)
  `front_ch_b` VARCHAR(20) DEFAULT '0',
  `front_cw_b` VARCHAR(20) DEFAULT '0',
  `rear_ch_b` VARCHAR(20) DEFAULT '0',
  `rear_cw_b` VARCHAR(20) DEFAULT '0',

  -- Waktu dari mesin (PrdLog)
  `waktu_mulai` VARCHAR(50) DEFAULT NULL,
  `waktu_selesai` VARCHAR(50) DEFAULT NULL,

  PRIMARY KEY (`id`),
  INDEX `idx_lko_sequen` (`sequen`),
  INDEX `idx_lko_mesin_date` (`no_mesin`, `waktu_simpan`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
