/* Author: Christy Wijaya
 * Desc: App to check payment for each tenant up to current date
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace ConsoleAccount
{
    class Program
    {
        static void Main(string[] args)
        {
            // Pull data from database
            DataReader reader = new DataReader();
            reader.CheckPayment();
            Console.Title = "ConsoleAccount";

            // Display menu and read user input, Esc/Enter keystroke to quit
            while (true)
            {
                Console.Clear();
                reader.ReadKeyStroke(true);
            }
        }
    }
}
