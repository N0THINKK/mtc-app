-- Create ticket_problems table
CREATE TABLE IF NOT EXISTS ticket_problems (
    problem_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL,
    problem_type_id INT,
    problem_type_remarks TEXT,
    failure_id INT,
    failure_remarks TEXT,
    root_cause_id INT,
    root_cause_remarks TEXT,
    action_id INT,
    action_details_manual TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (ticket_id) REFERENCES tickets(ticket_id) ON DELETE CASCADE
);

-- OPTIONAL: Migrate existing data from tickets table to ticket_problems
-- Only run this if you want to preserve old ticket problems
INSERT INTO ticket_problems (ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks, root_cause_id, root_cause_remarks, action_id, action_details_manual, created_at)
SELECT ticket_id, problem_type_id, problem_type_remarks, failure_id, failure_remarks, root_cause_id, root_cause_remarks, action_id, action_details_manual, created_at
FROM tickets
WHERE failure_id IS NOT NULL OR problem_type_id IS NOT NULL OR problem_type_remarks IS NOT NULL;
