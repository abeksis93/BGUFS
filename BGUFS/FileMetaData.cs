using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGUFS
{
    [Serializable]
    class FileMetaData
    {
        private string fileName;
        private long fileSize;
        private DateTime fileDate;
        private string status;
        private string linkedFileName;
        private string fileData;


        public FileMetaData(string name, long size, DateTime date, string stat, string data, string linked = null)
        {
            this.fileName = name;
            this.fileSize = size;
            this.fileDate = date;
            this.status = stat;
            this.linkedFileName = linked;
            this.fileData = data;
        }

        // Setters
        public void setFileName(string name) { this.fileName = name; }
        public void setFileSize(long size) { this.fileSize = size; }
        public void setFileDate(DateTime date) { this.fileDate = date; }
        public void setFileData(string data) { this.fileData = data; }

        // Getters
        public string getFileName() { return this.fileName; }
        public long getFileSize() { return this.fileSize; }
        public DateTime getFileDate() { return this.fileDate; }
        public string getFileData() { return this.fileData; }


        // print method
        public void printFileMetaData()
        {
            if(this.linkedFileName == null)
            {
                Console.WriteLine("{0},{1},{2},{3}", this.fileName, this.fileSize.ToString(), this.fileDate.ToString(), this.status);
            }
            else
            {
                Console.WriteLine("{0},{1},{2},{3},{4}", this.fileName, this.fileSize.ToString(), this.fileDate.ToString(), this.status, this.linkedFileName); ;
            }
        }


    }
}
