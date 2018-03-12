using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToFile
{
    public class FatalFormatException : WrongFormatException
    {
        public FatalFormatException(string message, string typeName, long LineNbr, string fileName)
            : base(message, typeName, LineNbr, fileName)
        {
        }
    }

    public class WrongFormatException : Exception
    {

        public WrongFormatException(string message, string typeName, long LineNbr, string fileName)
            : base(message, null)
        {
            Data["TypeName"] = typeName;
            Data["FileName"] = fileName;
            Data["LineNbr"] = LineNbr;
        }


        public WrongFormatException(string message, string typeName, string Value, FileColumnAttribute col, long LineNbr, string fileName)
            : base(message, null)
        {
            Data["TypeName"] = typeName;
            Data["FileName"] = fileName;
            Data["LineNbr"] = LineNbr;
            Data["Value"] = Value;
            Data["FileColumnAttribute"] = col.FieldIndex;
        }

        

        public WrongFormatException(string message, string typeName, string Value, string fileName)
            : base(message, null)
        {
            Data["TypeName"] = typeName;
            Data["FileName"] = fileName;
            Data["Value"] = Value;
        }

        /*
        public WrongFormatException(Exception ex, string typeName, string Value, string fileName)
            : base(null, ex)
        {
            Data["TypeName"] = typeName;
            Data["FileName"] = fileName;
            Data["Value"] = Value;
        }*/


        public WrongFormatException(string message)
            : base(message, null)
        {
        }

        public WrongFormatException(Exception ex)
            : base("", ex)
        {
        }
    }


   public class AggregatedException : Exception
    {
        public HashSet<Exception> m_InnerExceptionsList = null;
        private int m_MaximumNbrExceptions = 100;

        // -----

        public AggregatedException(string typeName, string fileName, int maximumNbrExceptions) :
            base(string.Format(
                 "There were 1 or more exceptions while reading data using type \"{0}\"." +
                 ((fileName == null) ? "" : " Reading file \"" + fileName + "\"."),
                 typeName))
        {
            m_MaximumNbrExceptions = maximumNbrExceptions;
            m_InnerExceptionsList = new HashSet<Exception>();

            Data["TypeName"] = typeName;
            Data["FileName"] = fileName;
            Data["InnerExceptionsList"] = m_InnerExceptionsList;
        }

        public void AddException(Exception ex)
        {
            m_InnerExceptionsList.Add(ex);
            if ((m_MaximumNbrExceptions != -1) &&
                (m_InnerExceptionsList.Count >= m_MaximumNbrExceptions))
            {
                throw this;
            }
        }

        public void ThrowIfExceptionsStored()
        {
            if (m_InnerExceptionsList.Count > 0)
            {
                throw this;
            }
        }
    }
}
