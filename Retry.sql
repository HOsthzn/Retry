CREATE OR ALTER PROCEDURE Retry (@TSQLCode NVARCHAR(MAX), @RetryCount INT, @IntervalTime VARCHAR(8))
AS
BEGIN
    -- Declare a variable to keep track of the number of retries left
    DECLARE @retry INT = @RetryCount;

    -- Declare a variable to store the interval time between each retry
    DECLARE @interval VARCHAR(8) = @IntervalTime;

    -- Declare a variable to store the SQL code to be executed
    DECLARE @SQL NVARCHAR(MAX);

    -- Start a loop that will repeat the execution of the SQL code for the specified number of retries
    WHILE (@retry > 0)
    BEGIN
        BEGIN TRY
            -- Set the SQL variable to the given SQL code
            SET @SQL = N'EXEC sp_executesql @TSQLCode, N''@TSQLCode NVARCHAR(MAX)'', @TSQLCode';

            -- Execute the SQL code using sp_executesql
            EXEC sp_executesql @SQL, N'@TSQLCode NVARCHAR(MAX)', @TSQLCode;

            -- If the execution of the SQL code was successful, set the number of retries to 0 to exit the loop
            SET @retry = 0;
        END TRY
        BEGIN CATCH
            -- If the execution of the SQL code fails, wait for the specified interval time before retrying
            WAITFOR DELAY @interval;

            -- Print the number of retries left
            PRINT @retry;

            -- Decrement the number of retries left
            SET @retry -= 1;

            -- If there are no more retries left, raise an error with the error message
            IF (@retry <= 0)
            BEGIN
                DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                RAISERROR (@ErrorMessage, 16, 1);
            END
        END CATCH
    END
END
