This step is very usefull to check if the user that initialized the Workflow execution, has one specified security role

For using this activity you mus access here and select CheckUserInRole:

![](Check If User is in Role_userinrole1.gif)

Then, you must select the Security Role you want to check:

![](Check If User is in Role_userinrole2.gif)
The full params description is:
* **Role (required)** : the Security Role to be searched. you must select the Security role from the "parent" BU. This will check the role in all BUs.
* **IsUserInRole** : boolean with the result  

After that you can use the result on your fields:
![](Check If User is in Role_userinrole3.gif)
