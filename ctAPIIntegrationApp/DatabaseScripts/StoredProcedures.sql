SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE xct_CustomProc
	@param1 varChar(255),
	@param2 varChar(255)
AS
BEGIN
	SET NOCOUNT ON;

	-- NOTE:  I have 2 parameters coming in.  you can create as many as you want to use in your queries
	-- Also note that I dont know what you will be retrieving from SQL.  So, I am just making this up

	SELECT *
	FROM APImport WITH(NOLOCK)

	-- I am only bringing back one table.  This will show up in the dataset (in c#) as returnDataSet.Tables[0]

END
GO
