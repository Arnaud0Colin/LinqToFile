using LinqToFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTest
{
    class Program
    {
        static void Main(string[] args)
        {

            ExportFileDescription inputFileDescription = new ExportFileDescription
            {
                SeparatorChar = ';',
                EnforceAllField = true,
                FirstLineHasColumnNames = true,
                FileCultureName = "fr-FR"
            };

          
            int Max = 500;
            HashSet<Company> list = new HashSet<Company>();
            for (int i = 1; i <= Max; i++)
            {
                list.Add(new Company() { Account_Code = i, A = i*23, B = i * 50, C = i * 75 });
            }

                using (FileContext cc = new FileContext())
            {
                cc.Write(list, "test.csv", inputFileDescription, null);
            }

        }

    }
}
