using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;


namespace LinqToFile
{
    public class ExportFileDescription
    {
        private CultureInfo m_cultureInfo = null;

        private int m_maximumNbrExceptions = 100;
        public int MaximumNbrExceptions
        {
            get { return m_maximumNbrExceptions; }
            set { m_maximumNbrExceptions = value; }
        }

        public iValidationChar ValidChar = null;

        public bool? FixColunm = null;

        public bool UseMaxLenght { get { return FixColunm ?? false || SeparatorChar == null; } }


        public char? SeparatorChar = null;
        public bool EnforceAllField { get; set; }

        public bool QuoteAllFields { get; set; }
        public bool FirstLineHasColumnNames { get; set; }

        public bool AllowSkipColunm { get; set; }

        public string FileCultureName
        {
            get { return m_cultureInfo.Name; }
            set { m_cultureInfo = new CultureInfo(value); }
        }
        public CultureInfo FileCultureInfo
        {
            get { return m_cultureInfo; }
            set { m_cultureInfo = value; }
        }

        public Encoding TextEncoding { get; set; }
        public bool DetectEncodingFromByteOrderMarks { get; set; }

        public bool IsValid
        {
            get
            {
                if (!SeparatorChar.HasValue && (FirstLineHasColumnNames || QuoteAllFields))
                    return false;

                


                return true;
            }
        }


        public ExportFileDescription(bool bIso = false)
        {
            m_cultureInfo = CultureInfo.CurrentCulture;
            FirstLineHasColumnNames = false;
            EnforceAllField = false;            
            QuoteAllFields = false;
            SeparatorChar = null;
            AllowSkipColunm = true;
            AllowIndexColumnChange = false;
            if( bIso)            
                TextEncoding = Encoding.GetEncoding("ISO-8859-1");
            else
                TextEncoding = Encoding.UTF8;
            DetectEncodingFromByteOrderMarks = true;
           
        }


        public bool AllowIndexColumnChange { get; set; }
    }
}
