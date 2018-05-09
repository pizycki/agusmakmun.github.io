---
layout: post
title:  "Porting to Oracle with Entity Framework"
date:   2017-02-01
categories: [.NET, Oracle, EF]
---

## TL;DR: 

We had to port our giant-_ish_, Entity Framework based application to work on Oracle RDBMS (11g, later 12c). 

We did it. 

And we learned **a lot**.



## Boss comes around and...

*One day boss comes around and says:*

**Boss:** "We need our application to work on Oracle"

**Dev:** "Oracle? But we use MS SQL..."

**Boss:** <Here you can put any reason why you cannot use MS SQL. In our case it was clients lack of Oracle license.> Can you do it?

**Dev:** [thinking at loud] We use Entity Framework, which is somewhat abstraction on top of our database. I've heared that there are some providers **enabling work with Oracle**, but never used them. **I've seen** this **Microsoft** conference where **leading programmer was swapping** SQL Server provider with Oracle one by a **single line** and it **worked like a charm**, but it was template-basic application and... 

**Boss:** Great! We need this in 6 weeks.

**Dev:** But...

**Boss:** Inform me constantly. It's very important project and fail is not an option. Don't forget we need some time for tests. 2 weeks should be enough I guess. I gotta go now, I have another meeting. Thanks !

_* Sound of the closing door. *_

**Dev:** ...what just happened?



## Accept and advance

This is fromatted versions of all information gathered by 3 folks working on Oracle port for couple weeks.

I believe we wouldn't do it if would not cooperate and solve each problem **together**.

All problems we've encountered were put on our slack channel with short solution and decryption.

It was the best decision we made in whole project.



# Most common problems

