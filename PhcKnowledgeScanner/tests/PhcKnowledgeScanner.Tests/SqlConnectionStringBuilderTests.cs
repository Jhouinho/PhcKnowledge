using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Collectors.Sql;

namespace PhcKnowledgeScanner.Tests;

public class SqlConnectionStringBuilderTests
{
    [Fact]
    public void Build_WindowsAuth_IsReadOnly_AndHasNoPassword()
    {
        var cs = SqlConnectionStringBuilderService.Build(new SqlConnectionRequest
        {
            Server = "localhost",
            Database = "Demo",
            AuthenticationMode = SqlAuthenticationMode.Windows
        });

        var builder = new SqlConnectionStringBuilder(cs);
        Assert.Equal(ApplicationIntent.ReadOnly, builder.ApplicationIntent);
        Assert.True(builder.IntegratedSecurity);
        Assert.True(string.IsNullOrEmpty(builder.Password));
    }

    [Fact]
    public void ToLogSafe_DoesNotIncludePassword()
    {
        var text = SqlConnectionStringBuilderService.ToLogSafe(new SqlConnectionRequest
        {
            Server = "srv",
            Database = "db",
            AuthenticationMode = SqlAuthenticationMode.SqlPassword,
            UserName = "sa",
            Password = "secret-value"
        });

        Assert.DoesNotContain("secret-value", text, StringComparison.Ordinal);
    }
}
