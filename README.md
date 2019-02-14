# EF Core Principal<->Dependant IDs

- [ ] Fix the repository name (on GitHub, generated namespaces etc.)

This repository explores how EF Core handles principal-dependent relationships
and the navigation properties related to those by convention.

We're going to demonstrate a 1:1 association between two entities and observe
EF Core manage the IDs of both ends for us. Then we are going to change them and
watch the IDs stay in sync. Next we are going to try to forcefully set incorrect
IDs and will observe EF Core push back on that idea.

- [ ] Do the above

Let's create a new .NET Core application and install the EF Core package to it,
including the SQL Server provider support, because we are going to be using
LocalDB.

Note that I am installing the NuGet packages from the official NuGet source and
in order to obtain consistent results, you should disable other package sources
in your Visual Studio > Tools > Options > NuGet Package Manager > Package
Sources option. It is enough to uncheck them while installing the packages
below.

```powershell
dotnet new console
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Now that we have the application set up, we also need to set up the LocalDB
database. We're going to call it by the name of the application namespace, which
works out to be `ef_core_principal_dependent_ids` since the repository directory
name where we've run `dotnet new` is called `ef-core-principal-dependent-ids`.
By doing this we can use `nameof` in the source code in the connection string.

```powershell
sqllocaldb create ef_core_principal_dependent_ids -s
```

Note that the `-s` switch automatically starts the instance after creation.

We are not going to be adding a dependency on
`Microsoft.EntityFrameworkCore.Design` as we do not need the `dotnet ef` tool,
we are not going to use EF Core Migrations.

Let's check everything compiles: `dotnet run`.

Now that we have the application up and running, we can move on to the entity
classes and the application database context setup.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; }
}

public class Car
{
    public int Id { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}
```

The model looks good, now for the application database context class and the
database connection:

```csharp
public class AppDbContext: DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer($@"Server=(localdb)\{nameof(ef_core_principal_dependent_ids)};Database={nameof(ef_core_principal_dependent_ids)};");
    }
}
```

To test this out, we will connect to the database and reset it by dropping it
and recreating it. This will help with repeatability of this demo.

```csharp
static void Main(string[] args)
{
    using (var appDbContext = new AppDbContext())
    {
        appDbContext.Database.EnsureDeleted();
        appDbContext.Database.EnsureCreated();
        Console.WriteLine("The database has been reset.");
    }
}
```

Running this (after adding `Microsoft.EntityFrameworkCore` namespace import),
we encounter this issue:

**The child/dependent side could not be determined for the one-to-one relationship between 'Car.User' and 'User.Car'.**

This makes sense, EF Core has no way of knowing which side of the 1:1
relationship is the principal and which one is the dependant.

Let's help it understand using the fluent API.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder
        .Entity<User>()
        .HasOne(principal => principal.Car)
        .WithOne(dependant => dependant.User)
        .HasForeignKey<Car>(car => car.UserId);
}
```

Now for the first scenarion - us adding a user and a car, and EF assigning the
correct IDs:

Whoops! It doesn't. I asked https://github.com/aspnet/EntityFrameworkCore/issues/14704.

I seem to have been mistaken remembering this working.
