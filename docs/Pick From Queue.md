This step allows you to pick a defined number of items from a Queue, and also to set of you want to remove from the source. 
With this solution you can automate the Queue Picking with workflows just by dates, avoiding the "Cherry Picking" issue.

For using this activity you must access here and select Pick From Queue:

![](Pick%20From%20Queue_wf1.gif)

Then in the activity you can fill all the parameters:

![](Pick%20From%20Queue_wf2.gif)

The Parameters are:
* Source Queue: Select the source Queue where the items to be picked come from.
* Remove Items From Source Queue: (yes/no) set if you want to remove the picked items from the source Queue.
* Quantity Items: Quantity of records to be picked from the source Queue. 

Note: The Picking order is defined by the field "enteredon" on descending order.
