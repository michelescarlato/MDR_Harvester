# MDR_Harvester
Takes downloaded source files and loads their data into the mdr's source databases.

The program uses the XML files already downloaded for a data source, located in a source folder (one is designated for each source). The XML files, or a subset as controlled by the parameters - see below - are converted into data in the 'sd' schema (= session data) tables within each source database. Note that on each run the sd tables are dropped and created anew, and thus only ever contain the data from the most recent harvest. The tables present will vary in different databases, though if a table *is* present it will have a consistent structure in every database. The conversion to sd data therefore represents the second and final stage of the conversion of the source data into the consistent ECRIN schema. For that reason the detailed code for different sources can vary widely. <br/><br/>
The program represents the second stage in the 5 stage MDR extraction process:<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Download => **Harvest** => Import => Coding => Aggregation<br/><br/>
For a much more detailed explanation of the extraction process,and the MDR system as a whole, please see the project wiki (landing page at https://ecrin-mdr.online/index.php/Project_Overview).<br/>
In particular, for the harvesting process, please see<br/>
http://ecrin-mdr.online/index.php/Harvesting_Data and linked pages

## Parameters and Usage
The system can take the following parameters:<br/>
**-s:** expects to be followed by a list of integer source ids, each representing a data source within the system. The data is obtained and added to the store of JSON source files for that source.<br/>
**-t:** followed by an integer. Indicates the type of harvest to be carried out. Types available vary for different source systems, and each type is linked with possible pre-requisites, e.g. cut-off date, start and end pages, filter to be applied, etc. - see below for specific details. The system checks for the presence of appropriate suitable pre-requisites, for the specified type, before proceeding.<br/>
<br/>
<br/>
Routine usage, as in the scheduled extraction process,  <br/>

## Dependencies
The program used the Nuget packages:
* CommandLineParser - to carry out initial processing of the CLI arguments
* Npgsql, Dapper and Dapper.contrib to handle database connectivity
* Microsoft.Extensions.Configuration, .Configuration.Json, and .Hosting to read the json settings file and support the initial host setup.

## Provenance
* Author: Steve Canham
* Organisation: ECRIN (https://ecrin.org)
* System: Clinical Research Metadata Repository (MDR)
* Project: EOSC Life
* Funding: EU H2020 programme, grant 824087
