-- Create ticket_technician_sessions table
-- Tracks multiple technicians working on a single ticket across shifts

CREATE TABLE IF NOT EXISTS `ticket_technician_sessions` (
    `session_id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ticket_id` BIGINT NOT NULL,
    `technician_id` INT NOT NULL,
    `shift_id` INT DEFAULT NULL,
    `started_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ended_at` DATETIME DEFAULT NULL,
    `elapsed_seconds` INT NOT NULL DEFAULT 0,
    `is_completing_session` TINYINT(1) NOT NULL DEFAULT 0,
    CONSTRAINT `fk_tts_ticket` FOREIGN KEY (`ticket_id`) REFERENCES `tickets`(`ticket_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_tts_technician` FOREIGN KEY (`technician_id`) REFERENCES `users`(`user_id`),
    CONSTRAINT `fk_tts_shift` FOREIGN KEY (`shift_id`) REFERENCES `shifts`(`shift_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Index for fast lookup by ticket
CREATE INDEX `idx_tts_ticket` ON `ticket_technician_sessions`(`ticket_id`);
