# MDR_Harvester
Takes downloaded source files and loads their data into the mdr's source databases.

The program uses the XML files already downloaded for a data source, located in a source folder (one is designated for each source). The XML files, or a subset as controlled by the parameters - see below - are converted into data in the 'sd' schema (= session data) tables within each source database. Note that on each run the sd tables are dropped and created anew, and thus only ever contain the data from the most recent harvest. The tables present will vary in different databases, though if a table *is* present it will have a consistent structure in every database. The conversion to sd data therefore represents the second and final stage of the conversion of the source data into the consistent ECRIN schema. For that reason the detailed code for different sources can vary widely. <br/><br/>
The program represents the second stage in the 4 stage MDR extraction process:<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Download => **Harvest** => Import => Aggregation<br/><br/>
For a much more detailed explanation of the extraction process,and the MDR system as a whole, please see the project wiki (landing page at https://ecrin-mdr.online/index.php/Project_Overview).<br/>
In particular, for the harvesting process, please see<br/>
http://ecrin-mdr.online/index.php/Harvesting_Data, and <br/>
https://ecrin-mdr.online/index.php/Contextual_Data<br/>
and linked pages

### Provenance
* Author: Steve Canham
* Organisation: ECRIN (https://ecrin.org)
* System: Clinical Research Metadata Repository (MDR)
* Project: EOSC Life
* Funding: EU H2020 programme, grant 824087

