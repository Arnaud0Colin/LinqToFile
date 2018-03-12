using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;


namespace LinqToFile
{

    public enum Align
    {
        None = 0,
        Left = 1,
        Right = 4,
    }

     [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property |System.AttributeTargets.Class,
                            Inherited = true,
                           AllowMultiple = true)]
    public class FileColumnAttribute : System.Attribute, IComparable<FileColumnAttribute>
     {

         public ushort Key { get; set; }

         public string Property { get; set; }
         public string Name { get; set; }
         public bool UsePropertyAsName /*UsePropertyAsName*/ { get; set; }

         public bool PassOver { get; set; }
         public bool CanBeNull { get; set; }
         public UInt16 FieldIndex { get; set; }
         public UInt16 MaxLength { get; set; }
         public NumberStyles NumberStyle { get; set; }
         public string OutputFormat { get; set; }
         public Align TextAlign = Align.None;
         public bool WithOutSeparator { get; set; }
         public char FillChar { get; set; }

         public UInt16 AutoIndex { get; set; }

         public Type fieldType { get; set; }
         public System.Reflection.MemberInfo MemberInfo { get; set; }

         private static UInt16 staticAutoIndex = 0;

         public System.Reflection.MethodInfo parseNumberMethod;
         public System.Reflection.MethodInfo parseDateMethod;
         public System.ComponentModel.TypeConverter typeConverter { get; set; }

     

         private void InitValue()
         {
             AutoIndex = staticAutoIndex++;
             Key = 0;
             Property = null;
             UsePropertyAsName = false;
             Name = null; // "";
             FieldIndex = UInt16.MaxValue;
             MaxLength = UInt16.MaxValue;
             CanBeNull = true;
             NumberStyle = NumberStyles.Any;
             OutputFormat = "G";
             FillChar = ' ';
             PassOver = false;
         }

          public FileColumnAttribute()
          {
              InitValue();
          }

      

         public int CompareTo(FileColumnAttribute other)
         {
             return FieldIndex.CompareTo(other.FieldIndex);
         }



        
     }



   
 }
