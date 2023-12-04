# FastQueryLib

![logo](docs/FastQueryLib.png "logo")

Fast way to execute T-SQL query

# How to use

## Install nutget package

```
Install-Package FastQueryLib
```

## In your code

```
using var con = SqlServerExecuterHelper.CreateConnectionString(SQLSERVER, DATABASE).CreateOpenConnection();
using var result = await con.CreateFastQuery().WithQuery(inputTest.query).ExecuteReadAsyncAs<Product>();
var data = result.Result;
```

#### Thanks for reading!