/****** Object:  Table [dbo].[AzureUsageAdditionalInfo]    Script Date: 4/5/2017 11:40:24 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AzureUsageAdditionalInfo](
	[Uid] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_AzureUsageAdditionalInfo] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC,
	[Name] ASC
)
)

GO
/****** Object:  Table [dbo].[AzureUsageRecords]    Script Date: 4/5/2017 11:40:25 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AzureUsageRecords](
	[Uid] [uniqueidentifier] NOT NULL,
	[Id] [varchar](150) NOT NULL,
	[Name] [varchar](150) NOT NULL,
	[Type] [varchar](150) NOT NULL,
	[SubscriptionId] [uniqueidentifier] NULL,
	[UsageStartTime] [datetime2](0) NOT NULL,
	[UsageEndTime] [datetime2](0) NOT NULL,
	[MeterId] [varchar](50) NOT NULL,
	[MeteredRegion] [varchar](50) NOT NULL,
	[MeteredService] [varchar](100) NOT NULL,
	[Project] [varchar](1000) NOT NULL,
	[MeteredServiceType] [varchar](150) NOT NULL,
	[ServiceInfo1] [varchar](100) NOT NULL,
	[ResourceUri] [varchar](250) NOT NULL,
	[Location] [varchar](100) NOT NULL,
	[Tags] [nvarchar](max) NULL,
	[AdditionalInfo] [nvarchar](max) NULL,
	[PartNumber] [varchar](150) NOT NULL,
	[OrderNumber] [uniqueidentifier] NULL,
	[Quantity] [float] NOT NULL,
	[Unit] [varchar](50) NOT NULL,
	[MeterName] [varchar](150) NOT NULL,
	[MeterCategory] [varchar](150) NOT NULL,
	[MeterSubCategory] [varchar](150) NOT NULL,
	[MeterRegion] [varchar](100) NOT NULL,
	[Cost] [float] NOT NULL,
 CONSTRAINT [PK_dbo.AzureUsageRecords] PRIMARY KEY NONCLUSTERED 
(
	[Uid] ASC
)
)

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords]    Script Date: 4/5/2017 11:40:25 AM ******/
CREATE CLUSTERED INDEX [IX_AzureUsageRecords] ON [dbo].[AzureUsageRecords]
(
	[UsageStartTime] ASC,
	[SubscriptionId] ASC,
	[MeterCategory] ASC,
	[MeterSubCategory] ASC,
	[MeterName] ASC
)
GO
/****** Object:  Table [dbo].[AzureUsageTags]    Script Date: 4/5/2017 11:40:25 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AzureUsageTags](
	[Uid] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[Value] [nvarchar](512) NOT NULL,
 CONSTRAINT [PK_AzureUsageTags] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC,
	[Name] ASC
)
)

GO
/****** Object:  Table [dbo].[PerUserTokenCaches]    Script Date: 4/5/2017 11:40:25 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PerUserTokenCaches](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[WebUserUniqueId] [nvarchar](max) NULL,
	[CacheBits] [varbinary](max) NULL,
	[LastWrite] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_dbo.PerUserTokenCaches] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
)

GO
/****** Object:  Table [dbo].[ReportRequests]    Script Date: 4/5/2017 11:40:26 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReportRequests](
	[RepReqId] [uniqueidentifier] NOT NULL,
	[ReportDate] [datetime2](0) NOT NULL,
	[StartDate] [datetime2](0) NOT NULL,
	[EndDate] [datetime2](0) NOT NULL,
	[DetailedReport] [bit] NOT NULL,
	[DailyReport] [bit] NOT NULL,
	[Url] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo.ReportRequests] PRIMARY KEY CLUSTERED 
(
	[RepReqId] ASC
)
)

GO
/****** Object:  Table [dbo].[Reports]    Script Date: 4/5/2017 11:40:26 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reports](
	[ReportId] [uniqueidentifier] NOT NULL,
	[SubscriptionId] [uniqueidentifier] NULL,
	[OrganizationId] [uniqueidentifier] NULL,
	[ReportRequest_RepReqId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_dbo.Reports] PRIMARY KEY CLUSTERED 
(
	[ReportId] ASC
)
)

GO
/****** Object:  Table [dbo].[Subscriptions]    Script Date: 4/5/2017 11:40:27 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Subscriptions](
	[Id] [uniqueidentifier] NOT NULL,
	[DisplayName] [nvarchar](max) NULL,
	[OrganizationId] [uniqueidentifier] NULL,
	[IsConnected] [bit] NOT NULL,
	[ConnectedOn] [datetime2](0) NOT NULL,
	[ConnectedBy] [nvarchar](max) NULL,
	[AzureAccessNeedsToBeRepaired] [bit] NOT NULL,
	[DisplayTag] [nvarchar](max) NULL,
	[DataGenStatus] [int] NOT NULL,
	[DataGenDate] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_dbo.Subscriptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
)

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords_Category]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_Category] ON [dbo].[AzureUsageRecords]
(
	[MeterCategory] ASC,
	[MeterSubCategory] ASC
)
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords_Cost]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_Cost] ON [dbo].[AzureUsageRecords]
(
	[Cost] ASC
)
INCLUDE ( 	[Location],
	[MeterCategory],
	[MeterName],
	[MeterSubCategory],
	[Quantity],
	[ResourceUri],
	[SubscriptionId],
	[UsageStartTime])
GO
/****** Object:  Index [IX_AzureUsageRecords_EndTime]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_EndTime] ON [dbo].[AzureUsageRecords]
(
	[UsageEndTime] ASC
)
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords_Location]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_Location] ON [dbo].[AzureUsageRecords]
(
	[Location] ASC
)
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords_MeterName]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_MeterName] ON [dbo].[AzureUsageRecords]
(
	[MeterName] ASC
)
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_AzureUsageRecords_ResourceUri]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_ResourceUri] ON [dbo].[AzureUsageRecords]
(
	[ResourceUri] ASC
)
INCLUDE ( 	[SubscriptionId])
GO
/****** Object:  Index [IX_AzureUsageRecords_SubscriptionId]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_SubscriptionId] ON [dbo].[AzureUsageRecords]
(
	[SubscriptionId] ASC
)
GO
/****** Object:  Index [IX_ReportRequest_repReqID]    Script Date: 4/5/2017 11:40:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_ReportRequest_RepReqId] ON [dbo].[Reports]
(
	[ReportRequest_RepReqId] ASC
)
GO
ALTER TABLE [dbo].[AzureUsageAdditionalInfo]  WITH CHECK ADD  CONSTRAINT [FK_AzureUsageAdditionalInfo_AzureUsageRecords] FOREIGN KEY([Uid])
REFERENCES [dbo].[AzureUsageRecords] ([Uid])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AzureUsageAdditionalInfo] CHECK CONSTRAINT [FK_AzureUsageAdditionalInfo_AzureUsageRecords]
GO
ALTER TABLE [dbo].[AzureUsageTags]  WITH CHECK ADD  CONSTRAINT [AzureUsageTags_Uid_FK] FOREIGN KEY([Uid])
REFERENCES [dbo].[AzureUsageRecords] ([Uid])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AzureUsageTags] CHECK CONSTRAINT [AzureUsageTags_Uid_FK]
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Reports_dbo.ReportRequests_ReportRequest_RepReqId] FOREIGN KEY([ReportRequest_RepReqId])
REFERENCES [dbo].[ReportRequests] ([RepReqId])
GO
ALTER TABLE [dbo].[Reports] CHECK CONSTRAINT [FK_dbo.Reports_dbo.ReportRequests_ReportRequest_RepReqId]
GO
ALTER TABLE [dbo].[AzureUsageRecords]  WITH NOCHECK ADD  CONSTRAINT [CK_AzureUsageRecords_AdditionalInfo] CHECK  (([AdditionalInfo] IS NULL))
GO
ALTER TABLE [dbo].[AzureUsageRecords] CHECK CONSTRAINT [CK_AzureUsageRecords_AdditionalInfo]
GO
ALTER TABLE [dbo].[AzureUsageRecords]  WITH NOCHECK ADD  CONSTRAINT [CK_AzureUsageRecords_Tags] CHECK  (([Tags] IS NULL))
GO
ALTER TABLE [dbo].[AzureUsageRecords] CHECK CONSTRAINT [CK_AzureUsageRecords_Tags]
GO
/****** Object:  Trigger [dbo].[AzureUsageRecords_InsertTrigger]    Script Date: 4/5/2017 11:40:27 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE trigger [dbo].[AzureUsageRecords_InsertTrigger]
   on [dbo].[AzureUsageRecords]
   instead of insert
as

set nocount on;

insert into dbo.AzureUsageRecords
	-- insert all columns besides [Tags] and [AdditionalInfo]
	([Uid], Id, [Name], [Type], SubscriptionId, UsageStartTime, UsageEndTime, MeterId, MeteredRegion, MeteredService, Project, MeteredServiceType, ServiceInfo1, ResourceUri, [Location], PartNumber, OrderNumber, Quantity, Unit, MeterName, MeterCategory, MeterSubCategory, MeterRegion, Cost)
select [Uid], Id, [Name], [Type], SubscriptionId, UsageStartTime, UsageEndTime, MeterId, MeteredRegion, MeteredService, Project, MeteredServiceType, ServiceInfo1, ResourceUri, [Location], PartNumber, OrderNumber, Quantity, Unit, MeterName, MeterCategory, MeterSubCategory, MeterRegion, Cost 
from inserted

-- insert Tags
insert into dbo.AzureUsageTags
select i.[Uid], a.[Name], a.[Value]
from inserted i cross apply openjson(Tags) with ([Name] nvarchar(256), [Value] nvarchar(512)) a
where Tags is not null and Tags != ''

-- insert AdditionalInfo
insert into dbo.AzureUsageAdditionalInfo
select i.[Uid], a.[Name], a.[Value]
from inserted i cross apply openjson(AdditionalInfo) with ([Name] nvarchar(128), [Value] nvarchar(256)) a
where AdditionalInfo is not null and AdditionalInfo != ''
GO
ALTER TABLE [dbo].[AzureUsageRecords] ENABLE TRIGGER [AzureUsageRecords_InsertTrigger]
GO
