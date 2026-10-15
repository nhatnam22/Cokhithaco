fail: CoKhiThaCo.Logging.LoggingInterceptor[0]
      [SVC] <- CommandCuttingFileService.ImportFromExcel FAIL 1189ms: DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
      Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
       ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Violation of PRIMARY KEY constraint 'PK_ProductionCommands'. Cannot insert duplicate key in object 'dbo.ProductionCommands'. The duplicate key value is (9a20e303-46fe-4552-a32b-3511f0beddc8).
      The statement has been terminated.
         at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
         at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObj


Vấn đề xảy ra: Tôi đang bị lỗi ở chức năng import dữ liệu CommandCuttingFile, 