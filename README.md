# MDR_Harvester
Takes downloaded source files and loads their data into the MDR source or staging databases.

The program uses the JSON files already downloaded for a data source, located in a source folder (one is designated for each source). The JSON files, or a subset as controlled by the parameters - see below - are converted into data in the 'sd' schema (= session data) tables within each source database. Note that on each run the sd tables are dropped and created anew, and thus only ever contain the data from the most recent harvest. The tables present will vary in different databases, though if a table *is* present it will have a consistent structure in every database. The conversion to sd data therefore represents the second and final stage of the conversion of the source data into the consistent ECRIN schema. For that reason the detailed code for different sources can vary widely. <br/><br/>
The program represents the second stage in the 5 stage MDR extraction process:<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Download => **Harvest** => Import => Coding => Aggregation<br/><br/>
For a much more detailed explanation of the extraction process,and the MDR system as a whole, please see the project wiki (landing page at https://ecrin-mdr.online/index.php/Project_Overview).<br/>
In particular, for the harvesting process, please see<br/>
http://ecrin-mdr.online/index.php/Harvesting_Data and linked pages


## Parameters and Usage
The system can take the following parameters:<br/>
**-s:** Followed by a comma delimited list of source ids. The data is harvest for each specified host<br/>
**-t:** followed by an integer. Indicates the type of harvest to be carried out. (1 = full, i.e. all available files, 2 = only files downloaded since last import, 3 = test data only.<br/>
**-E:** as a flag, establishes expected test data. If present only creates and fills tables for the 'expected' data. for comparison with processed test data.<br/>
**-F:** as a flag, harvests all test data. If present, only creates and fills tables for the designated test data, for comparison with expected test data. <br/>


## Dependencies
The program is written in .Net 7.0. <br/>
It uses the following Nuget packages:
* CommandLineParser 2.9.1 - to carry out initial processing of the CLI arguments
* Npgsql 7.0.0, Dapper 2.0.123 and Dapper.contrib 2.0.78 to handle database connectivity
* PostgreSQLCopyHelper 2.8.0 to support fast bulk inserts into Postgres
* Microsoft.Extensions.Configuration 7.0.0, and .Configuration.Json 7.0.0 to read the json settings file.
* Microsoft.Extensions.DependencyInjection 7.0.0 and .Hosting 7.0.0 to support the initial setup of the application

## Provenance
* Author: Steve Canham
* Organisation: ECRIN (https://ecrin.org)
* System: Clinical Research Metadata Repository (MDR)
* Project: EOSC Life
* Funding: EU H2020 programme, grant 824087

