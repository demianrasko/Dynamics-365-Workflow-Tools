The Geocode Address action allows you to retrieve the Latitude & Longitude from an address in a string.
It uses Azure Maps when you give an Azure Maps key, and Bing Maps otherwise.

The parameters are:
* **Address**: the address to look up. Spaces at either end are ignored.
* **Bing Maps Key**: a Bing Maps key. Microsoft is retiring Bing Maps for Enterprise: its keys stop working on June 30, 2028, so use Azure Maps for new workflows. The designer requires a value here, so when you use Azure Maps, type any text (for example `-`).
* **Azure Maps Key** (optional): an Azure Maps subscription key. When it's set, Azure Maps is used and the Bing Maps key is ignored.
* **Latitude** and **Longitude** (outputs): the location found. If the map service returns an error, the step fails with that error.

For using this activity, first you need to select the this action:

![](Geocode%20Address_wf1.gif)

Then you must set the two input parameters:

![](Geocode%20Address_wf2.gif)

NOTE: You must provide a valid Bing Maps key or Azure Maps key.

And Finnaly you can use the Latitude and Longitude output parameters:

![](Geocode%20Address_wf3.gif)

NOTE: the latitude and Longitude output parameters are numbers (Decimal)
