This action builds the record URL of any record from its entity logical name and its id (GUID).

It is useful between steps that return an id and steps that need a record URL, for example to pass the record
created by **Clone Record** (which returns the new record's GUID) to **Clone Children** (which needs a record URL).

Parameters:
* **Reference Record URL (required)**: any record URL from the same environment, usually the Record URL of the record
  the workflow runs on. Only its address is used.
* **Record ID (required)**: the GUID of the record.
* **Entity Logical Name (required)**: the logical name of the record's entity, e.g. `contact`.

Output:
* **Record URL**: the record URL, e.g. `https://org.crm.dynamics.com/main.aspx?etc=2&id=...&etn=contact&pagetype=entityrecord`.

Originally contributed by [vinaymenda](https://github.com/vinaymenda) (demianrasko/Dynamics-365-Workflow-Tools#274).
