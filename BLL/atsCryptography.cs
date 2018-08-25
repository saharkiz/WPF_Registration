using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Data;

class atsCryptography
{
    private static byte[] Main_rgbKey = ASCIIEncoding.ASCII.GetBytes("123123");
    private static byte[] Main_rgbIV = ASCIIEncoding.ASCII.GetBytes("321321");

    public static string EncryptIt(string encryptData)
    {
        try
        {
            byte[] data = ASCIIEncoding.ASCII.GetBytes(encryptData);
            byte[] rgbKey = Main_rgbKey;
            byte[] rgbIV = Main_rgbIV;
            //1024-bit encryption
            MemoryStream memoryStream = new MemoryStream(1024);
            DESCryptoServiceProvider desCryptoServiceProvider = new DESCryptoServiceProvider();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, desCryptoServiceProvider.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            byte[] result = new byte[(int)memoryStream.Position];
            memoryStream.Position = 0;
            memoryStream.Read(result, 0, result.Length);
            cryptoStream.Close();
            memoryStream.Dispose();
            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static string DecryptIt(string toDecrypt)
    {
        string decrypted = string.Empty;
        try
        {
            byte[] data = System.Convert.FromBase64String(toDecrypt);
            byte[] rgbKey = Main_rgbKey;
            byte[] rgbIV = Main_rgbIV;
            //1024-bit decryption
            MemoryStream memoryStream = new MemoryStream(data.Length);
            DESCryptoServiceProvider desCryptoServiceProvider = new DESCryptoServiceProvider();
            ICryptoTransform x = desCryptoServiceProvider.CreateDecryptor(rgbKey, rgbIV);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, x, CryptoStreamMode.Read);
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            decrypted = new StreamReader(cryptoStream).ReadToEnd();
            cryptoStream.Close();
            memoryStream.Dispose();

        }
        catch (Exception ex)
        {
            throw ex;
        }
        return decrypted;
    }

    /// <summary>
    /// Converts a given delimited file into a dataset. 
    /// Assumes that the first line    
    /// of the text file contains the column names.
    /// </summary>
    /// <param name="File">The name of the file to open</param>    
    /// <param name="TableName">The name of the 
    /// Table to be made within the DataSet returned</param>
    /// <param name="delimiter">The string to delimit by</param>
    /// <returns></returns>  
    public static DataSet csvConvert(string File,
     string TableName, string delimiter)
    {
        //The DataSet to Return
        DataSet result = new DataSet();

        //Open the file in a stream reader.
        StreamReader s = new StreamReader(File);

        //Split the first line into the columns       
        string[] columns = s.ReadLine().Split(delimiter.ToCharArray());

        //Add the new DataTable to the RecordSet
        result.Tables.Add(TableName);

        //Cycle the colums, adding those that don't exist yet 
        //and sequencing the one that do.
        foreach (string col in columns)
        {
            bool added = false;
            string next = "";
            int i = 0;
            while (!added)
            {
                //Build the column name and remove any unwanted characters.
                string columnname = col + next;
                columnname = columnname.Replace("#", "");
                columnname = columnname.Replace("'", "");
                columnname = columnname.Replace("&", "");

                //See if the column already exists
                if (!result.Tables[TableName].Columns.Contains(columnname))
                {
                    //if it doesn't then we add it here and mark it as added
                    result.Tables[TableName].Columns.Add(columnname);
                    added = true;
                }
                else
                {
                    //if it did exist then we increment the sequencer and try again.
                    i++;
                    next = "_" + i.ToString();
                }
            }
        }

        //Read the rest of the data in the file.        
        string AllData = s.ReadToEnd();

        //Split off each row at the Carriage Return/Line Feed
        //Default line ending in most windows exports.  
        //You may have to edit this to match your particular file.
        //This will work for Excel, Access, etc. default exports.
        string[] rows = AllData.Split("\r\n".ToCharArray());

        //Now add each row to the DataSet        
        foreach (string r in rows)
        {
            //Split the row at the delimiter.
            string[] items = r.Split(delimiter.ToCharArray());
            if (items.Count() > 2)
            {
                //Add the item only if atleast 2 columns are present
                result.Tables[TableName].Rows.Add(items);
            }
        }

        //Return the imported data.        
        return result;
    }
}