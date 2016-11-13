# ConsoleAccount
Bill payment check tool for CBA account dump

### Bit of story
I had to manage rent transfer between myself, my housemates, and the property agent. My housemates use to do a weekly and monthly transfer to my account before I transfer the money to the property agent. This little tool is built as a fun project to check how many transfers are required and how many transfers are done to date from a given start date indicator.

### Requirements
- C# .Net, little bit knowledge of LINQ, SQL Server database (I am using 2014 version)
- A few tables as described in BillPayment DBML
- A transaction table which I imported the CBA dump file into every now and then
- Related DBMLs to the tables in preview 

##### BillPayment DBML
![Bill Payment DBML](https://github.com/chriswijaya/ConsoleAccount/blob/master/images/BillPayment-DBML.PNG)

#### Notes
- Database structure is strictly required
- CBA dump file can be retrieved from your online account

### How to
Once the requirements are all set, DataReader class will do the task and you can add the class to your own project.
The data that after calculation would be stored under:

1. DataReader.PayDetails -> this stores how many times bill has due and paid by my mates
2. DataReader.Payments[name] -> stores all transfers for that bill for that person

### How it works
The DataReader class will filter transactions based on different tenants and amount of transcation of that tenant bills.
A tenant can have more than one bill record that is mapped into the BillPayment table.

### Previews
##### Main menu
![Main Menu Screen](https://github.com/chriswijaya/ConsoleAccount/blob/master/images/Main-menu.png)

##### Checked bill due and paid times screen (Option[1])
![Outstanding Bill Screen](https://github.com/chriswijaya/ConsoleAccount/blob/master/images/OutstandingBillScreen.png)

##### Select a person's transfer log (Option [2].1)
![Payment Transfers Select User](https://github.com/chriswijaya/ConsoleAccount/blob/master/images/PaymentTransferSelectUserScreen.png)

##### Single person transfer log screen (Option [2].2)
![Payment Transfer Screen](https://github.com/chriswijaya/ConsoleAccount/blob/master/images/PaymentTransferScreen.png)

### License
MIT