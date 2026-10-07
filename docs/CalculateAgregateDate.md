This Action allows you to execute a dynamic FetchXML with date agregation and return the result date.
Very usefull for example to retrieve the max/min date from child records from a customer.

First, you need to select the action:

![](CalculateAgregateDate1.png

Then, you complete the FetchXML:

![](CalculateAgregateDate2.png)

And finnaly you can use the response on the workflow like this:

![](CalculateAgregateDate3.png)

An example of the fetchXML to retrieve the max date of the related activities of the account:
![](CalculateAgregateDate4.png)

Note that the {PARENT_GUID} text will be replaced dynamically with the entity context Guid.

### Which date is returned

The date comes from the first record the query returns: the first attribute in the FetchXML whose value is a date. Other columns can come first, such as a `groupby` column, for example:

```xml
<fetch aggregate="true">
  <entity name="contact">
    <attribute name="parentcustomerid" groupby="true" alias="parent" />
    <attribute name="createdon" aggregate="max" alias="latest" />
    <filter>
      <condition attribute="parentcustomerid" operator="eq" value="{PARENT_GUID}" />
    </filter>
  </entity>
</fetch>
```

### When nothing is found

A workflow date output can't be empty, so when the query returns no record (or no date), **Value is 1/1/1753** (the earliest date Dataverse can store) and **Ok is No**. Always check **Ok** before using Value: add a **Check Condition** step on *Ok equals Yes*, and only update a field with Value inside it. To clear the field when nothing is found, add an *Otherwise* branch that clears it.
