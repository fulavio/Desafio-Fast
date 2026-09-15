-- Explicit development seed. Reuse matching names and workshop dates without overwriting user records.
SET NAMES utf8mb4;
START TRANSACTION;

CREATE TEMPORARY TABLE seed_workshops (
    name VARCHAR(100), held_at VARCHAR(33), description LONGTEXT
);
INSERT INTO seed_workshops VALUES
('Clean Code', '2026-07-09T16:00:00.0000000-03:00', 'Práticas para escrever código legível, simples e sustentável.'),
('Angular na prática', '2026-04-09T16:00:00.0000000-03:00', 'Componentes, navegação e experiências acessíveis com Angular.'),
('APIs com ASP.NET Core', '2026-01-08T16:00:00.0000000-03:00', 'Contratos HTTP e boas práticas para construir APIs consistentes.');

CREATE TEMPORARY TABLE seed_collaborators (name VARCHAR(100));
INSERT INTO seed_collaborators VALUES ('Ana Souza'), ('Bruno Lima'), ('Carla Santos'), ('Diego Oliveira');

INSERT INTO workshops (name, held_at, description)
SELECT s.name, s.held_at, s.description FROM seed_workshops s
WHERE NOT EXISTS (SELECT 1 FROM workshops w WHERE w.name = s.name AND w.held_at = s.held_at);

INSERT INTO collaborators (name)
SELECT s.name FROM seed_collaborators s
WHERE NOT EXISTS (SELECT 1 FROM collaborators c WHERE c.name = s.name);

CREATE TEMPORARY TABLE seed_workshop_ids AS
SELECT s.name, MIN(w.id) AS id FROM seed_workshops s
JOIN workshops w ON w.name = s.name AND w.held_at = s.held_at GROUP BY s.name;
CREATE TEMPORARY TABLE seed_collaborator_ids AS
SELECT s.name, MIN(c.id) AS id FROM seed_collaborators s
JOIN collaborators c ON c.name = s.name GROUP BY s.name;

INSERT INTO attendance_records (workshop_id)
SELECT s.id FROM seed_workshop_ids s
WHERE NOT EXISTS (SELECT 1 FROM attendance_records a WHERE a.workshop_id = s.id);

CREATE TEMPORARY TABLE seed_participation (workshop_name VARCHAR(100), collaborator_name VARCHAR(100));
INSERT INTO seed_participation VALUES
('Clean Code', 'Ana Souza'), ('Clean Code', 'Bruno Lima'), ('Clean Code', 'Carla Santos'),
('Angular na prática', 'Bruno Lima'), ('Angular na prática', 'Carla Santos'), ('Angular na prática', 'Diego Oliveira'),
('APIs com ASP.NET Core', 'Ana Souza'), ('APIs com ASP.NET Core', 'Bruno Lima');

INSERT INTO attendance_participants (attendance_id, collaborator_id)
SELECT a.id, c.id FROM seed_participation p
JOIN seed_workshop_ids w ON w.name = p.workshop_name
JOIN seed_collaborator_ids c ON c.name = p.collaborator_name
JOIN attendance_records a ON a.workshop_id = w.id
WHERE NOT EXISTS (
    SELECT 1 FROM attendance_participants existing
    WHERE existing.attendance_id = a.id AND existing.collaborator_id = c.id
);

DROP TEMPORARY TABLE seed_participation, seed_collaborator_ids, seed_workshop_ids, seed_collaborators, seed_workshops;
COMMIT;
