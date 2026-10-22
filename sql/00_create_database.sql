/* Creates the empty TradingDemo database. Run as an admin connected to master.
   Azure SQL Database: skip this script and create the database in the portal. */

USE master;
GO

-- Contained database authentication lets 02_security.sql use CREATE USER ... WITH PASSWORD,
-- as on Azure SQL, so the remaining scripts are the same locally and in the cloud.
EXEC sp_configure 'contained database authentication', 1;
RECONFIGURE;
GO

IF DB_ID(N'TradingDemo') IS NULL
    CREATE DATABASE TradingDemo;
GO

ALTER DATABASE TradingDemo SET CONTAINMENT = PARTIAL;
GO

-- sys.sp_verify_database_ledger needs snapshot isolation. Azure SQL has it on already.
ALTER DATABASE TradingDemo SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
