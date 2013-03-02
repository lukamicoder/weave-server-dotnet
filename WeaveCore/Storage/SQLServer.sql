USE [Weave]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[UserId] [bigint] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](32) NULL,
	[Md5] [nvarchar](128) NULL,
	[Email] [nvarchar](64) NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Wbos](
	[UserId] [bigint] NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Collection] [smallint] NOT NULL,
	[Modified] [float] NULL,
	[SortIndex] [bigint] NULL,
	[Payload] [ntext] NULL,
	[PayloadSize] [bigint] NULL,
	[Ttl] [float] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[Id] ASC,
	[Collection] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[Wbos]  WITH CHECK ADD  CONSTRAINT [Wbo_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([UserId])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Wbos] CHECK CONSTRAINT [Wbo_User]
GO

CREATE INDEX Index_UserId_Collection_Modified ON Wbos (UserId, Collection, Modified)
GO

CREATE INDEX Index_Ttl ON Wbos (Ttl)
GO