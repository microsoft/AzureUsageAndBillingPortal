/*
	Use the following SQL script to create required tables, stored procedures etc. 
	on your SQL Server.

	You should run the below SQL script after you create the required services 
	(i.e. SQL Server) with the "CreateAzureServicesScript.ps1" powershell script 
	in the solution folder.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

--DROP TABLE [dbo].[AzureUsageRecords];
--GO

CREATE TABLE [dbo].[AzureUsageRecords] (
    [uid]                UNIQUEIDENTIFIER NOT NULL,
    [id]                 NVARCHAR (150)   NULL,
    [name]               NVARCHAR (150)   NULL,
    [type]               NVARCHAR (150)   NULL,
    [subscriptionId]     NVARCHAR (50)    NULL,
    [usageStartTime]     DATETIME         NULL,
    [usageEndTime]       DATETIME         NULL,
    [meterId]            NVARCHAR (50)    NULL,
    [meteredRegion]      NVARCHAR (50)    NULL,
    [meteredService]     NVARCHAR (100)   NULL,
    [project]            NVARCHAR (1000)  NULL,
    [meteredServiceType] NVARCHAR (150)   NULL,
    [serviceInfo1]       NVARCHAR (100)   NULL,
    [instanceDataRaw]    NVARCHAR (1000)  NULL,
    [resourceUri]        NVARCHAR (250)   NULL,
    [location]           NVARCHAR (100)   NULL,
    [partNumber]         NVARCHAR (150)   NULL,
    [orderNumber]        NVARCHAR (150)   NULL,
    [quantity]           FLOAT (53)       NULL,
    [unit]               NVARCHAR (50)    NULL,
    [meterName]          NVARCHAR (150)   NULL,
    [meterCategory]      NVARCHAR (150)   NULL,
    [meterSubCategory]   NVARCHAR (150)   NULL,
    [meterRegion]        NVARCHAR (100)   NULL,
	[cost]				 DECIMAL (19, 4)  NULL
    CONSTRAINT [PK_AzureUsageRecords] PRIMARY KEY CLUSTERED ([uid] ASC)
);

GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_EndTime]
    ON [dbo].[AzureUsageRecords]([usageEndTime] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_meterCategory]
    ON [dbo].[AzureUsageRecords]([meterCategory] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_meterSubCategory]
    ON [dbo].[AzureUsageRecords]([meterSubCategory] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_StartTime]
    ON [dbo].[AzureUsageRecords]([usageStartTime] ASC);

GO
CREATE INDEX [IX_AzureUsageRecords_subscriptionId] ON [dbo].[AzureUsageRecords] ([subscriptionId])

GO


--DROP PROCEDURE [dbo].[InsertUsageRecord]
--GO

CREATE PROCEDURE  InsertUsageRecord
    @uid                UNIQUEIDENTIFIER ,
    @id                 NVARCHAR (150)   ,
    @name               NVARCHAR (150)   ,
    @type               NVARCHAR (150)   ,
    @subscriptionId     NVARCHAR (50)    ,
    @usageStartTime     DATETIME         ,
    @usageEndTime       DATETIME         ,
    @meterId            NVARCHAR (50)    ,
    @meteredRegion      NVARCHAR (50)    ,
    @meteredService     NVARCHAR (100)   ,
    @project            NVARCHAR (1000)  ,
    @meteredServiceType NVARCHAR (150)   ,
    @serviceInfo1       NVARCHAR (100)   ,
    @instanceDataRaw    NVARCHAR (1000)  ,
    @resourceUri        NVARCHAR (250)   ,
    @location           NVARCHAR (100)   ,
    @partNumber         NVARCHAR (150)   ,
    @orderNumber        NVARCHAR (150)   ,
    @quantity           FLOAT       ,
    @unit               NVARCHAR (50)    ,
    @meterName          NVARCHAR (150)   ,
    @meterCategory      NVARCHAR (150)   ,
    @meterSubCategory   NVARCHAR (150)   ,
    @meterRegion        NVARCHAR (100)   ,
	@cost				DECIMAL (19, 4)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @CNT INT

	SELECT @CNT = COUNT(*) FROM [dbo].[AzureUsageRecords] WHERE
			(--@id					= [id]
			--AND @name				= [name]
			@type					= [type]
			AND @subscriptionId		= [subscriptionId]
			AND @usageStartTime		= [usageStartTime]
			AND @usageEndTime		= [usageEndTime]
			AND @meterId			= [meterId]
			AND @meteredRegion		= [meteredRegion]
			AND @meteredService		= [meteredService]
			AND @project			= [project]
			AND @meteredServiceType	= [meteredServiceType]
			AND @serviceInfo1		= [serviceInfo1]
			AND @instanceDataRaw	= [instanceDataRaw]
			AND @resourceUri		= [resourceUri]
			AND @location			= [location]
			AND @partNumber			= [partNumber]
			AND @orderNumber		= [orderNumber]
			AND @quantity			= [quantity]
			AND @unit				= [unit]
			AND @meterName			= [meterName]
			AND @meterCategory		= [meterCategory]
			AND @meterSubCategory	= [meterSubCategory]
			AND @meterRegion		= [meterRegion]
			AND @cost				= [cost])
	
	IF ( @CNT > 0) 
	BEGIN
	  RETURN @CNT
	END

	INSERT INTO [dbo].[AzureUsageRecords]
			   ([uid]
			   ,[id]
			   ,[name]
			   ,[type]
			   ,[subscriptionId]
			   ,[usageStartTime]
			   ,[usageEndTime]
			   ,[meterId]
			   ,[meteredRegion]
			   ,[meteredService]
			   ,[project]
			   ,[meteredServiceType]
			   ,[serviceInfo1]
			   ,[instanceDataRaw]
			   ,[resourceUri]
			   ,[location]
			   ,[partNumber]
			   ,[orderNumber]
			   ,[quantity]
			   ,[unit]
			   ,[meterName]
			   ,[meterCategory]
			   ,[meterSubCategory]
			   ,[meterRegion]
			   ,[cost])
		 VALUES
			   (@uid
			   ,@id
			   ,@name
			   ,@type
			   ,@subscriptionId
			   ,@usageStartTime
			   ,@usageEndTime
			   ,@meterId
			   ,@meteredRegion
			   ,@meteredService
			   ,@project
			   ,@meteredServiceType
			   ,@serviceInfo1
			   ,@instanceDataRaw
			   ,@resourceUri
			   ,@location
			   ,@partNumber
			   ,@orderNumber
			   ,@quantity
			   ,@unit
			   ,@meterName
			   ,@meterCategory
			   ,@meterSubCategory
			   ,@meterRegion
			   ,@cost)
END