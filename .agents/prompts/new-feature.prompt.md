---
name: new-feature
description: Prompt template for creating new aggregate features in the internal Sample projects and external projects consuming the Structur framework.
agent: feature-developer
tools: ['read', 'search', 'edit']
---

Your goal is to generate a new aggregate feature leveraging the Structur framework.  
You will create backend domain related objects such as aggregate roots, aggregate members, value objects, domain event handlers and projections.
You will create corresponding client API related objects such as resources.
You may also create or update documentation related to the feature you implement as well as automated tests as appropriate. 

Requirements for the aggregate feature:
- The singular aggregate root is named [Insert the singular name for the aggregate root here, for example 'Employee' or 'Order']
- The pluralised collection name for the aggregate root is named [Insert the pluralised name for the aggregate root here, for example 'Employees' or 'Orders']
- The feature [Insert should/should not] support Create operations
- The feature [Insert should/should not] support Update operations
- The feature [Insert should/should not] support Delete operations
- The properties of the Aggregate Root and corresponding Resource are as follows:
| Property Name                                              | Data Type on Aggregate                                                              | Data Type on Resource                                                             | Business rules to apply in aggregate (if any)                                                                 |
|------------------------------------------------------------|-------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| [Insert the name of the property here, for example 'Name'] | [Insert the data type of the property on the aggregate here, for example 'Moniker'] | [Insert the data type of the property on the resource here, for example 'string'] | [Insert any business rules to apply in the aggregate for this property here, for example 'Must not be empty'] |
