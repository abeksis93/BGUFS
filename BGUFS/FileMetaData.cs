using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGUFS
{
    class FileMetaData
    {
        string fileName;
        long fileSize;
        DateTime fileDate;
        byte[] fileData;

        public FileMetaData(string name, long size, DateTime date, byte[] data)
        {
            this.fileName = name;
            this.fileSize = size;
            this.fileDate = date;
            this.fileData = data;
        }

        // Setters


        // Getters
        

        // print method
        public void printFileMetaData(FileMetaData fmd)
        {
            Console.WriteLine(" print ");
        }

    }
}
