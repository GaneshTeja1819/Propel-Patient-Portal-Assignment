CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE audit_logs (
        "Id" uuid NOT NULL,
        "ActorId" uuid,
        "ActorEmail" character varying(256) NOT NULL,
        "Action" character varying(256) NOT NULL,
        "EntityType" character varying(256) NOT NULL,
        "EntityId" uuid,
        "Details" text,
        "IpAddress" character varying(45),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE insurance_records (
        "Id" uuid NOT NULL,
        "ProviderName" character varying(256) NOT NULL,
        "InsuranceIdPattern" character varying(512) NOT NULL,
        "PlanName" character varying(256),
        "ContactPhone" character varying(30),
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_insurance_records" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL,
        "Email" character varying(256) NOT NULL,
        "PasswordHash" character varying(512) NOT NULL,
        "Role" character varying(50) NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "PhoneNumber" character varying(30),
        "IsEmailVerified" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE appointment_slots (
        "Id" uuid NOT NULL,
        "ProviderId" uuid NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone NOT NULL,
        "IsAvailable" boolean NOT NULL,
        "DurationMinutes" integer NOT NULL,
        "RecurrencePattern" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_appointment_slots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_appointment_slots_users_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE calendar_syncs (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Provider" text NOT NULL,
        "EncryptedAccessToken" text NOT NULL,
        "EncryptedRefreshToken" text NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "LastSyncedAt" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_calendar_syncs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_calendar_syncs_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE data_conflicts (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "FieldName" text NOT NULL,
        "SourceValue" text NOT NULL,
        "TargetValue" text NOT NULL,
        "IsResolved" boolean NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_data_conflicts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_data_conflicts_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE notifications (
        "Id" uuid NOT NULL,
        "RecipientId" uuid NOT NULL,
        "Type" text NOT NULL,
        "Title" text NOT NULL,
        "Body" text NOT NULL,
        "IsRead" boolean NOT NULL,
        "SentAt" timestamp with time zone,
        "ScheduledFor" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_notifications_users_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE patient_profiles_360 (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "EncryptedSummaryJson" text NOT NULL,
        "LastUpdatedAt" timestamp with time zone NOT NULL,
        "ConflictCount" integer NOT NULL,
        CONSTRAINT "PK_patient_profiles_360" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_patient_profiles_360_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE waitlist_entries (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "ProviderId" uuid,
        "RequestedDate" timestamp with time zone NOT NULL,
        "PreferredTimeRange" text,
        "Status" text NOT NULL,
        "NotifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_waitlist_entries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_waitlist_entries_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_waitlist_entries_users_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE appointments (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "ProviderId" uuid NOT NULL,
        "SlotId" uuid NOT NULL,
        "Status" text NOT NULL,
        "Notes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_appointments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_appointments_appointment_slots_SlotId" FOREIGN KEY ("SlotId") REFERENCES appointment_slots ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_appointments_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_appointments_users_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE clinical_documents (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "ProviderId" uuid,
        "AppointmentId" uuid,
        "StoragePath" text NOT NULL,
        "DocumentType" text NOT NULL,
        "FileName" text NOT NULL,
        "MimeType" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "IsProcessed" boolean NOT NULL,
        CONSTRAINT "PK_clinical_documents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_clinical_documents_appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES appointments ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_clinical_documents_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_clinical_documents_users_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE intake_records (
        "Id" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "AppointmentId" uuid,
        "EncryptedFormData" text NOT NULL,
        "FormType" text NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        "IsReviewed" boolean NOT NULL,
        CONSTRAINT "PK_intake_records" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_intake_records_appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES appointments ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_intake_records_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE extracted_clinical_data (
        "Id" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "EncryptedExtractedJson" text NOT NULL,
        "ExtractionModel" text NOT NULL,
        "ExtractedAt" timestamp with time zone NOT NULL,
        "ConfidenceScore" double precision,
        CONSTRAINT "PK_extracted_clinical_data" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_extracted_clinical_data_clinical_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES clinical_documents ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_extracted_clinical_data_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE medical_code_suggestions (
        "Id" uuid NOT NULL,
        "ClinicalDataId" uuid NOT NULL,
        "PatientId" uuid NOT NULL,
        "CodeSystem" text NOT NULL,
        "SuggestedCode" text NOT NULL,
        "Description" text NOT NULL,
        "ConfidenceScore" double precision NOT NULL,
        "IsVerified" boolean NOT NULL,
        "SuggestedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_medical_code_suggestions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_medical_code_suggestions_extracted_clinical_data_ClinicalDa~" FOREIGN KEY ("ClinicalDataId") REFERENCES extracted_clinical_data ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_medical_code_suggestions_users_PatientId" FOREIGN KEY ("PatientId") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE TABLE verified_medical_codes (
        "Id" uuid NOT NULL,
        "SuggestionId" uuid NOT NULL,
        "VerifiedById" uuid NOT NULL,
        "CodeSystem" text NOT NULL,
        "Code" text NOT NULL,
        "Description" text NOT NULL,
        "VerifiedAt" timestamp with time zone NOT NULL,
        "Notes" text,
        CONSTRAINT "PK_verified_medical_codes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_verified_medical_codes_medical_code_suggestions_SuggestionId" FOREIGN KEY ("SuggestionId") REFERENCES medical_code_suggestions ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_verified_medical_codes_users_VerifiedById" FOREIGN KEY ("VerifiedById") REFERENCES users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    INSERT INTO insurance_records ("Id", "ContactPhone", "InsuranceIdPattern", "IsActive", "PlanName", "ProviderName")
    VALUES ('11111111-0000-0000-0000-000000000001', '1-800-262-2583', '^BCBS\d{9}$', TRUE, 'PPO Preferred', 'Blue Cross Blue Shield');
    INSERT INTO insurance_records ("Id", "ContactPhone", "InsuranceIdPattern", "IsActive", "PlanName", "ProviderName")
    VALUES ('11111111-0000-0000-0000-000000000002', '1-800-872-3862', '^AET\d{10}$', TRUE, 'Aetna Choice POS II', 'Aetna');
    INSERT INTO insurance_records ("Id", "ContactPhone", "InsuranceIdPattern", "IsActive", "PlanName", "ProviderName")
    VALUES ('11111111-0000-0000-0000-000000000003', '1-866-844-4864', '^UHC[A-Z]\d{8}$', TRUE, 'UnitedHealthcare Choice Plus', 'United Healthcare');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_appointment_slots_ProviderId" ON appointment_slots ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_appointments_PatientId" ON appointments ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_appointments_ProviderId" ON appointments ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_appointments_SlotId" ON appointments ("SlotId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_calendar_syncs_UserId" ON calendar_syncs ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_clinical_documents_AppointmentId" ON clinical_documents ("AppointmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_clinical_documents_PatientId" ON clinical_documents ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_clinical_documents_ProviderId" ON clinical_documents ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_data_conflicts_PatientId" ON data_conflicts ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_extracted_clinical_data_DocumentId" ON extracted_clinical_data ("DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_extracted_clinical_data_PatientId" ON extracted_clinical_data ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_intake_records_AppointmentId" ON intake_records ("AppointmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_intake_records_PatientId" ON intake_records ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_medical_code_suggestions_ClinicalDataId" ON medical_code_suggestions ("ClinicalDataId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_medical_code_suggestions_PatientId" ON medical_code_suggestions ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_notifications_RecipientId" ON notifications ("RecipientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_patient_profiles_360_PatientId" ON patient_profiles_360 ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_verified_medical_codes_SuggestionId" ON verified_medical_codes ("SuggestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_verified_medical_codes_VerifiedById" ON verified_medical_codes ("VerifiedById");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_waitlist_entries_PatientId" ON waitlist_entries ("PatientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    CREATE INDEX "IX_waitlist_entries_ProviderId" ON waitlist_entries ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517145221_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260517145221_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

