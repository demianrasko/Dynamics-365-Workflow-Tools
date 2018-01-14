This Action allows you to retrieve the Organization settings. 
All the organization settings available to be updated are here: https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/entities/organization under "Writable attributes".
Example settings are: 
- KbPrefix
- LanguageCode
- LocaleId
- MaxAppointmentDurationDays
- MaxProductsInBundle
- MaxUploadFileSize
- QuotePrefix
- SessionTimeoutEnabled
- and many more...

For using this action, just select it from the list:

![](OrgDBSettingsRetrieve.png)

And then, select the values:

![](OrgDBSettingsRetrieve1.png)

Finally you have three output parameters, depending on the value type (String, Number or Bool) and you can use them:

![](OrgDBSettingsRetrieve2.png)

A really good example for using OrgDBSettings Update&Retrieve is here: [OrgDBSettings Example](OrgDBSettingsExample.md) 


Regards!
