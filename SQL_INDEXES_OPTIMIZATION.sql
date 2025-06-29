-- =====================================================
-- SQL INDEXES OPTIMIZATION FOR CATCHCORNERSTATS
-- =====================================================
-- This script creates optimized indexes for better query performance
-- Run this script on your SQL Server database

USE [CatchCorner.Common];
GO

-- =====================================================
-- CRITICAL INDEXES FOR LEAD TIME ANALYSIS
-- =====================================================

-- Index for lead time calculations (most critical)
CREATE NONCLUSTERED INDEX IX_Bookings_LeadTime_Analysis
ON [powerBI].[VW_Bookings] ([FacilityId], [CreatedDateUtc], [HappeningDate])
INCLUDE ([BookingNumber])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for date range filtering
CREATE NONCLUSTERED INDEX IX_Bookings_Date_Filters
ON [powerBI].[VW_Bookings] ([HappeningDate], [CreatedDateUtc])
INCLUDE ([BookingNumber], [FacilityId], [StartTime], [EndTime])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- INDEXES FOR BOOKING ANALYSIS
-- =====================================================

-- Index for day of week analysis
CREATE NONCLUSTERED INDEX IX_Bookings_DayOfWeek_Analysis
ON [powerBI].[VW_Bookings] ([HappeningDate])
INCLUDE ([BookingNumber], [FacilityId], [StartTime], [EndTime])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for start time analysis
CREATE NONCLUSTERED INDEX IX_Bookings_StartTime_Analysis
ON [powerBI].[VW_Bookings] ([HappeningDate], [StartTime])
INCLUDE ([BookingNumber], [FacilityId], [EndTime])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- INDEXES FOR ARENA LOOKUPS
-- =====================================================

-- Primary index for Arena joins
CREATE NONCLUSTERED INDEX IX_Arena_Facility_Lookup
ON [powerBI].[VW_Arena] ([FacilityId])
INCLUDE ([Sport], [Area], [Size], [Facility])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for sport filtering
CREATE NONCLUSTERED INDEX IX_Arena_Sport_Filter
ON [powerBI].[VW_Arena] ([Sport])
INCLUDE ([FacilityId], [Area], [Size], [Facility])
WHERE [Sport] IS NOT NULL
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for city filtering
CREATE NONCLUSTERED INDEX IX_Arena_City_Filter
ON [powerBI].[VW_Arena] ([Area])
INCLUDE ([FacilityId], [Sport], [Size], [Facility])
WHERE [Area] IS NOT NULL
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for rink size filtering
CREATE NONCLUSTERED INDEX IX_Arena_Size_Filter
ON [powerBI].[VW_Arena] ([Size])
INCLUDE ([FacilityId], [Sport], [Area], [Facility])
WHERE [Size] IS NOT NULL
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- COMPOSITE INDEXES FOR COMMON FILTER COMBINATIONS
-- =====================================================

-- Index for sport + city combinations
CREATE NONCLUSTERED INDEX IX_Arena_Sport_City_Composite
ON [powerBI].[VW_Arena] ([Sport], [Area])
INCLUDE ([FacilityId], [Size], [Facility])
WHERE [Sport] IS NOT NULL AND [Area] IS NOT NULL
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- Index for facility + sport combinations
CREATE NONCLUSTERED INDEX IX_Arena_Facility_Sport_Composite
ON [powerBI].[VW_Arena] ([Facility], [Sport])
INCLUDE ([FacilityId], [Area], [Size])
WHERE [Facility] IS NOT NULL AND [Sport] IS NOT NULL
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- INDEXES FOR MONTHLY REPORT ANALYSIS
-- =====================================================

-- Index for monthly grouping
CREATE NONCLUSTERED INDEX IX_Bookings_Monthly_Analysis
ON [powerBI].[VW_Bookings] ([FacilityId], [HappeningDate])
INCLUDE ([BookingNumber], [Facility])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- INDEXES FOR SPORT COMPARISON ANALYSIS
-- =====================================================

-- Index for sport comparison by city
CREATE NONCLUSTERED INDEX IX_Bookings_Sport_Comparison
ON [powerBI].[VW_Bookings] ([FacilityId], [HappeningDate])
INCLUDE ([BookingNumber])
WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
GO

-- =====================================================
-- STATISTICS UPDATES
-- =====================================================

-- Update statistics for better query optimization
UPDATE STATISTICS [powerBI].[VW_Bookings] WITH FULLSCAN;
UPDATE STATISTICS [powerBI].[VW_Arena] WITH FULLSCAN;
GO

-- =====================================================
-- INDEX USAGE MONITORING QUERIES
-- =====================================================

-- Query to monitor index usage
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.user_updates,
    ius.last_user_seek,
    ius.last_user_scan
