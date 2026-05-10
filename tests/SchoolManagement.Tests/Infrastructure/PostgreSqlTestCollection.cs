namespace SchoolManagement.Tests.Infrastructure;

[CollectionDefinition("PostgreSQL")]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlSchoolManagementApiFactory>
{
    public const string Name = "PostgreSQL";
}
