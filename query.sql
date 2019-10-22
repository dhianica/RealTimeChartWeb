USE [testing]
GO

/****** Object:  Table [dbo].[RealTimeChartWeb]    Script Date: 10/23/2019 3:39:34 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[RealTimeChartWeb](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[usage] [float] NOT NULL,
	[created_at] [datetime] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[RealTimeChartWeb] ADD  CONSTRAINT [DF_RealTimeChartWeb_created_at]  DEFAULT (getdate()) FOR [created_at]
GO


