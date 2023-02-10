CREATE or alter PROCEDURE Retry(@TSQLCode NVARCHAR(MAX), @RetryCount INT, @IntervalTime VARCHAR(8))
AS
BEGIN
    -- Declare variables to store the number of retries and interval time
    DECLARE @retry INT = @RetryCount;
    DECLARE @interval VARCHAR(8) = @IntervalTime;

    -- While the number of retries is greater than 0
    WHILE (@retry > 0)
        BEGIN
            BEGIN TRY
                -- Execute the specified T-SQL code
                EXEC sp_executesql @TSQLCode;

                -- Set the number of retries to 0 to break out of the loop
                SET @retry = 0;
            END TRY
            BEGIN CATCH
                -- Wait for the specified interval time before retrying
                WAITFOR DELAY @interval;


                print @retry
                -- Decrement the number of retries by 1
                SET @retry -= 1;

                -- If the number of retries has reached 0, throw the original exception
                IF (@retry <= 0)
                    BEGIN
                        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                        raiserror (@ErrorMessage,16,1);
                    END
            END CATCH
        END
END
