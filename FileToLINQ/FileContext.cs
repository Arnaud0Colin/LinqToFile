using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;


namespace LinqToFile
{
   public class FileContext : IDisposable
    {


       public ushort? Key { get; set; }


        public void Write<T>(
          IEnumerable<T> values,
          string fileName,
          ExportFileDescription fileDescription, Func<int, bool> myMethodName)
        {
            using (StreamWriter sw = new StreamWriter(
                                                 fileName,
                                                 false,
                                                 fileDescription.TextEncoding))
            {
                WriteData<T>(values, fileName, sw, fileDescription, myMethodName);
            }
        }


        public void Write<T>(
          IEnumerable<T> values,
          string fileName,
          ExportFileDescription fileDescription)
        {
            using (StreamWriter sw = new StreamWriter(
                                                 fileName,
                                                 false,
                                                 fileDescription.TextEncoding))
            {
                WriteData<T>(values, fileName, sw, fileDescription);
            }
        }


        public void Write<T>(
           IEnumerable<T> values,
           TextWriter stream,
           ExportFileDescription fileDescription, Func<int, bool> myMethodName)
        {
            WriteData<T>(values, null, stream, fileDescription, myMethodName);
        }

        public void Write<T>(
           IEnumerable<T> values,
           TextWriter stream,
           ExportFileDescription fileDescription)
        {
            WriteData<T>(values, null, stream, fileDescription);
        }

       /*
        public void Write<T>(
         IEnumerable<T> values,
         string fileName,
         ExportFileDescription fileDescription)
        {
            using (StreamWriter sw = new StreamWriter(
                                                fileName,
                                                false,
                                                fileDescription.TextEncoding))
            {
                WriteData<T>(values, fileName, sw, fileDescription);
            }
        }


        public void Write<T>(
            IEnumerable<T> values,
            TextWriter stream)
        {
            Write<T>(values, stream, new ExportFileDescription());
        }

        public void Write<T>(
            IEnumerable<T> values,
            string fileName)
        {
            Write<T>(values, fileName, new ExportFileDescription());
        }

        public void Write<T>(
            IEnumerable<T> values,
            TextWriter stream,
            ExportFileDescription fileDescription)
        {
            WriteData<T>(values, null, stream, fileDescription);
        }

       */
     
        private void WriteData<T>(
    IEnumerable<T> values,
    string fileName,
    TextWriter stream,
    ExportFileDescription fileDescription, Func<int, bool> myMethodName = null)
        {
            FieldMapper<T> fm = new FieldMapper<T>(fileDescription, fileName, true, Key);
            ExportStream cs = new ExportStream(null, stream, fileDescription);
            List<string> row = new List<string>();

          
            // If first line has to carry the field names, write the field names now.
            if (fileDescription.FirstLineHasColumnNames)
            {
                fm.WriteNames(ref row);
                cs.WriteRow(row);
            }


            // -----
            int i = 0;
            foreach (T obj in values)
            {

                i++;
                if (myMethodName != null)
                    myMethodName(i);

                // Convert obj to row
                fm.WriteObject(obj, ref row);
                cs.WriteRow(row);

            }
        }




        public IEnumerable<T> Read<T>(string fileName, ExportFileDescription fileDescription) where T : class, new()
        {
            // Note that ReadData will not be called right away, but when the returned 
            // IEnumerable<T> actually gets accessed.

            IEnumerable<T> ie = ReadData<T>(fileName, null, fileDescription);
            return ie;
        }

        public IEnumerable<T> Read<T>(StreamReader stream) where T : class, new()
        {
            return Read<T>(stream, new ExportFileDescription());
        }

        public IEnumerable<T> Read<T>(string fileName) where T : class, new()
        {
            return Read<T>(fileName, new ExportFileDescription());
        }

        public IEnumerable<T> Read<T>(StreamReader stream, ExportFileDescription fileDescription) where T : class, new()
        {
            return ReadData<T>(null, stream, fileDescription);
        }


