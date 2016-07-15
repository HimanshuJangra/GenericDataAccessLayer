# GenericDataAccessLayer
What exactly was the intention of this project?

I often asked myself, why people do not create a proper Data Access Layer (as Repository) to minimize the code and complexity.
We want to Create, Read, Update and Delete (CRUD) something. Some implement DAL Schema based, some write blob that contains whole database. Both examples create a redundant code that repease itself alot of time.

This project is an example how we can use stored procedures (SP) to implement CRUD and more using Entity Framework (EF) model first to create maintainable DAL.
I will try to push some examples and Unit Tests für DAL until next month.

# Lazy DAL
This is the second project inside the solution. 

Why Lazy? 
* If you are very lazy and dont want to implement some new Stored Procedure... 
	* You can use interface to create a new SP Call.
* Maintainability of the code is super easy. Just change the interface defintion.
* Reduce Code Coverage for any call. 
	* Mock the IRepository interface if you don't want to mock IDbConnection and it subclasses.
	* Mocking the interface will not cause any Code Coverage issues

```c#
public interface SomeNewSuperCalls : IRepository
{
	SomeEntity GetSomeEntity(int id);
	void UpdateSomeEntities(IEnumerable items);
}
```
further initialize the Lazy DAL with and call the method:

```c#
using (var repository = DynamicRepository.CreateDynamic<SomeNewSuperCalls>())
{
	var result = repository.GetSomeEntity(1);
}
```
```DynamicRepository.CreateDynamic<>``` create a new interceptor that handle all operation provided by interface definition

IRepository contains properties like:
* ConnectionStringSettings: "DefaultConnection" as default if not set
* Connection: IDbConnection, that can be exchanged on runtime... be aware, if you call dispose and call Connection again the new Connection will be created by DbProvider that use ConnectionStringSettings
* QueryExecutionTime, TotalExecutionTime: Query and Total Execution tracker. Will be reseted before we start a new method call
* TvpNameConvension: naming of the datatable by type and pre-/post-/any-fixes. By Default - "{0}TVP"
* Operations: 
```c#
[Flags]
public enum RepositoryOperations
{
	/// <summary>
	/// Remove all extended operations
	/// </summary>
	None,
	/// <summary>
	/// Enable using Table Valued Parameter => List will be converted to Table in SQL
	/// </summary>
	UseTableValuedParameter = 1,
	/// <summary>
	/// Log Total Execution Time for a whole "Process"
	/// </summary>
	LogTotalExecutionTime = 2,
	/// <summary>
	/// Log Database Execution Time.
	/// </summary>
	LogQueryExecutionTime = 4,
	/// <summary>
	/// Any exception that happens during stored procedure execution will be ignored
	/// </summary>
	IgnoreException = 8,
	/// <summary>
	/// Init only Log Execution watches
	/// </summary>
	TimeLoggerOnly = LogTotalExecutionTime | LogQueryExecutionTime,
	/// <summary>
	/// Include all operations
	/// </summary>
	All = UseTableValuedParameter | LogTotalExecutionTime | LogQueryExecutionTime | IgnoreException
}
```
### What can be used as parameter in IRepository methods

* Shared (TVP & ^TVP)
	* primitives - casted to DbType.Whatever
	* class - will be transformed into parameter foreach property (not tested)
* TVP - ```IRepository.Operations = UseTableValuedParameter;``` 
	* ICollection - as Table Values Parameter (TVP)
	* No limitation of ICollection parameters
* NOT TVP
	* Only one ICollection. Any futher will throw not supported exception
	
### More exaples
Take a look on Unit Test project

### Next Steps
* finish unit tests with proper coordination
* End to End Tests, incl. local database
* T4 templates für Generic DAL
	* all abstract implementations
	* unit test for the current example (and unit test T4 template for code coverage)


