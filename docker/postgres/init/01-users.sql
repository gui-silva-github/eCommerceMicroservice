-- UsersService schema (PostgreSQL / Dapper)
CREATE TABLE IF NOT EXISTS public."Users" (
    "UserID" uuid NOT NULL,
    "Email" varchar(255) NOT NULL,
    "PersonName" varchar(255) NULL,
    "Gender" varchar(50) NULL,
    "Password" varchar(255) NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("UserID"),
    CONSTRAINT "UQ_Users_Email" UNIQUE ("Email")
);

CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON public."Users" ("Email");

-- Seed admin used by the Angular frontend (email: admin@gmail.com / password: admin)
INSERT INTO public."Users" ("UserID", "Email", "PersonName", "Gender", "Password")
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'admin@gmail.com',
    'Administrador',
    'Masculino',
    'admin'
)
ON CONFLICT ("Email") DO NOTHING;
