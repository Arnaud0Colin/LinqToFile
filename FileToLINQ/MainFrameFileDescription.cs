﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LinqToFile
{
    public class MainFrameFileDescription : ExportFileDescription
    {
        public MainFrameFileDescription( char? spChar = null): base(true)
        {
            FileCultureName = "fr-FR";
            SeparatorChar = spChar;
            ValidChar = new MainframeChar();
           // FirstLineHasColumnNames = true;
           // AllowIndexColumnChange = true;
        }

    }


    public class MainframeChar : iValidationChar
    {
        static HashSet<char> RealAllowChars = new HashSet<char>(new char[] {          
        '\x0020','\x0021','\x0022','\x0023','\x0025','\x0026','\x0027','\x0028','\x0029','\x002A',
        '\x002B','\x002C','\x002D','\x002E','\x002F','\x0030','\x0031','\x0032','\x0033','\x0034',
        '\x0035','\x0036','\x0037','\x0038','\x0039','\x003A','\x003B','\x003C','\x003D','\x003E',
        '\x003F','\x0040','\x0041','\x0042','\x0043','\x0044','\x0045','\x0046','\x0047','\x0048',
        '\x0049','\x004A','\x004B','\x004C','\x004D','\x004E','\x004F','\x0050','\x0051','\x0052',
        '\x0053','\x0054','\x0055','\x0056','\x0057','\x0058','\x0059','\x005A','\x005B','\x005C',
        '\x005D','\x005F','\x0060','\x0061','\x0062','\x0063','\x0064','\x0065','\x0066','\x0067',
        '\x0068','\x0069','\x006A','\x006B','\x006C','\x006D','\x006E','\x006F','\x0070','\x0071',
        '\x0072','\x0073','\x0074','\x0075','\x0076','\x0077','\x0078','\x0079','\x007A','\x007B',
        '\x007C','\x007D','\x007E' });


        public void Corrige(ref string str)
        {
            if (str != null)
            {
                byte[] tab = System.Text.Encoding.GetEncoding(1251).GetBytes(str);
                str = System.Text.Encoding.ASCII.GetString(tab);
                str = str.Where(p => RealAllowChars.Contains(p)).Aggregate("", (a, b) => a + b);
            }
        }

        public static bool Contains(string str)
        {
            if (str != null)
                return str.All(p => RealAllowChars.Contains(p));
            else
                return false;
        }


    }
}
