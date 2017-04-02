**Project Description**

This project contains Tools created in WorkFlow Activities to be imported in Dynamics CRM, to use them
All the Source code is included and open.

**CHANGE**: _Updated to the Dynamics 365 (fall release)

Right now there are this tools:

* **1) Force Calculate Rollup Field**
Since Dynamics CRM 2015, we can add Rollup fields. The Rollup fields calculation is an asynchronous process, and with this project, we are giving more possibilities to this calculation.
The idea is to use the Workflows (Sync & Async) with custom workflow Activity, to force this calculation when the user define.

* **2) Apply Routing Rules**
This Action forces the execution of the active Routing Rules for the Case passed in the parameter

* **3) Query Values Step**
This Action could be used to query to another entity with two filters fields, and get up to two fields. Very usefull for example to query a custom entity used with parameters.

* **4) Share Record With Team**
This Action could be used to Share a record with a Team .

* **5) Share Record With User**
This Action could be used to Share a record with a User.

* **6) Unshare Record With Team**
This Action could be used to Unshare a record with a Team.

* **7) Unshare Record With User**
This Action could be used to Unshare a record with a User.

* **8) Check If User is in Role**
This Action is for checking if the user has assigned a Security Role.

* **9) Check If User is in Team**
This Action is for checking if the user is a member of a specified Team.

* **10) Add To Marketing List**
This Action is for adding accounts, contacts and lead to marketing lists.

* **11) Remove From Marketing List**
This Action is for adding accounts, contacts and lead to marketing lists.

* **12) Clone Record**
This Action is for record clonning. You can clone any record, using their URL.

* **13) Set Process**
This Action set the Active Business Process to a record.

* **14) Rollup Functions**
This Action is for executing a calculation of rollup values (Count, Sum, Average, Min & Max) based on numeric and Money attributes from a FetchXML Query.

* **15) Entity Attachment To Email**
With this action you will be able to attach to an Email all the Attachments from any Entity using a filter by name. 

* **16) Pick From Queue**
With this action you will be able to pick a defined number of items from a Queue. You can also set the parameter to remove them from the source Queue. With this solution you can automate the Queue Picking with workflows, avoiding the "Cherry Picking" issue.

* **17) Queue Item Count**
This action helps you to know how many items you have in a defined Queue. You can also filter the Items by assigned and unassigned oned, to help you know how many work in process, and pending to pick.

* **18) Add Role To User **
This action is to add Security Roles to Users.

* **19) Add Role To Team**
This action is to add Security Roles to Teams.

* **20) Remove Role From User**
This action is to remove Security Roles from Users.

* **21) Remove Role From Team**
This action is to remove Security Roles from Teams.

* **22) Set User Settings**
This action is for User Settings Update.

* **23) String Functions**
This action includes all string functions like Capitalized, Length, Padding, Replacing, Substring, Trim and Regular Expressions.

* **24) Delete Record**
This action is for record deletion.

* **25) Entity Json Serializer**
This action serialize an entity to jSon string Format

* **26) Qualify Lead**
This action for automation the Lead Qualification

* **27) Add Marketing List To Campaign** (Thanks to Mitch Milam)
This action for adding a list to a Campaign

* **28) Copy Marketing List Members** (Thanks to Mitch Milam)
This action for copy the list members from one list to another

* **29) Copy To Static List** (Thanks to Mitch Milam)
This action for copy the list members from one dynamic list to a static new one

* **30) Is Member Of Marketing List** (Thanks to Mitch Milam)
This action for querying if a record is in a marketing list or not

* **31) Remove From All Marketing Lists** (Thanks to Mitch Milam)
This action for removing the current record from all the lists

* **32) Numeric Functions**
This action is for basic Numerics functions

* **33) Email To Team**
This action is for adding on the "To" field of an email, all the team members

* **34) Set Process Stage** (Thanks to Pablo Peralta)
This action is for changing the current stage of the process of the record

* **35) Entity Mobile Deep Link** (Thanks to Jerry Weinstock)
This action is for retrieving the Mobile Deep Links for New, Edit, and default view of a record.

* **36) Send Email** (Thanks to George Doubinski)
This action is for email sending.

* **37) Geocode Address** 
This action is for retrieving the Latitude & Longitude from an address in a string.

* **38) Add User To Team** 
This action is for adding users to Teams.

* **39) Remove User From Team** 
This action is for removing users from Teams.

* **40) Associate Entity** 
This action is for N-N record creation with association.

* **41) Goal Recalculate** 
This action forces the Goal Recalculation.

* **42) Get Initiating User** 
This action returns the user who initiated the workflow execution.

* **43) Encrypt Text** 
This action returns the MD5 hash of a text.

* **44) Check Associate Entity** 
This action true/false if the M-M relationship exists or not.

* **45) Set State** 
This action for changing the state and Statuscode.

* **46) Update Child Records** 
This action for updating a field on multiple records, based on parent record value (or string value).

* **47) Disassociate Entity**
This action is for N-N record removal with Disassociation.

* **48) Insert Option Value**
This action is for adding Values in global and local OptionSets.

![](Home_wf1.png)
