This step is helps you retrieve Rollups results (Count, Sum, Average, Max and Min) on the fly on output parameters of the Workflow Activity.
It's based on one input Parameter with a Fetch XML, where you can set a Dynamic Value (for the parent filter), or just create an open FetchXML.

For using this activity you mus access here and select Rollup Functions:

![](Rollup%20Functions_wf1.gif)

Then, you must set the FetchXML parameter:

![](Rollup%20Functions_wf2.gif)

The full params description is:
* **FetchXML (required)** : The FetchXML Query

Output Parameters:
* **Count**
* **Sum**
* **Average**
* **Max**
* **Min**


**Dynamic Query from Parent record**: In the FetchXML, you can set a dynamic value for the parent record using the TAG "{PARENT_GUID}" like I show in the following image:
![](Rollup%20Functions_wf3.gif)

**Value for Calculations**: For calculations, I use only the first Attribute on the FetchXML query. This attribute must to be a number or money Attribute.

Finally you can use all the output values like this:
![](Rollup%20Functions_wf4.gif)
