This step allows you to modify the user settings.

For using this activity you must access here and select Set User Settings:
![](Set%20User%20Settings_wf1.gif)


Then in the activity you can fill all the parameters:

![](Set%20User%20Settings_wf2.png)

The Parameters are:
* User: Select the User to be updated
* PagingLimit: Specify how many records per view. Value can be 25,50,75,100,250  
* AdvancedFindStartupMode: Specify AdvancedFind mode. 1:simple, 2:detail.
* TimeZoneCode: Specify TimeZoneCode for users. Use Get-CrmTimeZones to see all options 0 for ignore the value. example of all values are here: http://www.powerobjects.com/2014/07/25/importing-values-time-zone-fields-dynamics-crm/
* HelpLanguageId: Specify Unique identifier of the Help language. 0 for ignore
* UILanguageId: Specify Unique identifier of the language in which to view the user interface (UI). 0 for ignore
* DefaultCalendarView: specify the default calendar view values:  0 to Show the day by default. 2 to Show the month by default.  1 to Show the week by default
* IsSendAsAllowed
