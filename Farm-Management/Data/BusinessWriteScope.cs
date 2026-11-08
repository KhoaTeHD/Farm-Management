using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Farm_Management.Data;

public static class BusinessWriteScope
{
    // Serialize application writes across requests and app instances. A transaction alone
    // cannot protect check-then-insert rules once database UNIQUE/FK constraints are removed.
    // Future write endpoints must acquire this same scope before checking business rules.
    public static async Task<IDbContextTransaction> BeginBusinessWriteAsync(this ApplicationDbContext context)
    {
        var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = N'FarmManagement.BusinessWrite',
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;
                IF @result < 0 THROW 50001, N'Không thể lấy quyền cập nhật. Vui lòng thử lại.', 1;
                """);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
