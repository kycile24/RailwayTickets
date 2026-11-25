using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;

namespace RailwayTickets.Services;

public sealed class AtomicWrite(RailwayDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = context.HttpContext.User;
        string actor = user.Identity?.IsAuthenticated == true
            ? $"{user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value} #{user.AccountId()}"
            : "Відвідувач";
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sys.sp_set_session_context @key=N'RailwayActor', @value={actor}");
        var result = await next();
        if (result.Exception == null)
        {
            await transaction.CommitAsync();
            return;
        }
        if (result.Exception.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 or 547 } or Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 or 19 })
        {
            result.ExceptionHandled = true;
            result.Result = new ContentResult { StatusCode = 409, ContentType = "text/plain; charset=utf-8",
                Content = "Операцію не виконано. Дані змінилися, місце зайняте або запис має залежності. Поверніться до форми й оновіть дані." };
        }
    }
}
