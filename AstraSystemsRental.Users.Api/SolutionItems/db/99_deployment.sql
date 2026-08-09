:on error exit

:r 00_create_database.sql
:r 03_schema_access.sql
:r 02_schema_subscriptions.sql
:r 01_schema_users.sql
:r 06_schema_companies.sql
:r 07_schema_plan_node_quotas.sql
:r 08_schema_company_invitations.sql
:r 04_indexes.sql
:r 05_seed.sql

PRINT 'AstraSystemsRental deployment completed.';
GO
