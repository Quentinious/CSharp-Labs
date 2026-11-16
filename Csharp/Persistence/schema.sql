CREATE TABLE IF NOT EXISTS semesters (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    status text NOT NULL CHECK (status IN ('active', 'completed', 'expelled')),
    next_day integer NOT NULL CHECK (next_day BETWEEN 0 AND 100),
    total_pleasure integer NOT NULL CHECK (total_pleasure >= 0),
    expelled boolean NOT NULL,
    random_state bigint NOT NULL,
    random_version integer NOT NULL DEFAULT 1 CHECK (random_version = 1),
    observation_days integer NOT NULL CHECK (observation_days >= 0),
    risk_threshold double precision NOT NULL CHECK (risk_threshold BETWEEN 0 AND 1),
    CHECK ((status = 'active' AND NOT expelled AND next_day < 100)
        OR (status = 'completed' AND NOT expelled AND next_day = 100)
        OR (status = 'expelled' AND expelled AND next_day > 0 AND total_pleasure = 0))
);

CREATE TABLE IF NOT EXISTS semester_teachers (
    semester_id uuid NOT NULL REFERENCES semesters(id) ON DELETE CASCADE,
    subject integer NOT NULL CHECK (subject BETWEEN 0 AND 5),
    position integer NOT NULL CHECK (position BETWEEN 0 AND 5),
    rule integer NOT NULL CHECK (rule BETWEEN 0 AND 2),
    depend_a integer NOT NULL CHECK (depend_a BETWEEN 0 AND 5),
    depend_b integer NOT NULL CHECK (depend_b BETWEEN 0 AND 5),
    PRIMARY KEY (semester_id, subject),
    UNIQUE (semester_id, position)
);

CREATE TABLE IF NOT EXISTS simulation_days (
    semester_id uuid NOT NULL REFERENCES semesters(id) ON DELETE CASCADE,
    day integer NOT NULL CHECK (day BETWEEN 0 AND 99),
    total_pleasure integer NOT NULL CHECK (total_pleasure >= 0),
    expelled boolean NOT NULL,
    CHECK (NOT expelled OR total_pleasure = 0),
    PRIMARY KEY (semester_id, day)
);

CREATE TABLE IF NOT EXISTS lesson_results (
    semester_id uuid NOT NULL,
    day integer NOT NULL,
    subject integer NOT NULL,
    attended boolean NOT NULL,
    was_actually_asked boolean NOT NULL,
    PRIMARY KEY (semester_id, day, subject),
    FOREIGN KEY (semester_id, day) REFERENCES simulation_days(semester_id, day) ON DELETE CASCADE,
    FOREIGN KEY (semester_id, subject) REFERENCES semester_teachers(semester_id, subject)
);

-- Composite primary keys above also index the leading foreign-key columns.
CREATE INDEX IF NOT EXISTS lesson_results_teacher_idx ON lesson_results(semester_id, subject);
