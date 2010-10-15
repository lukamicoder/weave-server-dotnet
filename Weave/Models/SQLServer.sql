﻿GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[UserId] [bigint] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](32) NOT NULL,
	[Md5] [nvarchar](32) NOT NULL,
	CONSTRAINT [PK_Users_1] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Wbos](
	[UserId] [bigint] NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Collection] [smallint] NOT NULL,
	[ParentId] [nvarchar](64) NULL,
	[PredecessorId] [nvarchar](64) NULL,
	[Modified] [float] NULL,
	[SortIndex] [int] NULL,
	[Payload] [text] NULL,
	[PayloadSize] [int] NULL,
	CONSTRAINT [PK_Wbos] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[Collection] ASC,
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Wbos]  WITH CHECK ADD  CONSTRAINT [FK_Wbos_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([UserId])
GO
ALTER TABLE [dbo].[Wbos] CHECK CONSTRAINT [FK_Wbos_Users]
GO