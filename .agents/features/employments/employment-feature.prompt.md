---
name: employment-feature
description: Prompt template for creating new aggregate features in the internal Sample projects and external projects consuming the Structur framework.
agent: sample-api-developer
tools: ['read', 'search', 'edit']
---

Your goal is to generate a new aggregate feature in the Structur Sample projects for Employments, leveraging the Structur framework.
Employments represent the employment details of an employee, including their employee number and the continuous start date of their employment.
The Employment aggregate will allow us to manage and track the employment history of employees within our system, ensuring that we have accurate and up-to-date information about their employment status and history.
The aggregate will contain an aggregate member collection that represent Employment Contracts, which will capture the details of each individual employment contract an employee has had, including the start and end dates of each contract, the position held, and any relevant notes or comments about the employment.
The aggregate will contain a complex value object to represent the employee's bank details.
You will create backend domain related objects such as aggregate roots, aggregate members, value objects, domain event handlers and projections.
You will create corresponding client API related objects such as resources.
You may also create or update documentation related to the feature you implement as well as automated tests as appropriate.

Requirements for the aggregate feature:
- The singular aggregate root is named "Employment"
- The pluralised collection name for the aggregate root is named "Employments"
- The feature should support Create operations
- The feature should support Update operations
- The feature should NOT support Delete operations
- The properties of the Employment aggregate root and corresponding resource are as follows:
  | Property Name           | Data Type on Aggregate           | Data Type on Resource                    | Business rules to apply in aggregate (if any)                                                   |
  |-------------------------|----------------------------------|------------------------------------------|-------------------------------------------------------------------------------------------------|
  | EmployeeId              | EntityId<Employee>               | Guid                                     | Valid non-empty Guid. Foreign key to the related Employee aggregate.                            |
  | EmployeeNumber          | ExternalReference                | string                                   | Not null, empty or whitespace                                                                   |
  | ContinuousStartDate     | EffectiveDate                    | DateOnly                                 | Not null.  Must be a valid date on or after 01 Jan 1990.                                        |
  | Contracts               | Collection of EmploymentContract | Collection of EmploymentContractResource | Collection must be initialised but may be empty.                                                |
  | BankDetails             | UkBankDetails                    | UkBankDetailsResource                    | Validation rules on the BankDetails value object should be applied.                             |
- The properties of the EmploymentContract aggregate member and corresponding resource are as follows:
  | Property Name                  | Data Type on Aggregate     | Data Type on Resource  | Business rules to apply in aggregate (if any)                                              |
  |--------------------------------|----------------------------|------------------------|--------------------------------------------------------------------------------------------|
  | EmploymentId                   | EntityId<Employment>       | Guid                   | Valid non-empty Guid. Foreign key to the parent Employment aggregate root.                 |
  | StartDate                      | EffectiveDate              | DateOnly               | Not null. Must be a valid date after 1990.                                                 |
  | ProbationEndDate               | EventDate?                 | DateOnly?              | Optional. If populated, must be on or after the StartDate.                                 |
  | EndDate                        | EffectiveDate?             | DateOnly?              | Optional. If populated, must be on or after the StartDate.                                 |
  | EmployerNoticePeriod           | Period                     | PeriodResource         | Validation rules on the Period value object should be applied.                             |
  | EmployeeNoticePeriod           | Period                     | PeriodResource         | Validation rules on the Period value object should be applied.                             |
  | Position                       | Role                       | string                 | Not null, empty or whitespace.                                                             |
  | Location                       | WorkLocation               | string                 | One of [Office,Home,Hybrid,Roaming,Other].                                                 |
  | LocationNotes                  | Notes?                     | string?                | Optional free text notes about the location.                                               |
  | ReportsToEmployeeId            | EntityId<Employee>?        | Guid?                  | Optional. If populated, valid non-empty Guid. Foreign key to a related Employee aggregate. |
  | CarRegistrationPlate           | CarRegistrationPlate?      | string?                | Optional. If populated, must be a valid car registration plate reference.                  |
  | Notes                          | Notes?                     | string?                | Optional free text notes about the employment contract.                                    |
  | CanClaimTravelExpensesToOffice | bool                       | bool                   | Optional and defaults to FALSE if not specified.                                           |
- Role is a new simple value object that should be created in the Structur Domain project for greater re-use.
- WorkLocation is a new simple value object that should be created in the Structur Domain project for greater re-use.  It should be restricted to the enumerated values of [Office,Home,Hybrid,Roaming,Other].
- CarRegistrationPlate is a new simple value object that should be created in the Structur Domain project for greater re-use.  It should validate that any value assigned to it is a valid car registration plate reference (for simplicity, we can assume a valid reference is any non-empty string that matches the regex pattern "^[A-Z0-9]{1,7}$", which allows for typical UK car registration plates without spaces or special characters).
- Period is a new complex value object that will be used to represent the notice period for either the employer or employee. It consists of a number and a time interval (e.g. 2 weeks, 1 month). The Period value object and corresponding resource should be created in the Structur Domain project for greater re-use (with corresponding resource added to Structur Api Core project). The properties of the Period complex value object and corresponding resource are as follows:
  | Property Name | Data Type on ValueObject | Data Type on Resource | Business rules to apply in aggregate (if any)                            |
  |---------------|--------------------------|-----------------------|--------------------------------------------------------------------------|
  | Units         | Units                    | int                   | Must be >= 0                                                             |
  | Interval      | Interval                 | string                | Must be one of [Second, Hour, Minute, Day, Week, Fortnight, Month, Year] |
- The UkBankDetails value object is not domain specific and should be created in the Structur Domain project for greater re-use (with corresponding resource added to Structur Api Core project). The properties of the UkBankDetails complex value object and corresponding resource are as follows:
  | Property Name             | Data Type on Aggregate      | Data Type on Resource  | Business rules to apply in aggregate (if any)                              |
  |---------------------------|-----------------------------|------------------------|----------------------------------------------------------------------------|
  | BankName                  | Moniker                     | string                 | Not null, empty or whitespace.                                             |
  | SortCode                  | UkBankSortCode              | string                 | Must be a valid UK bank sort code, either in format "999999" or "99-99-99" |
  | AccountNumber             | UkBankAccountNumber         | string                 | Must be a valid UK bank account number, e.g. in format "99999999"          |
  | AccountName               | Moniker                     | string                 | Not null, empty or whitespace.                                             |
  | BuildingSocietyRollNumber | UkBuildingSocietyRollNumber | string                 | Optional. If populated, must be a valid UK Building Society Roll Number.   |
