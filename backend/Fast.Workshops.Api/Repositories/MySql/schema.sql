CREATE TABLE IF NOT EXISTS workshops (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name LONGTEXT NOT NULL,
    held_at VARCHAR(33) NOT NULL,
    description LONGTEXT NOT NULL
) ENGINE=InnoDB CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS collaborators (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name LONGTEXT NOT NULL
) ENGINE=InnoDB CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS attendance_records (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    workshop_id INT NOT NULL UNIQUE,
    FOREIGN KEY (workshop_id) REFERENCES workshops(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS attendance_participants (
    attendance_id INT NOT NULL,
    collaborator_id INT NOT NULL,
    PRIMARY KEY (attendance_id, collaborator_id),
    FOREIGN KEY (attendance_id) REFERENCES attendance_records(id),
    FOREIGN KEY (collaborator_id) REFERENCES collaborators(id)
) ENGINE=InnoDB;
