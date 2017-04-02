This action retrieves the Mobile Deep Links strings to be used on the workflows, for example to send them on an Email.

For using this feature, first select the Entity Mobile Deep Link action:

![](Entity%20Mobile%20Deep%20Link_wf1.gif)

Then, on the input Parameters, select the record URL of the entity you want to retrieve the mobile deep link:

![](Entity%20Mobile%20Deep%20Link_wf2.gif)

Then, you can use the Output parameters on the Workflow. The Output Paramaters are:

* **Mobile Deep Link Edit**: the link to go to the record form
* **Mobile Deep Link New**: the link to go to record creation form
* **Mobile Deep Link Default View**: the link to go to the default view of the entity type

This is an example of using this params in a Email:

![](Entity%20Mobile%20Deep%20Link_wf3.gif)

You can retrieve and Send these Deep Links in a workflow like this:

![](Entity%20Mobile%20Deep%20Link_wf4.gif)

Then, you can receive this email on a mobile app:

![](Entity%20Mobile%20Deep%20Link_wf5.gif)

If you click on any link you must confirm to change the app:

![](Entity%20Mobile%20Deep%20Link_wf6.gif)

The first link goes directly to the record form:

![](Entity%20Mobile%20Deep%20Link_wf7.gif)

The creation form goes to here:

![](Entity%20Mobile%20Deep%20Link_wf8.gif)

And the default view jumps to here:

![](Entity%20Mobile%20Deep%20Link_wf9.gif)