        public IEnumerable<T> ReadData<T>(
                string fileName,
                StreamReader stream,
                ExportFileDescription fileDescription) where T : class, new()
        {
            // If T implements IDataRow, then we're reading raw data rows 
            bool readingRawDataRows = typeof(IDataRow).IsAssignableFrom(typeof(T));

#if DEBUG
            List<T> ret = new List<T>();
#endif

            // The constructor for FieldMapper_Reading will throw an exception if there is something
            // wrong with type T. So invoke that constructor before you open the file, because if there
            // is an exception, the file will not be closed.
            //
            // If T implements IDataRow, there is no need for a FieldMapper, because in that case we're returning
            // raw data rows.
            FieldMapperReading<T> fm = null;

            if (!readingRawDataRows)
            {
                fm = new FieldMapperReading<T>(fileDescription, fileName, false, Key);
            }

            // -------
            // Each time the IEnumerable<T> that is returned from this method is 
            // accessed in a foreach, ReadData is called again (not the original Read overload!)
            //
            // So, open the file here, or rewind the stream.

            bool readingFile = !string.IsNullOrEmpty(fileName);

            if (readingFile)
            {
                stream = new StreamReader(
                                    fileName,
                                    fileDescription.TextEncoding,
                                    fileDescription.DetectEncodingFromByteOrderMarks);
            }
            else
            {
                // Rewind the stream

                if ((stream == null) || (!stream.BaseStream.CanSeek))
                {
                   // throw new BadStreamException();
                    throw new Exception();
                }

                stream.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            // ----------

            ExportStream cs = new ExportStream(stream, null, fileDescription);

            // If we're reading raw data rows, instantiate a T so we return objects
            // of the type specified by the caller.
            // Otherwise, instantiate a DataRow, which also implements IDataRow.
            IDataRow row = null;
            if (readingRawDataRows)
            {
                row = new T() as IDataRow;
            }
            else
            {
                row = new DataRow();
            }

           AggregatedException ae =
                new AggregatedException(typeof(T).ToString(), fileName, fileDescription.MaximumNbrExceptions);

            try
            {              
                bool firstRow = true;
                while (cs.ReadRow(ref row))
                {
                    // Skip empty lines.
                    // Important. If there is a newline at the end of the last data line, the code
                    // thinks there is an empty line after that last data line.
                    if ((row.Count == 1) &&
                        ((row[0].Value == null) ||
                         (string.IsNullOrEmpty(row[0].Value.Trim()))))
                    {
                        continue;
                    }


                    fm.CheckValid(row,ae);

                    if (firstRow && fileDescription.FirstLineHasColumnNames)
                    {
                        if (!readingRawDataRows) { fm.ReadNames(row); }
                    }
                    else
                    {
                        T obj = default(T);
                        try
                        {
                            if (readingRawDataRows)
                            {
                                obj = row as T;
                            }
                            else
                            {
                                obj = fm.ReadObject(row,ae); 
                            }
                        }
                        catch (FatalFormatException fex)
                        {
                            throw fex;
                        }
                        catch (AggregatedException ae2)
                        {
                            // Seeing that the AggregatedException was thrown, maximum number of exceptions
                            // must have been reached, so rethrow.
                            // Catch here, so you don't add an AggregatedException to an AggregatedException
                            throw ae2;
                        }
                        catch (Exception ex)
                        {
                            // Store the exception in the AggregatedException ae.
                            // That way, if a file has many errors leading to exceptions,
                            // you get them all in one go, packaged in a single aggregated exception.
                            ae.AddException(ex);
                        }
#if DEBUG
                        ret.Add(obj);
#else
                        yield return obj;
#endif
                    }
                    firstRow = false;
                }
            }
            finally
            {
                if (readingFile)
                {
                    stream.Close();
                }

                // If any exceptions were raised while reading the data from the file,
                // they will have been stored in the AggregatedException ae.
                // In that case, time to throw ae.
                //ae.ThrowIfExceptionsStored();
            }
#if DEBUG
            return ret;
#endif

        }



        public void Dispose()
        {
           // throw new NotImplementedException();
        }
    }
}
