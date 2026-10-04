This step allows you to retrieve attachments from one Entity, and attach them to a created Email record.
You can also filter the attached Files by FileName.

For using this activity you must access here and select Entity Attachment To Email:

![](Entity%20Attachment%20To%20Email_wf1.gif)

Then in the activity you can fill all the parameters:

![](EntityEmail1.gif)

The Parameters are:
* Main Record URL: The URL of the Record that contains the source Attachments
* File Name: Name of the file you want to find (multiple). You can use "*" to search other files. All the finded files will be added to the Email
* Email: Email record to add all the Attachments. 
* Retrieve ActivityMimeAttachment: false=retrieve attachments from related notes. true=retrieve attachments from ActivityMimeAttachment (for appointments and emails)
* Select Most Recent Disctinc Files: Added option to only select the Most Recent file by distinct file name.


Note: The Main Record URL, is a standard feature of Dynamics CRM, taht contains the full URL of a record. In this URL you have the entity type, and the record GUID. Right now this is the only way we have to pass a "Dynamic" EntityReference (with not hard coding an entity type) to Workflows Activities. If you pass this string URL as a parameter, in the Workflow Activity you can retrieve this entity Reference.
