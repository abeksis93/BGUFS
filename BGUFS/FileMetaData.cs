using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGUFS
{
    class FileMetaData
    {
        private string fileName;
        private long fileSize;
        private DateTime fileDate;
        private byte[] fileData;

        public FileMetaData(string name, long size, DateTime date, byte[] data)
        {
            this.fileName = name;
            this.fileSize = size;
            this.fileDate = date;
            this.fileData = data;
        }

        // Setters
        public void setFileName(string name) { this.fileName = name; }
        public void setFileSize(long size) { this.fileSize = size; }
        public void setFileDate(DateTime date) { this.fileDate = date; }
        public void setFileData(byte[] data) { this.fileData = data; }

        // Getters
        public string getFileName() { return this.fileName; }
        public long getFileSize() { return this.fileSize; }
        public DateTime getFileDate() { return this.fileDate; }
        public byte[] getFileData() { return this.fileData; }


        // print method
        public void printFileMetaData(FileMetaData fmd)
        {
            Console.WriteLine(" print ");
        }

    }
}
