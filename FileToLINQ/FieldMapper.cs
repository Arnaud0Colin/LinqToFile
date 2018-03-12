using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToFile
{
    public class FieldMapper<T>
    {
        protected  HashSet<FileColumnAttribute> m_ListColumn = null;
        protected FileColumnAttribute[] m_Array = null;

        public ExportFileDescription m_fileDescription { get; set; }
        public string m_fileName { get; set; }

        public MemberInfo memberInfo = null;
        public Type fieldType = null;


        public FieldMapper(ExportFileDescription fileDescription, string fileName, bool writingFile, ushort? Key = null)
        {

            if (!fileDescription.IsValid)
            {
                throw new Exception("Description Invalid configuration.");
            }

            m_fileDescription = fileDescription;
            m_fileName = fileName;

            m_ListColumn = new HashSet<FileColumnAttribute>();

            m_ListColumn.Clear();

            IEnumerable<FileColumnAttribute> ListOfAttribute = typeof(T).GetCustomAttributes(typeof(FileColumnAttribute), true).Cast<FileColumnAttribute>();
            if(ListOfAttribute.Count() < 1)
                throw new Exception(string.Format("The class {0} don't have FileColumn declaration.", typeof(T).ToString()));

            if (Key.HasValue)
                ListOfAttribute = ListOfAttribute.Where(p => p.Key == Key.Value);

            var listMember = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => (m.MemberType == MemberTypes.Field) || (m.MemberType == MemberTypes.Property));

            if (fileDescription.EnforceAllField && ListOfAttribute.All(p => listMember.Any(v => v.Name == p.Property)))
                throw new Exception("EnforceCsvColumnAttribute is true, but some Menbers don't get FileColumn");

            foreach (FileColumnAttribute mi in ListOfAttribute)
            {

                if (fileDescription.FixColunm.HasValue && fileDescription.FixColunm.Value && mi.MaxLength == ushort.MaxValue)
                {
                    throw new Exception(string.Format("FixColunm is true, but {0} column missing MaxLength", mi.Property));
                }

                if (!fileDescription.SeparatorChar.HasValue && mi.MaxLength == ushort.MaxValue)
                {
                    throw new Exception(string.Format("SeparatorChar is false, but {0} column missing MaxLength", mi.Property));
                }

                if (mi.Property == null)
                {
                    if (fileDescription.AllowSkipColunm)
                    {
                        m_ListColumn.Add(mi);
                        continue;
                    }
                    else
                        throw new Exception("AllowSkipColunm is false, but some one column missing Property");
                }
                else if (mi.UsePropertyAsName && mi.Name == null)
                    mi.Name = mi.Property;

                var Field = listMember.Where(at => at.Name == mi.Property).FirstOrDefault();

                if (Field == null)
                    throw new Exception(string.Format("The field {0} is missing on the class {1}", mi.Property, typeof(T).ToString()));


                if (!m_fileDescription.FixColunm.HasValue && mi.MaxLength == UInt16.MaxValue)
                    throw new Exception(string.Format("{0} don't have a Maxlength, needed for a file With/without separator in FixColunm", mi.Property));
                

                 if (!m_fileDescription.SeparatorChar.HasValue && mi.MaxLength == UInt16.MaxValue)
                     throw new Exception(string.Format("{0} don't have a Maxlength, needed for a file without separator", mi.Property));


                if (Field is PropertyInfo)
                {
                    mi.fieldType = ((PropertyInfo)Field).PropertyType;
                    mi.MemberInfo = Field;
                }
                else
                {
                    mi.fieldType = ((FieldInfo)Field).FieldType;
                    mi.MemberInfo = Field;
                }

#if NET45
                Type theType = mi.fieldType.IsGenericType && mi.fieldType.GenericTypeArguments.Any() ? mi.fieldType.GenericTypeArguments[0] : mi.fieldType;
#else
                Type theType = mi.fieldType.IsGenericType ? mi.fieldType.GetGenericTypeDefinition() : mi.fieldType;
#endif

              
                mi.parseNumberMethod = theType.GetMethod(
                       "Parse", new Type[] { typeof(String), typeof(NumberStyles), typeof(IFormatProvider) });
                mi.parseDateMethod = theType.GetMethod(
                        "ParseExact", new Type[] { typeof(String), typeof(String), typeof(IFormatProvider) });


                if(  mi.parseNumberMethod == null && mi.parseDateMethod == null)
                    mi.typeConverter = TypeDescriptor.GetConverter(mi.fieldType);
                else
                    mi.typeConverter = null;

                if (!string.IsNullOrEmpty(mi.Name) && m_ListColumn.Any(c => c.Name == mi.Name))
                    throw new Exception(string.Format("The Name '{0}' allready exist", mi.Name));
                else
                    m_ListColumn.Add(mi);
            }

             m_Array = m_ListColumn.OrderBy(p => p.FieldIndex).ToArray();
        }



        public void WriteNames(ref List<string> row)
        {
            row.Clear();
            row = m_Array.Select(col => col.Name).ToList();
                
              //  m_ListColumn.OrderBy(p => p.FieldIndex).Select(col => col.Name).ToList();
        }



      

        /// ///////////////////////////////////////////////////////////////////////
        /// WriteObject
        /// 
        public void WriteObject(T obj, ref List<string> row)
        {
            row.Clear();

            foreach (var col in m_Array)
            {

                Object objValue = null;


                if (col.MemberInfo != null)
                {
                    if (col.MemberInfo is PropertyInfo)
                    {
                        objValue =
                            ((PropertyInfo)col.MemberInfo).GetValue(obj, null);
                    }
                    else
                    {
                        objValue =
                            ((FieldInfo)col.MemberInfo).GetValue(obj);
                    }
                }
                else
                    if (!m_fileDescription.AllowSkipColunm)
                        throw new Exception();


                row.Add(ConvertObject(col, objValue));
            }
        }


       internal void WriteObject(System.Data.SqlClient.SqlDataReader values, ref List<string> row)
       {
           row.Clear();

           foreach (var col in m_Array)
           {
               Object objValue = null;
               try
               {
                   objValue = values[col.Property];
               }
               catch( Exception ex)
               {
                   throw new Exception(string.Format("The field {0} is missing on the class {1}", col.Property, "SqlDataReader"), ex);
               }
               row.Add(ConvertObject(col, objValue));
           }
       }



       private string ConvertObject(FileColumnAttribute col, Object objValue)
       {
            

           string resultString = null;
           if (objValue != null)
           {
               if ((objValue is IFormattable))
               {
                   resultString =
                       ((IFormattable)objValue).ToString(
                           col.OutputFormat,
                            m_fileDescription.FileCultureInfo);

                   if (col.WithOutSeparator)
                       resultString = resultString.Replace(m_fileDescription.FileCultureInfo.NumberFormat.CurrencyDecimalSeparator, "");

                   if (resultString == null && !col.CanBeNull)
                       throw new Exception(string.Format("{0} is NULL, but this colunm Can't be NULL", col.Property));

                   Alter(ref resultString, col, col.TextAlign == Align.None ? false : col.TextAlign == Align.Left);

               }
               else
               {
                   resultString = objValue.ToString();

                   if (resultString == null && !col.CanBeNull)
                       throw new Exception(string.Format("{0} is NULL, but this colunm Can't be NULL", col.Property));

                   Alter(ref resultString, col, col.TextAlign == Align.None ? true : col.TextAlign == Align.Left);
               }
           }
           else
           {

               if (!col.CanBeNull)
                   throw new Exception(string.Format("{0} is NULL, but this colunm Can't be NULL", col.Property));

               Alter(ref resultString, col, col.TextAlign == Align.None ? false : col.TextAlign == Align.Left);

           }

           return resultString;
       }

       private void Alter(ref string resultString, FileColumnAttribute col, bool bLeft)
       {

           if (m_fileDescription.ValidChar != null)
               m_fileDescription.ValidChar.Corrige(ref  resultString);


            if (resultString != null)
            {

                if (col.MaxLength == UInt16.MaxValue && m_fileDescription.UseMaxLenght)
                    throw new Exception(string.Format("{0} don't have a Maxlength, needed for a file without separator", col.Property));
                

                if (m_fileDescription.UseMaxLenght && resultString.Length < col.MaxLength)
                {
                    


                    if (bLeft)
                        resultString = resultString + new string(/*' '*/col.FillChar, col.MaxLength - resultString.Length);
                    else
                        resultString = new string(col.FillChar, col.MaxLength - resultString.Length) + resultString;
                }
                else
                {
                    if(bLeft)
                        resultString = resultString.Length > col.MaxLength ? resultString.Substring(0, col.MaxLength) : resultString;
                    else
                        resultString = resultString.Length > col.MaxLength ? resultString.Substring(resultString.Length - col.MaxLength, col.MaxLength) : resultString;
                }                  

            }
            else
                resultString = new string(col.FillChar, col.MaxLength);



       //     if (!m_fileDescription.SeparatorChar.HasValue || (m_fileDescription.UseMaxLenght.HasValue && m_fileDescription.UseMaxLenght.Value))
       //    {
       //        if (col.MaxLength == UInt16.MaxValue)
       //            throw new Exception(string.Format("{0} don't have a Maxlength, needed for a file without separator", col.Property));

       //        if (resultString != null)
       //        {
       //            if (resultString.Length < col.MaxLength)
       //            {
       //                if (bLeft)
       //                    resultString = resultString + new string(/*' '*/col.FillChar , col.MaxLength - resultString.Length);
       //                else
       //                    resultString = new string(col.FillChar, col.MaxLength - resultString.Length) + resultString;
       //            }
       //            else
       //                resultString = resultString.Substring(0, col.MaxLength);
       //        }
       //        else
       //            resultString = new string(col.FillChar, col.MaxLength);
       //    }
       //    else
       //        if (col.MaxLength != UInt16.MaxValue)
       //            if (resultString != null)
       //                resultString = resultString.Length > col.MaxLength ? resultString.Substring(0, col.MaxLength) : resultString;
       }
    }
}
