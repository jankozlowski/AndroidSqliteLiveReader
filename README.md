# AndroidSqliteLiveReader

Tool for quick reading sqlite database in android project within Visual Studio. Useful for testing and quick debugging android native apps, xamarin forms and Maui android apps using sqlite database.

This extension works as follows:
- user selects running device and path where database is located,
- clicking get data copies database from the device to user ApplicationData, with use of adb,
- user can view data by tables or by executing custom sql statements,
- after closing extension window, extension remembers sdkPath, dbPath and if extension settings should be visible. After closing Visual Studio temporary database is deleted from hard drive.

Because of permissions this software works only on emulators and with applications created in debug mode. User needs to have installed android SDK on the computer. Extension wast tested on basic Visual Studio themes.

**Download:**

Extension is available for download from Visual Studio Marketplace at https://marketplace.visualstudio.com/items?itemName=BinaryAlchemist.AndroidSqliteLiveReader

**How to use:**

After installation go to View -> Other Windows -> AndroidSqliteLiveView

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/04/sqlextension1.jpg" alt="AndroidSqliteLiveReader1"/>

You will have to provide a path to the folder where Android Debug Bridge is located (Default: C:\Program Files (x86)\Android\android-sdk\platform-tools)

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/04/sqlextension2.jpg" alt="AndroidSqliteLiveReader1"/>

Select from which running device You want to get database

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/04/sqlextension3.jpg" alt="AndroidSqliteLiveReader1"/>

Manually input or browse path where database is located

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/04/sqlextension4.jpg" alt="AndroidSqliteLiveReader1"/>

Click get data will download database from the device and load it to grid view

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/04/sqlextension5.jpg" alt="AndroidSqliteLiveReader1"/>

You can also execute custom sql queries using custom sql box and clicking execute

You are always working on a copy of database, any changes in the database will not affect the source data. Refreshing data requires clicking again get Data.

**Feedback:**

Any issues and enhancement suggestions can be submitted on github project page
