-- Explicit development seed. Reuse matching names and workshop dates without overwriting user records.
SET NAMES utf8mb4;
START TRANSACTION;

CREATE TEMPORARY TABLE seed_workshops (
    seed_order INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100), held_at VARCHAR(33), description LONGTEXT
);
-- Fixed calendar: second Thursday of each quarter, 2022 through 2026, at 16:00 -03:00.
INSERT INTO seed_workshops (name, held_at, description) VALUES
('Fundamentos de Git', '2022-01-13T16:00:00.0000000-03:00', 'Versionamento, branches e colaboração em equipe.'),
('Comunicação em equipe', '2022-04-14T16:00:00.0000000-03:00', 'Escuta ativa, feedback e alinhamento de expectativas.'),
('Introdução ao SQL', '2022-07-14T16:00:00.0000000-03:00', 'Consultas, relacionamentos e organização de dados.'),
('Métodos ágeis', '2022-10-13T16:00:00.0000000-03:00', 'Planejamento iterativo e melhoria contínua de entregas.'),
('Testes automatizados', '2023-01-12T16:00:00.0000000-03:00', 'Testes repetíveis para proteger regras e contratos.'),
('Design de interfaces', '2023-04-13T16:00:00.0000000-03:00', 'Hierarquia visual e fluxos claros para usuários.'),
('Docker no desenvolvimento', '2023-07-13T16:00:00.0000000-03:00', 'Containers e ambientes locais reproduzíveis.'),
('Segurança de aplicações', '2023-10-12T16:00:00.0000000-03:00', 'Validação de entradas e proteção de informações sensíveis.'),
('Refatoração na prática', '2024-01-11T16:00:00.0000000-03:00', 'Pequenas melhorias de código apoiadas por testes.'),
('Acessibilidade na web', '2024-04-11T16:00:00.0000000-03:00', 'Navegação por teclado, semântica e contraste.'),
('Integração contínua', '2024-07-11T16:00:00.0000000-03:00', 'Pipelines de build, testes e validação automatizada.'),
('Modelagem de dados', '2024-10-10T16:00:00.0000000-03:00', 'Entidades, integridade referencial e evolução de schemas.'),
('Observabilidade', '2025-01-09T16:00:00.0000000-03:00', 'Logs estruturados e diagnóstico de falhas.'),
('TypeScript na prática', '2025-04-10T16:00:00.0000000-03:00', 'Tipos explícitos e modelagem de estados da interface.'),
('Desempenho de consultas', '2025-07-10T16:00:00.0000000-03:00', 'Índices, planos de execução e consultas eficientes.'),
('Revisão de código', '2025-10-09T16:00:00.0000000-03:00', 'Revisões objetivas e compartilhamento de conhecimento.'),
('Planejamento técnico', '2026-10-08T16:00:00.0000000-03:00', 'Decomposição de tarefas, estimativas e riscos de entrega.'),
('Clean Code', '2026-07-09T16:00:00.0000000-03:00', 'Práticas para escrever código legível, simples e sustentável.'),
('Angular na prática', '2026-04-09T16:00:00.0000000-03:00', 'Componentes, navegação e experiências acessíveis com Angular.'),
('APIs com ASP.NET Core', '2026-01-08T16:00:00.0000000-03:00', 'Contratos HTTP e boas práticas para construir APIs consistentes.');

CREATE TEMPORARY TABLE seed_collaborators (seed_order INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(100));
INSERT INTO seed_collaborators (name) VALUES
('Ana Souza'), ('Bruno Lima'), ('Carla Santos'), ('Diego Oliveira'),
('Eduarda Costa'), ('Felipe Almeida'), ('Gabriela Rocha'), ('Henrique Pereira'),
('Isabela Martins'), ('João Ribeiro'), ('Karina Gomes'), ('Lucas Carvalho'),
('Mariana Fernandes'), ('Natália Barbosa'), ('Otávio Mendes'), ('Paula Nascimento'),
('Rafael Araújo'), ('Sofia Cardoso'), ('Thiago Teixeira'), ('Vanessa Moreira'),
('André Correia'), ('Beatriz Dias'), ('Caio Batista'), ('Daniela Freitas'),
('Elisa Monteiro'), ('Fábio Castro'), ('Giovana Cunha'), ('Hugo Azevedo'),
('Júlia Nunes'), ('Leonardo Ramos');

INSERT INTO workshops (name, held_at, description)
SELECT s.name, s.held_at, s.description FROM seed_workshops s
WHERE NOT EXISTS (SELECT 1 FROM workshops w WHERE w.name = s.name AND w.held_at = s.held_at);

INSERT INTO collaborators (name)
SELECT s.name FROM seed_collaborators s
WHERE NOT EXISTS (SELECT 1 FROM collaborators c WHERE c.name = s.name);

CREATE TEMPORARY TABLE seed_workshop_ids AS
SELECT s.seed_order, MIN(w.id) AS id,
    CAST(MOD(CONV(LEFT(SHA2(CONCAT('workshop:', s.seed_order), 256), 8), 16, 10), 51) AS SIGNED) - 25 AS popularity
FROM seed_workshops s
JOIN workshops w ON w.name = s.name AND w.held_at = s.held_at GROUP BY s.seed_order;
CREATE TEMPORARY TABLE seed_collaborator_ids AS
SELECT s.seed_order, MIN(c.id) AS id,
    15 + MOD(CONV(LEFT(SHA2(CONCAT('collaborator:', s.seed_order), 256), 8), 16, 10), 71) AS frequency
FROM seed_collaborators s
JOIN collaborators c ON c.name = s.name GROUP BY s.seed_order;

INSERT INTO attendance_records (workshop_id)
SELECT s.id FROM seed_workshop_ids s
WHERE NOT EXISTS (SELECT 1 FROM attendance_records a WHERE a.workshop_id = s.id);

-- Stable pseudo-random profiles and draws avoid uniform totals without accumulating
-- new random attendees on every run. Seed positions keep the result independent of database IDs.
-- Frequency is 15..85, popularity is -25..25, and attendance probability is clamped to 5..95%.
INSERT INTO attendance_participants (attendance_id, collaborator_id)
SELECT a.id, c.id FROM seed_workshop_ids w
CROSS JOIN seed_collaborator_ids c
JOIN attendance_records a ON a.workshop_id = w.id
WHERE MOD(CONV(LEFT(SHA2(CONCAT('attendance:', w.seed_order, ':', c.seed_order), 256), 8), 16, 10), 100)
    < GREATEST(5, LEAST(95, c.frequency + w.popularity))
AND NOT EXISTS (
    SELECT 1 FROM attendance_participants existing
    WHERE existing.attendance_id = a.id AND existing.collaborator_id = c.id
);

DROP TEMPORARY TABLE seed_collaborator_ids, seed_workshop_ids, seed_collaborators, seed_workshops;
COMMIT;
