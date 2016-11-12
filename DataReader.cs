/* Author: Christy Wijaya
 * Desc: App to check payment for each tenant up to current date
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Collections;
using System.Globalization;

namespace ConsoleAccount
{
    class DataReader
    {
        DateTime lastOkDate;
        private List<PayDetail> PayDetails;
        Dictionary<string, List<CBACompleteAccess>> Payments;

        public DataReader() { }

        // Gather all data
        public void CheckPayment()
        {
            int weekDiff = 0;
            int monthDiff = 0;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;

            Console.Write("Checking Payment... ");

            // Get current week distance to OK date
            using (var billPaymentDB = new BillPaymentDataContext())
            {
                lastOkDate = billPaymentDBDB.CheckDates.ToList()[0].OnDate;
                weekDiff = cal.GetWeekOfYear(DateTime.Now, dfi.CalendarWeekRule, dfi.FirstDayOfWeek) - cal.GetWeekOfYear(lastOkDate, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                monthDiff = (((DateTime.Now.Year - lastOkDate.Year) * 12) + DateTime.Now.Month - lastOkDate.Month);

                // Work out how many bills in between ok date and month that each tenant has to pay
                PayDetails = (
                   from BillPayment in billPaymentDBDB.BillPayments.ToList()
                   join Tenant in billPaymentDBDB.Tenants.ToList() on BillPayment.TenantId equals Tenant.Id
                   join Bill in billPaymentDBDB.Bills.ToList() on BillPayment.BillId equals Bill.Id
                   select new PayDetail
                   {
                       BillName = Bill.Desc,
                       TenantName = Tenant.Name,
                       Amount = BillPayment.Amount,
                       KeyWords = BillPayment.KeyWords,
                       DueTimes = (
                       Bill.Frequency == "W" ? weekDiff :
                       Bill.Frequency == "M" ? monthDiff : 0
                   ),
                       PaidTimes = 0
                   }).ToList();

                // Work out distinct bill amounts to take transaction into account
                var distinctPayAmts = PayDetails.GroupBy(x => x.Amount).Select(x => x.First()).ToList();

                // Set up transaction placeholder
                Payments = new Dictionary<string, List<CBACompleteAccess>>();
                foreach (var tenant in billPaymentDBDB.Tenants) { Payments[tenant.Name.Split(' ')[0].ToLower()] = new List<CBACompleteAccess>(); }

                // Go through all transactions count how many times who paid what and increment the payment count if match
                using (var transDB = new AccountsDataContext())
                {
                    // Filter by incoming transfer > tenant > bill by amount
                    var incomingTrnsfr = transDB.CBACompleteAccesses.Where(x => x.TransDesc.ToLower().StartsWith("transfer from") || x.TransDesc.ToLower().StartsWith("direct credit")).ToList();
                    var tenantsTrnsfr = incomingTrnsfr.Where(x => Payments.Keys.Any(w => x.TransDesc.ToLower().Contains(w))).ToList();
                    var billTrnsfr = tenantsTrnsfr.Where(x => distinctPayAmts.Any(a => decimal.Equals(x.TransAmount, a.Amount))).ToList();

                    // Split transactions by tenants
                    foreach (var tenant in Payments.Keys.ToList())
                    {
                        Payments[tenant] = billTrnsfr.Where(x => x.TransDesc.ToLower().Contains(tenant)).ToList();

                        // Count paid bills
                        foreach (var pay in PayDetails)
                        {
                            if (pay.TenantName.ToLower().StartsWith(tenant))
                            {
                                pay.PaidTimes = Payments[tenant].Count(x => decimal.Equals(x.TransAmount, pay.Amount));
                            }
                        }
                    }
                }                
            }

            Console.WriteLine("DONE");
        }

        // Print activity menu
        public void PrintActivityMenu()
        {
            Console.WriteLine("Menu:");
            Console.WriteLine("[1] Check outstanding bills");
            Console.WriteLine("[2] Check payment transfer");
            Console.WriteLine("[Esc | Enter] Exit");
        }

        // Take input and follow up
        public void ReadKeyStroke(bool mainMenu)
        {
            if (mainMenu)
                PrintActivityMenu();

            ConsoleKeyInfo key = Console.ReadKey();
            int input = 0;

            // Exit
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter) Environment.Exit(0);

            Console.Clear();

            // Get the key stroke and action
            if (mainMenu)
            {
                if (int.TryParse(key.KeyChar.ToString(), out input) && Enumerable.Range(1, 2).Contains(input))
                {
                    if (input.Equals(1))
                    {
                        PrintOutStandingBills();
                    }
                    else if (input.Equals(2))
                        PrintTenantList();
                }
            }
            else
            {
                if (Payments.Keys.ToList().Any(x => x.ToLower()[0].Equals(key.KeyChar)))
                    PrintIncomingTransferFrom(key.KeyChar);
            }
        }

        // Print outstanding bills
        private void PrintOutStandingBills()
        {
            PayDetails.ForEach(x => Console.WriteLine("{0} has paid [{1}/{2}] - {3} for ${4}", x.TenantName, x.PaidTimes, x.DueTimes, x.BillName, x.Amount));
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // Print tenant names option for payment transfers
        private void PrintTenantList()
        {
            Console.WriteLine("Print incoming bill payments from: ");

            foreach (var tenant in this.Payments.Keys.ToList())
                Console.WriteLine("[{0}]{1}", tenant[0].ToString().ToUpper(), tenant.Substring(1,tenant.Length-1));

            Console.WriteLine("\nOr any other key to go back...");
            this.ReadKeyStroke(false);
        }

        // Print incoming transfer record per page
        private void PrintIncomingTransferFrom(char key)
        {
            string tenant = Payments.Keys.ToList().Find(x=>x.StartsWith(key.ToString()));
            int transCount = Payments[tenant].Count;
            int entPerPage = 15;
            int page = (int)(transCount / entPerPage) + ((transCount % entPerPage) == 0 ? 0 : 1);
            int entCount = 1, onPage = 1;
            var iter = Payments[tenant].GetEnumerator();
            Console.WriteLine("[Page {0}/{1}] | Found {2} incoming transfer from {3}\n", onPage, page, transCount, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tenant));

            while (iter.MoveNext())
            {
                Console.WriteLine("[{0},{1},{2}]", iter.Current.TransDate.ToShortDateString(), iter.Current.TransAmount, iter.Current.TransDesc);
                entCount++;

                if (entCount == entPerPage || ((onPage-1) * entPerPage) + entCount == transCount)
                {
                    string nextPageMsg = onPage == page ? "\nPress any key to return to main menu..." : "\nPress any key to continue to page " + (++onPage) + "...";
                    Console.WriteLine(nextPageMsg);
                    Console.ReadKey();
                    entCount = 1;
                    Console.Clear();
                    Console.WriteLine("Found {0} incoming transfer from {1}\n", transCount, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tenant));
                }
            }
        }
    }
}
