This step allows you to get the "count" of Items on one Queue, and filter them to retrieve them all, or only the unassigned ones.


For using this activity you must access here and select Queue Item Count:

![](Queue%20Item%20Count_wf1.gif)

Then in the activity you can fill all the parameters:

![](Queue%20Item%20Count_wf2.gif)

The Parameters are:
* Source Queue: Select the source Queue where the items to be count come from.
* Count Only Unassigned Items: (yes/no) set if you want to count only the Unassigned work (Worked by field is Empty).  
* ItemsCount: (Output Parameter - int) quantity of records from the source Queue. 


You can use this step, in combination of the "Pick From Queue", here is an example:
![](Queue%20Item%20Count_wf4.gif)

In this example, you can ask for the available work in the Queue "Test1", and if there are available (more than zero), pick some work.