>  **Note:** If you have troubles that are not covered in this article, here's some notes from Oracle. Some of the issues are described there as well as are here. **THOSE ARE THE MOST VALUABLE LINKS IN WHOLE ARTICLE.**
>
>  *  [Release 12.1.0.2.1 for ODAC 12c Release 3 Production](http://www.oracle.com/technetwork/topics/dotnet/downloads/odpnet-managed-nuget-121021-2405792.txt)
>  *  [Oracle.ManagedDataAccess NuGet Package 12.1.24160419 README](http://www.oracle.com/technetwork/topics/dotnet/downloads/odpnet-managed-nuget-121024160419-2999562.txt) 
>
>  If you google more you will find other resources on Oracle servers. Unfortunately, I discovered them  when I was near finishing writing whole document.
>
>  Those files can be found inside different ODP.NET nuget packages (those readme files you never read).
>
>  This is what happen when you don't read `readme` files.



## Which provider should I use?

We recommend using [Official Oracle ODP.NET](https://www.nuget.org/packages/Oracle.ManagedDataAccess.EntityFramework/). All cases presented here use this and **only** this provider. Version is `12.1.2400`.

```
Dependencies
EntityFramework (>= 6.0.0 && < 7.0.0)
Oracle.ManagedDataAccess (>= 12.1.2400 && < 12.2.0)
```



## Schema == User

This is the first thing you should learn about Oracle. In Oracle, there are no databases. There are **schemas** and **schemas are users**.

Do you want to create new database on your localhost like you did on MS SQL? **Create new user.** Its name is the name of your datab... I mean schema.

You can connect to your new schema with **new connection** in [Oracle SQL Developer](http://www.oracle.com/technetwork/developer-tools/sql-developer/downloads/index.html). Remember to assign to the created user `Connect` role (and maybe some others, I used to **grant it with all roles,** since I didn't have to deal with security issues).

>  **Note:** User `system` has `Other users`. Those are all users on your Oracle server. **You can access their database objects (dbo)** from `system` user level, without connecting to the user itself. It might come handy in scenarios when you often repeat drop/create users. 



## DBO names.Length <= 30

All **table names** in your schemas must contain **less or equal than 30 characters**.

To configure your entity to map to table with specified name use `ToTable(:string)` in your `EntityTypeConfiguration<>` derived class.

```csharp
public class FoobarEntityConfiguration : EntityTypeConfiguration<Foobar>
{
  public FoobarEntityConfiguration()
  {
    ToTable("Foobar");

    // More configuration here ...
  }
}
```

http://stackoverflow.com/a/756569/864968

I'd suggest creating convention test that checks every oracle table configuration defines table name no greater then 30 characters.



## EF logger

This is extremely helpful when a) you don't have Oracle license (which contains profiler and developer edition not) and b) you want to peek what's going on under the hood after your C# is magically transformed to some kind of SQL.

EF allows to log executed queries.

```csharp
private void EnableDebugLogs(DbContext context)
{
  context.Database.Log = s => Debug.WriteLine(s); // SQL writer
}
```

Logged queries can be found in `Output` window in Visual Studio. You can provide any action particularly.



## ORA-01918: user 'dbo' does not exist
You've created EF migration, run `Update-Database` and you get this error.

Remember what I was talking about **schemas and users? They're the same thing!**

In `DbContext` derived class specify schema name. It should be the same as the user you're connecting with database.

```csharp
public class FooDbContext : DbMigrationsConfiguration<FooDbContext>
{
  protected override void OnModelCreating(DbModelBuilder modelBuilder)
  {
    modelBuilder.HasDefaultSchema(" < Your connection string user here > ".ToUpper()); // Make sure it's upper case !
  }
}
```



## ORA-01005: null password given; logon denied

This might happen while you're running `Update-Database` and your `ConnectionString` does not contain `Persist Security Info`.

http://stackoverflow.com/questions/14810868/ora-01005-null-password-given-logon-denied



## Migration directory

If you, just like us, have to support both MS SQL and Oracle, you should consider seperate migrations set for each RDBMS. To do so, configure your migration directory in `DbMigrationConfiguration` derived class.

```csharp
public class FooMigrationConfiguration : DbMigrationsConfiguration<FooContext>
{
  public FooMigrationConfiguration()
  {
	DatabaseHelper.SetMigrationDirectory(this, " i.e. Contexts\Foo\Migrations  ");
  }
}

public static void SetMigrationDirectory<TContext>(DbMigrationsConfiguration<TContext> migration, string migrationsPath) where TContext : DbContext
{
  if (string.IsNullOrEmpty(migrationsPath))
  {
    throw new ArgumentException($"{nameof(migrationsPath)} cannot be null or empty.");
  }

  if (migrationsPath.Contains("/"))
    throw new ArgumentException($"Invalid {nameof(migrationsPath)}. Path should be valid Windows path. Use backslashes instead of slashes.");

  migration.MigrationsDirectory = migrationsPath;
}
```

> **Note:** Speaking aside, support for both RDBMS is like maintaining two **similar** but **different** applications. **They just differ.** It is very unlikely that you will end up with application working on both MS SQL and Oracle without any changes.
>
> EF is great tool, but don't expect it to do miracles. 



## NLog + Oracle

If you used to log exception errors directly to Database with help of [NLog](https://www.google.pl/search?q=NLOG) then you should also modify your `database target ` in your `nlog.config`.

```xml
<!--NLog configuration-->
<!--For more info visit: https://github.com/NLog/NLog website -->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xsi:schemaLocation="http://www.nlog-project.org/schemas/NLog.xsd NLog.xsd"
      autoReload="true"
      throwExceptions="true"
      internalLogLevel="Off"
      internalLogFile="App_Data\nlog-internal.log" >

  <targets>

    <!--Puts logs into database-->
    <target name="db" xsi:type="Database" commandType="Text" connectionStringName="FoobarDbContext">
      <commandText>
        insert into FOOBARSCHEMA."FOOBARTABLE"("Id", "Level", "Logger", "Message", "StackTrace", "Date", "TenantId", "UserId")
        values(sys_guid(), :LogLevel, :Logger, :Message, :StackTrace, systimestamp, :TenantId, :UserId)
      </commandText>

      <!--Reserved words by Oracle (don't use them as variable names: Level, Date-->
      <parameter name="LogLevel" layout="${level}"/>
      <parameter name="Logger" layout="${logger}"/>
      <parameter name="Message" layout="${message}"/>
      <parameter name="StackTrace" layout="${exception:format=Message,Type,Method,StackTrace,Data:separator=\r\n\r\n:maxInnerExceptionLevel=10:innerFormat=Message,Type,Method,StackTrace,Data:innerExceptionSeparator=\r\n\r\n}"/>
      <parameter name="TenantId" layout="${event-properties:item=TenantId}"/>
      <parameter name="UserId" layout="${event-properties:item=UserId}"/>

    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Warn" writeTo="trace,db" />
  </rules>

</nlog>
```



## Web.config

It's quite clear that we should maintain at least two versions of configuration file.

We decided to name them accordingly `Web.Oracle.config` and `Web.MSSQL.config`. It's clear and simple. During deploy the correct file is being selected and included into artifact.

You can also write watcher that detects `Web.config` change and accordingly replaces rest of `web.configs` in whole solution. You can do that like we did, with help of [Cake Watch](https://github.com/cake-addin/cake-watch).  



### External `app.settings` section

It is a good practice to have separate config file with common settings for both MS SQL and Oracle version. We can exclude them by adding `file` attribute.

```xml
<appSettings file="AppSettings.config" />
```



### Connection strings

Connections string differ so much that should be kept separately.

```xml
<connectionStrings>   
  
  <add name="ApplicationDbContext"
       providerName="Oracle.ManagedDataAccess.Client"
       connectionString="User Id=ApplicationDbContext;Password=P4S5W0RD;Data Source=OracleDataSource;Persist Security Info=true" />

</connectionStrings>

<entityFramework>
  <defaultConnectionFactory type="Oracle.ManagedDataAccess.EntityFramework.OracleConnectionFactory, Oracle.ManagedDataAccess.EntityFramework" />
  
  <providers>
    <provider invariantName="Oracle.ManagedDataAccess.Client"
              type="Oracle.ManagedDataAccess.EntityFramework.EFOracleProviderServices, Oracle.ManagedDataAccess.EntityFramework, Version=6.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
  </providers>
</entityFramework>

<system.data>
  <DbProviderFactories>
    <remove invariant="Oracle.ManagedDataAccess.Client" />
    <add name="ODP.NET, Managed Driver"
         invariant="Oracle.ManagedDataAccess.Client"
         description="Oracle Data Provider for .NET, Managed Driver"
         type="Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
  </DbProviderFactories>
</system.data>

<oracle.manageddataaccess.client>
  <version number="*">
  <dataSources>
  <dataSource alias="OracleDataSource"
              descriptor="(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORC)))" />
  </dataSources>
  </version>
</oracle.manageddataaccess.client>
```



## ODP.NET GAC (Global Assembly Cache)

### Uninstall

Sometime you might get following error

```
Could not load type 'OracleInternal.Common.ConfigBaseClass' from assembly 'Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342'.
```

This is not clear to me. Sometime I have to uninstall it, sometime I have to install it (due the error suggests something opposite).

I presume you have `gacutil` in your `PATH`. To check out, simply type `gacutil /?` to see available parameters. If you don't, [google for gacutil location](https://www.google.pl/search?q=gacutil+location) and [add it to your system variable PATH](https://www.google.pl/search?q=add+path+windows), or use `gacutil` with absolute path. Remember to open command window in Administrator mode.

```
gacutil /u Oracle.ManagedDataAccess
```

```
C:\...\packages\Oracle.ManagedDataAccess.12.1.2400\bin\x64
λ gacutil /u Oracle.ManagedDataAccess
Microsoft (R) .NET Global Assembly Cache Utility.  Version 4.0.30319.33440
Copyright (c) Microsoft Corporation.  All rights reserved.


Assembly: Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342, processorArchitec
ture=MSIL
Uninstalled: Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342, processorArchi
tecture=MSIL
Number of assemblies uninstalled = 1
Number of failures = 0
```

After that, reset IIS with `iisreset` to reload loaded assemblies and reload the page.

ref: [Oracle .Net ManagedDataAccess Error: Could not load type 'OracleInternal.Common.ConfigBaseClass' from assembly](http://stackoverflow.com/questions/30407213/oracle-net-manageddataaccess-error-could-not-load-type-oracleinternal-common)



### Install

```
Type is not resolved for member 'Oracle.ManagedDataAccess.Client.OracleException,Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342'.
```

Generally speaking all you got to do is to install `Oracle.ManagedDataAccess.dll` to your GAC (Global Assembly Cache).

```bash
gacutil /i <dll_path>
```

`Oracle.ManagedDataAccess.dll` can be found in your nuget package lib directory (mine was `.\packages\Oracle.ManagedDataAccess.12.1.2400\lib\net40`).

```cmd
C:\...\packages\Oracle.ManagedDataAccess.12.1.2400\lib\net40> gacutil /i .\Oracle.ManagedDataAccess.dll
Microsoft (R) .NET Global Assembly Cache Utility.  Version 4.0.30319.33440
Copyright (c) Microsoft Corporation.  All rights reserved.

Assembly successfully added to the cache
```

ref: [Entity Framework Seed method exception](http://stackoverflow.com/questions/32006884/entity-framework-seed-method-exception)



### Bounty

Install and uninstall enable further work, but what really would do the trick is a proper configuration that resolves constant installing and uninstalling assemblies from cache. So if come up with any better solution, feel free to share.



## Identity

In both 11g and 12c, GUID Identity columns in migration files must be replaced from 

```csharp
Id = c.Guid(nullable: false, identity: true),
```

to

```csharp
Id = c.Guid(nullable: false, identity: false, defaultValueSql: "SYS_GUID()"),
```

This is for both `11g` and `12c`



### Identity in 11g

`11g` does not offer autoincrement + uniquness feature (commonly known as `Identity` in MS SQL). EF handles generating next Identity values by incrementing [sequence]() by a [trigger](). Sequences are good for numeric column types, but they don't work well with GUIDs.

Change generated triggers to insert `SYS_GUID()` (which is `NEWID()` equivalent in MS SQL)  or change C# migration.



## Apply

```
"Oracle 11.2.0.2.0 does not support APPLY" exception
```

Error says everything. More robust EF queries are not supported on `11g`. You can rewrite your query, but it's just workaround, not a solution for this particular problem. Apply are supported from version `12c`. This is the main reason we moved from `11g` to `12c`. Fortunately, client was moving to `12c` as well.

ref: [ODAC 11.2 Release 4 (11.2.0.3.0) throwing “Oracle 11.2.0.2.0 does not support APPLY” exception](http://stackoverflow.com/questions/8892515/odac-11-2-release-4-11-2-0-3-0-throwing-oracle-11-2-0-2-0-does-not-support-ap)



## Bulk inserts

Bulk inserts are possible with ODP.NET Unmanaged Driver.



## Isolation levels

This one is quite important. In EF we can create transaction with specified **Isolation level**. Those differ in MS SQL and Oracle.



MS SQL 2014 isolation levels

- `READ UNCOMMITTED`
- `READ COMMITTED`
- `REPEATABLE READ`
- `SNAPSHOT`
- `SERIALIZABLE`




Oracle 11g/12c isolation levels

- `READ UNCOMMITTED`
- `READ COMMITTED`
- `REPEATABLE READ`
- `SERIALIZABLE`



I'm no Oracle expert, so if you want gain more knowledge about those, check out this awesome [blog post](http://www.dba-in-exile.com/2012/11/isolation-levels-in-oracle-vs-sql-server.html). It helped me a lot in understanding differences in isolation levels between Oracle and MS SQL.



## High Oracle Memory usage

[High RAM Memory Consumed by Oracle 11g for Windows Server 2008](http://windows.ittoolbox.com/groups/technical-functional/windows-server2008-l/high-ram-memory-consumed-by-oracle-11g-for-windows-server-2008-4928149)



## PL/SQL tips

`clear screen;`

`SET FEEDBACK OFF;`

`/` and `;` [When do I need to use a semicolon vs a slash in Oracle SQL?](http://stackoverflow.com/questions/1079949/when-do-i-need-to-use-a-semicolon-vs-a-slash-in-oracle-sql)

`commit;` commits changes on database. **You have to put this after every inserting section in your script.**



## SID vs SERVICE_NAME 

[How SID is different from Service name in Oracle tnsnames.ora](http://stackoverflow.com/questions/43866/how-sid-is-different-from-service-name-in-oracle-tnsnames-ora)



## Quartz.NET + Oracle

Some configuration need to be done before your scheduler will run on Oracle.

### jobStore.driverDelegateType

```
properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.OracleDelegate, Quartz";
```

http://stackoverflow.com/a/20343752/864968



### dataSource.default.provider

```
["quartz.dataSource.default.provider"] = "OracleODPManaged-1211-40",
```

http://www.quartz-scheduler.net/documentation/quartz-2.x/tutorial/job-stores.html



## Migrations

It is highly possible that you're gonna mess around with migrations, DbContexts and entities. You might end up with changes in your model that will need new migration.

We had one case when after adding a migration (for MSSQL) we got a lot of table creates like this migration was the very first, but it wasn't. 

To avoid such issues with your model changes (when you're absolutly sure there were none, like any table definition changes), **add `ContextKey` and `MigrationsNamespace` to your `MigrationConfiguration`**.

`ContextKey` is stored in `__MigrationHistory` table, created by EF migrator during first successful migration.

`MigrationsNamespace` is a bit different. It will appear in C# migrations. During migration on database those migrations transformed to xml (`edmx`), gzipped, compressed with "base64" and finally stored in `__MigrationHistory` table. **This will cause differences in model**.

Also, correct namespace is crucial while looking for proper migrations during EF migrations scan.

 

Compressed model can be easly decompressed. Check this link for more.

[Can I get decode an EntityFramework Model from a specified migration?](http://stackoverflow.com/questions/15709849/can-i-get-decode-an-entityframework-model-from-a-specified-migration), 

[Entity Framework Migrations Rebuild Target Hash](https://gist.github.com/gligoran/87fe3e8eadf5db97ad03#file-targethash)



More here:

[Changing the Namespace With Entity Framework 6.0 Code First Databases](http://jameschambers.com/2014/02/changing-the-namespace-with-entity-framework-6-0-code-first-databases/),

[Namespace changes with Entity Framework 6 migrations](https://rimdev.io/namespace-changes-with-ef-migrations/)



## Create table if does not exist 

The easiest way is to handle it with try/catch section.

```sql

begin
  execute immediate
  '
    create table "SCHEMA"."__MigrationHistory"
    (
      "MigrationId" nvarchar2(150) not null,
      "ContextKey" nvarchar2(300) not null,
      "Model" blob not null,
      "ProductVersion" nvarchar2(32) not null,
      constraint "PK___MigrationHistory" primary key("MigrationId", "ContextKey")
    )
  ';
exception
  when others then
    if sqlcode <> -955 then
      raise;
    end if;
end;
/
```

source: http://stackoverflow.com/questions/15630771/check-table-exist-or-not-before-create-it-in-oracle

## Drop all tables in schema 

```sql
BEGIN
   FOR cur_rec IN (SELECT object_name, object_type
                     FROM user_objects
                    WHERE object_type IN
                             ('TABLE',
                              'VIEW',
                              'PACKAGE',
                              'PROCEDURE',
                              'FUNCTION',
                              'SEQUENCE'
                             ))
   LOOP
      BEGIN
         IF cur_rec.object_type = 'TABLE'
         THEN
            EXECUTE IMMEDIATE    'DROP '
                              || cur_rec.object_type
                              || ' "'
                              || cur_rec.object_name
                              || '" CASCADE CONSTRAINTS';
         ELSE
            EXECUTE IMMEDIATE    'DROP '
                              || cur_rec.object_type
                              || ' "'
                              || cur_rec.object_name
                              || '"';
         END IF;
      EXCEPTION
         WHEN OTHERS
         THEN
            DBMS_OUTPUT.put_line (   'FAILED: DROP '
                                  || cur_rec.object_type
                                  || ' "'
                                  || cur_rec.object_name
                                  || '"'
                                 );
      END;
   END LOOP;
END;
/
```

source: http://stackoverflow.com/a/1690419/864968



## Inserting IDs to Identity columns

Sometime you have to insert row with specific ID. If your ID column is was defined with `generated always as identity not null` you will get the following exception: `ORA-32795: cannot insert into a generated always identity column`. This is the default way of generting scripts by EF btw.

You can replace this definition with `genereted by default as identity on null`. After doing this, you will be able to insert values to ID columns and nulls will be replaced with generated values.



## Making backup

It will work for 12c. Not sure will for 11g.

Firstly, create directory where your backups will be stored.

Open **sqldeveloper** and run query (as `sys`) to see if `DATA_PUMP_DIR` already exists.

```sql
select * from dba_directories;
```

if not, create one

```plsql
create directory my_data_pump_dir as 'C:\dev\oracle_data_pump';
grant read, write on directory my_data_pump_dir to sys;
```

Open `cmd` (not `powershell`!) and login to `expdp` as oracle `sys` user. `orcl` is my TNS.

```
expdp \"SYS@oracle AS SYSDBA\" ^
DIRECTORY=my_data_pump_dir ^
REUSE_DUMPFILES=y ^
LOGFILE=my_data_pump_dir:expsh.log ^
DUMPFILE='identityserver%U.dmp' ^
SCHEMAS=identityserver
```





https://stackoverflow.com/a/9259221/864968

http://oracleinaction.com/ora-39070-unable-to-open-the-log-file/

https://serverfault.com/a/368041/364838

https://stackoverflow.com/a/605724/864968

## Types mapping

Sometime we need to configure column types explicitly.

### Extensions

We can use extensions.

#### String properties

```csharp

using System;
using System.Data.Entity.ModelConfiguration.Configuration;

/// <summary>
/// String property configuration helpers.
/// More info about Oracle Data Provider mapping can be found here https://docs.oracle.com/cd/E63277_01/win.121/e63268/entityCodeFirst.htm
/// </summary>
public static class OracleStringPropertyConfigurationExtensions
{
  /// <summary>
  /// Configures string property for valid NVarChar2 column.
  /// Sets max length and encoding.
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <param name="maxLength">Column value max size. Size cannot be greater than 2000 bytes.</param>
  /// <returns>Property configuration</returns>
  public static StringPropertyConfiguration IsNVarChar2(this StringPropertyConfiguration property, int maxLength = NVARCHAR2_MAX)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));
    if (maxLength > NVARCHAR2_MAX) throw new ArgumentException($"Oracle nvarchar2 column accepts strings that are not greater than {NVARCHAR2_MAX}.");

    property
      .HasMaxLength(maxLength)
      .IsUnicode(true);

    return property;
  }

  /// <summary>
  /// Configures string property for valid NVarChar2 column.
  /// Sets max length and encoding.
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <param name="maxLength">Column value max size. Size cannot be greater than 4000 bytes.</param>
  /// <returns>Property configuration</returns>
  public static StringPropertyConfiguration IsVarChar2(this StringPropertyConfiguration property, int maxLength = VARCHAR2_MAX)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));
    if (maxLength > VARCHAR2_MAX) throw new ArgumentException($"Oracle varchar2 column accepts strings that are not greater than {VARCHAR2_MAX}.");

    property
      .HasMaxLength(maxLength)
      .IsUnicode(false);

    return property;
  }

  /// <summary>
  /// Configures string property for valid NVarChar2 column.
  /// Sets max length and encoding.
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <param name="maxLength">Column value max size. Size must be greater than 4000 bytes.</param>
  /// <returns>Property configuration</returns>
  public static StringPropertyConfiguration IsClob(this StringPropertyConfiguration property, int? maxLength = null)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));
    if (maxLength < CLOB_MIN) throw new ArgumentException($"To configure column as CLOB type set its max length to be greater or equal than {CLOB_MIN}.");

    property.IsUnicode(false);

    // String Length
    if (maxLength.HasValue) property.HasMaxLength(maxLength);
    else property.IsMaxLength();

    return property;
  }

  /// <summary>
  /// Configures string property for valid NVarChar2 column.
  /// Sets max length and encoding.
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <param name="maxLength">Column value max size. Size must be greater than 4000 bytes.</param>
  /// <returns>Property configuration</returns>
  public static StringPropertyConfiguration IsNClob(this StringPropertyConfiguration property, int? maxLength = null)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));
    if (maxLength < NCLOB_MIN) throw new ArgumentException($"To configure column as NCLOB type set its max length to be greater or equal than {NCLOB_MIN}.");

    property.IsUnicode(true);

    // String Length
    if (maxLength.HasValue) property.HasMaxLength(maxLength);
    else property.IsMaxLength();

    return property;
  }

  private const short NVARCHAR2_MAX = 2000;
  private const short VARCHAR2_MAX = 4000;
  private const short CLOB_MIN = 4001;
  private const short NCLOB_MIN = CLOB_MIN;
}
```



#### Binary properties

```csharp

using System.Data.Entity.ModelConfiguration.Configuration;

/// <summary>
/// Binary property configuration helpers.
/// More info about Oracle Data Provider mapping can be found here https://docs.oracle.com/cd/E10405_01/appdev.120/e10379/ss_oracle_compared.htm
/// </summary>
public static class OracleBinaryPropertyConfigurationExtensions
{
  /// <summary>
  /// Configures property to map the type BLOB.
  /// BLOB is equivalent of MSSQL Image type.
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <returns>Property configuration</returns>
  public static BinaryPropertyConfiguration IsOracleImage(this BinaryPropertyConfiguration property)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));

    property.HasColumnType("BLOB");

    return property;
  }
}
```



#### Numeric properties

```csharp
using System.Data.Entity.ModelConfiguration.Configuration;

/// <summary>
/// Decimal property configuration helpers.
/// More info about Oracle Data Provider mapping can be found here https://docs.oracle.com/cd/E63277_01/win.121/e63268/entityCodeFirst.htm
/// </summary>
public static class OracleDecimalPropertyConfigurationExtensions
{
  /// <summary>
  /// Configures property to map the type Number with given scale and precision.
  /// https://docs.oracle.com/cd/E10405_01/appdev.120/e10379/ss_oracle_compared.htm
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <param name="precision">Number of digits in a number.</param>
  /// <param name="scale">Scale is the number of digits to the right of the decimal point in a number.</param>
  /// <returns>Property configuration</returns>
  public static DecimalPropertyConfiguration IsNumber(this DecimalPropertyConfiguration property, byte precision, byte scale)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));

    if (precision < 1 || precision > 38)
      throw new InvalidOperationException("Precision must be between 1 and 38.");

    if (scale > precision)
      throw new InvalidOperationException("Scale must be between 0 and the Precision value.");

    property
      .HasColumnType("NUMBER")
      .HasPrecision(precision, scale);

    return property;
  }

  /// <summary>
  /// Configures property to map the type Number(19,4).
  /// This is equivalent of Money type from MS SQL.
  /// https://docs.oracle.com/cd/E10405_01/appdev.120/e10379/ss_oracle_compared.htm
  /// </summary>
  /// <param name="property">Property to be configured.</param>
  /// <returns>Property configuration</returns>
  public static DecimalPropertyConfiguration IsOracleMoney(this DecimalPropertyConfiguration property)
  {
    if (property == null) throw new ArgumentNullException(nameof(property));

    property.IsNumber(19, 4);

    return property;
  }
}
```



## Consolidate

Keep both `Oracle.ManagedDataAccess` and `Oracle.ManagedDataAccess.EntityFramework` in the same version. It's good practice to have assemblies of same version across whole all projects in the solution.



## Example EF DbContext configuration

```csharp
public class FoobarContext : DbContext, IFoobarContext
{
     ////////// DbSets //////////

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        foreach (var configuration in GetConfigurations())
        {
            base.OnModelCreating(configuration.ConfigureModel(modelBuilder));
        }
    }

    protected virtual IEnumerable<IModelCreateConfiguration> ConfigurationsForMssql
    {
        get
        {
            yield return new MssqlModelCreateConfiguration();
            // Add more 'yield return' for more CreateConfigurations
        }
    }

    protected virtual IEnumerable<IModelCreateConfiguration> ConfigurationsForOracle
    {
        get
        {
            yield return new OracleModelCreateConfiguration();
            // Add more 'yield return' for more CreateConfigurations
        }
    }

    private IEnumerable<IModelCreateConfiguration> GetConfigurations()
    {
        return this.GetDatabaseType()
            .ReturnWhen(
                mssql: () => ConfigurationsForMssql,
                oracle: () => ConfigurationsForOracle);
    }
}

public interface IModelCreateConfiguration
{
    DbModelBuilder ConfigureModel(DbModelBuilder modelBuilder);
}

public class OracleFoobarModelCreateConfiguration : FoobarCreateModelConfiguration
{
    public override DbModelBuilder ConfigureModel(DbModelBuilder modelBuilder)
    {
        base.ConfigureModel(modelBuilder);

        modelBuilder.HasDefaultSchemaForOracle("Foobar", true);

        modelBuilder.Configurations.Add(new FooyConfigurationForOracle());
        modelBuilder.Configurations.Add(new BarConfigurationForOracle());
        modelBuilder.Configurations.Add(new BuzzConfigurationForOracle());

      	// more configurations ...

        return modelBuilder;
    }
}


public class OracleMigrationConfiguration : FoobarDbMigrationsConfiguration
{
    public OracleMigrationConfiguration()
        : base(new Arguments
        {
            MigrationDirectoryPath = @"Contexts\FoobarContext\Oracle\Migrations",
            MigrationNamespace = @"Foobar.FoobarContext.Oracle.Migrations"
        })
    {
    }
}

public abstract class MigrationConfigurationBase : DbMigrationsConfigurationBase<FoobarContext>
{
    protected MigrationConfiguration(Arguments arguments)
        : base(arguments.Configure(args =>
                         {
                             args.ContextKey = CONTEXT_KEY;
                         }))
    {
    }

    public const string CONTEXT_KEY = "FoobarContext";
}


public abstract class DbMigrationsConfigurationBase<TContext> : DbMigrationsConfiguration<TContext> where TContext : DbContext, new()
{
    protected FoobarDbMigrationsConfiguration(Arguments arguments)
    {
        DatabaseHelper.ConfigureAutomaticDatabaseMigration(this); // true
        DatabaseHelper.SetMigrationDirectory(this, arguments.MigrationDirectoryPath);
        DatabaseHelper.SetContextKey(this, arguments.ContextKey);
        DatabaseHelper.SetMigrationNamespace(this, arguments.MigrationNamespace);
    }

    /// <summary>
    /// Arguments for DbMigrationsConfiguration
    /// </summary>
    public class Arguments
    {
        /// <summary>
        /// Migration directory is the place where all migration classes files will be stored.
        /// </summary>
        public string MigrationDirectoryPath { get; set; }

        /// <summary>
        /// Context Key must remain the same so migration remain continous.
        /// </summary>
        public string ContextKey { get; set; }

        /// <summary>
        /// Generated migration classes namespaces.
        /// </summary>
        public string MigrationNamespace { get; set; }

    }
}

public static class DatabaseHelper
{
    public static void ConfigureAutomaticDatabaseMigration<TContext>(DbMigrationsConfiguration<TContext> migration) where TContext : DbContext
    {
        migration.AutomaticMigrationsEnabled = Convert.ToBoolean(WebConfigurationManager.AppSettings[SettingConstants.AutomaticMigrationsEnabled]);
        migration.AutomaticMigrationDataLossAllowed = Convert.ToBoolean(WebConfigurationManager.AppSettings[SettingConstants.AutomaticMigrationDataLossAllowed]);
    }

    public static void SetMigrationDirectory<TContext>(DbMigrationsConfiguration<TContext> migration,
                                                       string migrationsPath)
      where TContext : DbContext
    {
        if (string.IsNullOrEmpty(migrationsPath))
        {
            throw new ArgumentException($"{nameof(migrationsPath)} cannot be null or empty.");
        }

        if (migrationsPath.Contains("/"))
            throw new ArgumentException($"Invalid {nameof(migrationsPath)}. Path should be valid Windows path. Use backslashes instead of slashes.");

        migration.MigrationsDirectory = migrationsPath;
    }

    public static void SetContextKey<TContext>(DbMigrationsConfiguration<TContext> migration,
                                               string contextKey)
      where TContext : DbContext
    {
        if (contextKey != null)
        {
            migration.ContextKey = contextKey;
        }
    }

    public static void SetMigrationNamespace<TContext>(DbMigrationsConfiguration<TContext> migration,
                                                       string migrationNamespace)
      where TContext : DbContext
    {
        if (string.IsNullOrEmpty(migrationNamespace) == false)
        {
            migration.MigrationsNamespace = migrationNamespace;
        }
    } 
}

public enum DatabaseType
{
    Mssql, Oracle
}

public static class DatabaseTypeProviderExtensions
{
    public static void PerformActionOn(this DatabaseType type,
                                       Action mssql,
                                       Action oracle)
    {
        switch (type)
        {
            case DatabaseType.Mssql:
                mssql();
                break;

            case DatabaseType.Oracle:
                oracle();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static TResult ReturnOn<TResult>(this DatabaseType type,
                                            Func<TResult> mssql,
                                            Func<TResult> oracle)
    {
        switch (type)
        {
            case DatabaseType.Mssql:
                return mssql();

            case DatabaseType.Oracle:
                return oracle();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public static class DbModelBuilderExtensions
{    
    /// <summary>
    /// Set default schema name for context. Schema name must be valid with Oracle schemas standard.
    /// </summary>
    /// <param name="builder">Context model builder.</param>
    /// <param name="schemaName">Schema name to set.</param>
    /// <param name="capitalize">Capilize provided schema name.</param>
    /// <returns>Context model builder.</returns>
    public static DbModelBuilder HasDefaultSchemaForOracle(this DbModelBuilder builder,
                                                           string schemaName,
                                                           bool capitalize = false)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        // Schema name validation

        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentNullException(nameof(schemaName));

        if (schemaName.Length > 30)
            throw new ArgumentException("Schema name max length is 30.");

        if (capitalize)
        {
            schemaName = schemaName.ToUpperInvariant();
        }

        if (schemaName.All(char.IsUpper) == false) // This will also check for whitespaces ;)
            throw new ArgumentException("Schema name should contain no whitespaces and be uppercase.");

        builder.HasDefaultSchema(schemaName);

        return builder;
    }
}


/// <summary>
/// Enables fluent configuration.
/// </summary>
public static class ConfigureExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="system"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static T Configure<T>(this T system, Action<T> configure)
    {
        if (system == null)
            throw new ArgumentNullException(nameof(system));

        configure(system);
        return system;
    }
}
```





## Disclaimer #1

I'm Oracle newbie, a .NET developer, *challenged* to achieve impossible with technology I used to have very little to do in the past. If you know any better solution, feel free to suggest or create Pull Request to this article. Thank you in advance! 



## Disclaimer #2

We've done it. We've delivered the product to client in the last day without any bigger bugs. 

We were heroes.



Two weeks laters, product owners decided that we're not merging oracle to master branch. 

"We won't have any clients with Oracle *ever*."



Please kill me.
