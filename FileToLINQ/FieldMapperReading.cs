using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LinqToFile
{
    public /*internal*/ class FieldMapperReading<T> : FieldMapper<T> where T : new()
    {

        private UInt16? RowCount = null;

        /// ///////////////////////////////////////////////////////////////////////
        /// FieldMapper
        /// 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileDescription"></param>
        public FieldMapperReading(
                    ExportFileDescription fileDescription,
                    string fileName,
                    bool writingFile, ushort? Key)
            : base(fileDescription, fileName, writingFile, Key)
        {
        }


        /// ///////////////////////////////////////////////////////////////////////
        /// ReadNames
        /// 
        /// <summary>
        /// Assumes that the fields in parameter row are field names.
        /// Reads the names into the objects internal structure.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="firstRow"></param>
        /// <returns></returns>
        ///
        public void ReadNames(IDataRow row)
        {
           // var array = m_ListColumn.OrderBy(p => p.FieldIndex).ToArray();
            for (int i = 0; i < row.Count; i++)
            {
                var colTitle = row[i].Value;

                if (!m_fileDescription.AllowIndexColumnChange)
                {

                    if (colTitle != null)
                        colTitle = colTitle.Trim();

                    if (m_Array[i].Name != colTitle)
                        throw new WrongFormatException("Wrong column name", typeof(T).ToString(), colTitle, m_fileName);
                }
                else
                {

                    if (colTitle != null)
                        colTitle = colTitle.Trim();
                    else
                    {
                        if (m_Array[i].Name == null)
                            m_Array[i].FieldIndex = (ushort)i;
                         else
                             throw new WrongFormatException("column doesn't exist", typeof(T).ToString(), colTitle, m_fileName);

                         continue;
                    }

                    var col = m_ListColumn.Where(p => p.Name == colTitle).FirstOrDefault();

                    if (col != null)
                    {
                        col.FieldIndex = (ushort)i;
                    }
                    else
                        throw new WrongFormatException("column doesn't exist", typeof(T).ToString(), colTitle, m_fileName);
                }
            }

            if (m_fileDescription.AllowIndexColumnChange)
                m_Array = m_ListColumn.OrderBy(p => p.FieldIndex).ToArray();
        }


        public T ReadObjectWithSeparatorChar( IDataRow row , AggregatedException ae)
        {

             T obj = new T();

            for (int i = 0; i < row.Count; i++)
            {
                FileColumnAttribute tfi = m_Array[i];
                string value = row[i].Value;
                Convert(obj, tfi, value, row[i].LineNbr, ae);
            }

            for (int i = row.Count; i < m_Array.Length; i++)
            {
                FileColumnAttribute tfi = m_Array[i];

                /*    if (((!m_fileDescription.EnforceCsvColumnAttribute) ||
                         tfi.hasColumnAttribute) &&
                        (!tfi.canBeNull))
                    {
                        ae.AddException(
                            new MissingRequiredFieldException(
                                    typeof(T).ToString(),
                                    tfi.name,
                                    row[row.Count - 1].LineNbr,
                                    m_fileName));
                    }*/
            }
            return obj;
        }

        public T ReadObjectWithOutSeparatorChar(IDataRow row, AggregatedException ae)
        {
            T obj = new T();
            int cur = 0;
            foreach (var col in m_Array)
            {

                string value = row[0].Value.Substring( cur, col.MaxLength);
                cur += col.MaxLength;

                Convert(obj, col, value, row[0].LineNbr ,ae);

            }
            return obj;
        }

        private void Convert(T obj, FileColumnAttribute col, string value, int LineNbr, AggregatedException ae)
        {

            if (value == null || col.PassOver)
            {
                if (!col.CanBeNull)
                    ae.AddException(new WrongFormatException("Value can't be null", typeof(T).ToString(), value, col, LineNbr, m_fileName));
                    //throw new Exception();
            }
            else
            {
                if (value != null)
                {
                    value = value.Trim();
                    if (col.MaxLength != UInt16.MaxValue && value.Length > col.MaxLength)
                    {
                        ae.AddException(new WrongFormatException(" Value too long", typeof(T).ToString(), value, col, LineNbr, m_fileName ));
                        value = value.Substring(0, col.MaxLength);
                    }
                }            

                try
                {
                    Object objValue = null;

                    // Normally, either tfi.typeConverter is not null,
                    // or tfi.parseNumberMethod is not null. 
                    // 
                    if (col.typeConverter != null)
                    {
                        objValue = col.typeConverter.ConvertFromString(
                                        null,
                                        m_fileDescription.FileCultureInfo,
                                        value);
                    }
                    else if (col.parseNumberMethod != null)
                    {
                        if (col.WithOutSeparator)
                        {
                            int pos = col.MaxLength - col.OutputFormat.IndexOf('.');

                            if (col.MaxLength == value.Length)
                                value = value.Substring(0, col.MaxLength - pos) + m_fileDescription.FileCultureInfo.NumberFormat.CurrencyDecimalSeparator + value.Substring(col.MaxLength - pos, pos);
                            else
                            {
                                if (value.Length > pos)
                                    value = value.Substring(0, value.Length - pos) + m_fileDescription.FileCultureInfo.NumberFormat.CurrencyDecimalSeparator + value.Substring(value.Length - pos, pos);
                                else
                                {
                                    ae.AddException(new WrongFormatException(" Value too long", typeof(T).ToString(), value, col, LineNbr, m_fileName));
                                    return;
                                    //throw new Exception();
                                }
                                
                            }
                                
                        }

                        objValue =
                            col.parseNumberMethod.Invoke(
                                col.fieldType,
                                new Object[] { 
                                    value, 
                                    col.NumberStyle, 
                                    m_fileDescription.FileCultureInfo });
                    }
                    else if (col.parseDateMethod != null)
                    {
                        objValue =
                            col.parseDateMethod.Invoke(
                                col.fieldType,
                                new Object[] { 
                                    value, 
                                    col.OutputFormat, 
                                    m_fileDescription.FileCultureInfo });
                    }
                    else
                    {
                        // No TypeConverter and no Parse method available.
                        // Try direct approach.
                        objValue = value;
                    }

                    if (col.MemberInfo != null)
                    {
                        if (col.MemberInfo is PropertyInfo)
                        {
                            ((PropertyInfo)col.MemberInfo).SetValue(obj, objValue, null);
                        }
                        else
                        {
                            ((FieldInfo)col.MemberInfo).SetValue(obj, objValue);
                        }
                    }
                    else
                        if(!m_fileDescription.AllowSkipColunm)
                            throw new Exception();
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                    {
                        ex = ex.InnerException;
                    }

                    if (ex is FormatException)
                    {

                        ae.AddException(new WrongFormatException(value.ToString() + "-" + col.ToString() + "-" + LineNbr.ToString() + "Wrong Format", typeof(T).ToString(), value, col, LineNbr, m_fileName));

                        /*ex = new WrongDataFormatException(
                                typeof(T).ToString(),
                                tfi.name,
                                value,
                                row[i].LineNbr,
                                m_fileName,
                                ex);*/
                    }

                   // ae.
                }
            }


           // return default(T);
        }


        public bool CheckValid(IDataRow row, AggregatedException ae)
        {

            if (!RowCount.HasValue)
                RowCount = (UInt16)row.Count;

            if (m_fileDescription.SeparatorChar.HasValue)
            {

                if( RowCount != row.Count)
                    throw new FatalFormatException(string.Format("Incoerante column munber"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                else
                    if (row.Count > m_ListColumn.Count())
                        throw new FatalFormatException(string.Format("The file got more column"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                    else
                        if (row.Count < m_ListColumn.Count())
                            throw new FatalFormatException(string.Format("The file got less column"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
            }
            else
            {
                if (row.Count != 1)
                    throw new FatalFormatException(string.Format("File format Error"), typeof(T).ToString(), row[0].LineNbr, m_fileName);

                if (row[0].Value == null)
                {
                    if (m_ListColumn.Any(p => !p.CanBeNull))
                        throw new FatalFormatException(string.Format("The line can't be null"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                }
                else
                {
                    if (row[0].Value.Length != m_ListColumn.Sum(p => p.MaxLength))
                        throw new FatalFormatException(string.Format("wrong number of char"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                }
            }

            return true;
        }


        /// ///////////////////////////////////////////////////////////////////////
        /// ReadObject
        /// 
        /// <summary>
        /// Creates an object of type T from the data in row and returns that object.
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="firstRow"></param>
        /// <returns></returns>
        public T ReadObject( IDataRow row, AggregatedException ae)
        {

              if (m_fileDescription.SeparatorChar.HasValue)
                  return ReadObjectWithSeparatorChar(row,ae);
              else
                  return ReadObjectWithOutSeparatorChar(row, ae);

            /*
            if (m_fileDescription.SeparatorChar.HasValue)
            {
                if (row.Count > m_ListColumn.Count())
                    throw new FatalFormatException(string.Format("The file got more column"), typeof(T).ToString(), row[0].LineNbr, m_fileName);

                return ReadObjectWithSeparatorChar(row,ae);
            }
            else
            {
                if (row.Count != 1)
                    throw new FatalFormatException(string.Format("File format Error"), typeof(T).ToString(), row[0].LineNbr, m_fileName);

                if (row[0].Value == null)
                {
                    if (m_ListColumn.Any(p => !p.CanBeNull))
                        throw new FatalFormatException(string.Format("The line can't be null"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                }
                else
                {
                    if (row[0].Value.Length != m_ListColumn.Sum(p => p.MaxLength))
                        throw new FatalFormatException(string.Format("wrong number of char"), typeof(T).ToString(), row[0].LineNbr, m_fileName);
                }

                return ReadObjectWithOutSeparatorChar(row, ae);
            }
            */
        }
    }
}