FROM sys.dm_db_index_usage_stats ius
INNER JOIN sys.indexes i ON ius.object_id = i.object_id 
    AND ius.index_id = i.index_id
WHERE ius.database_id = DB_ID()
    AND OBJECT_NAME(i.object_id) IN ('VW_Bookings', 'VW_Arena')
ORDER BY (ius.user_seeks + ius.user_scans + ius.user_lookups) DESC;
GO

-- Query to find missing indexes
SELECT 
    dm_mid.database_id AS DatabaseID,
    dm_migs.avg_user_impact,
    dm_migs.last_user_seek,
    dm_mid.statement AS TableName,
    dm_mid.equality_columns,
    dm_mid.inequality_columns,
    dm_mid.included_columns,
    dm_migs.unique_compiles,
    dm_migs.user_seeks,
    dm_migs.avg_total_user_cost,
    dm_migs.avg_user_impact
FROM sys.dm_db_missing_index_group_stats dm_migs
INNER JOIN sys.dm_db_missing_index_groups dm_mig 
    ON dm_migs.group_handle = dm_mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details dm_mid 
    ON dm_mig.index_handle = dm_mid.index_handle
WHERE dm_mid.database_id = DB_ID()
    AND dm_mid.statement LIKE '%VW_Bookings%' 
    OR dm_mid.statement LIKE '%VW_Arena%'
ORDER BY dm_migs.avg_user_impact DESC;
GO

-- =====================================================
-- PERFORMANCE MONITORING VIEWS
-- =====================================================

-- Create a view for monitoring slow queries
CREATE VIEW [dbo].[v_SlowQueries] AS
SELECT 
    qs.sql_handle,
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 AS total_elapsed_time_seconds,
    qs.total_elapsed_time / qs.execution_count / 1000000.0 AS avg_elapsed_time_seconds,
    qs.total_logical_reads,
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    qs.total_physical_reads,
    qs.total_physical_reads / qs.execution_count AS avg_physical_reads,
    qs.total_worker_time,
    qs.total_worker_time / qs.execution_count AS avg_worker_time,
    qs.last_execution_time,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS statement_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count / 1000000.0 > 1.0  -- Queries taking more than 1 second
ORDER BY avg_elapsed_time_seconds DESC;
GO

-- =====================================================
-- MAINTENANCE PROCEDURES
-- =====================================================

-- Procedure to rebuild/reorganize indexes
CREATE PROCEDURE [dbo].[sp_MaintainIndexes]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TableName NVARCHAR(128);
    DECLARE @IndexName NVARCHAR(128);
    DECLARE @Fragmentation FLOAT;
    DECLARE @SQL NVARCHAR(MAX);
    
    DECLARE IndexCursor CURSOR FOR
    SELECT 
        OBJECT_NAME(ips.object_id) AS TableName,
        i.name AS IndexName,
        ips.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
    INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
    WHERE ips.avg_fragmentation_in_percent > 10
        AND OBJECT_NAME(ips.object_id) IN ('VW_Bookings', 'VW_Arena');
    
    OPEN IndexCursor;
    FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Fragmentation > 30
        BEGIN
            -- Rebuild index
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [powerBI].[' + @TableName + '] REBUILD WITH (ONLINE = ON)';
            EXEC sp_executesql @SQL;
            PRINT 'Rebuilt index: ' + @IndexName + ' on table: ' + @TableName;
        END
        ELSE
        BEGIN
            -- Reorganize index
            SET @SQL = 'ALTER INDEX [' + @IndexName + '] ON [powerBI].[' + @TableName + '] REORGANIZE';
            EXEC sp_executesql @SQL;
            PRINT 'Reorganized index: ' + @IndexName + ' on table: ' + @TableName;
        END
        
        FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;
    END
    
    CLOSE IndexCursor;
    DEALLOCATE IndexCursor;
    
    -- Update statistics
    UPDATE STATISTICS [powerBI].[VW_Bookings] WITH FULLSCAN;
    UPDATE STATISTICS [powerBI].[VW_Arena] WITH FULLSCAN;
    
    PRINT 'Index maintenance completed successfully.';
END;
GO

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify indexes were created successfully
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique,
    i.is_primary_key,
    STUFF((
        SELECT ', ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal > 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS KeyColumns,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS IncludedColumns
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('VW_Bookings', 'VW_Arena')
    AND i.name LIKE 'IX_%'
ORDER BY t.name, i.name;
GO

PRINT 'SQL Indexes Optimization script completed successfully!';
PRINT 'Remember to:';
PRINT '1. Monitor index usage with the provided queries';
PRINT '2. Schedule regular index maintenance with sp_MaintainIndexes';
PRINT '3. Update statistics regularly';
PRINT '4. Monitor slow queries with v_SlowQueries view'; 