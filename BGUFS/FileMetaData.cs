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
        //private string fileData;
        //private string md5hash;
        //private int lineNum;
        //private long fileStartIndex;

        // string data, string md5, int line)
        public FileMetaData(string name, long size, DateTime date, string stat, string linked = null) 
        {
            this.fileName = name;
            this.fileSize = size;
            this.fileDate = date;
            this.status = stat;
            this.linkedFileName = linked;
            //this.fileData = data;
            //this.md5hash = md5;
            //this.lineNum = line;
            //this.fileStartIndex = startIndex;
        }

        // Setters
        public void setFileName(string name) { this.fileName = name; }
        public void setFileSize(long size) { this.fileSize = size; }
        public void setFileDate(DateTime date) { this.fileDate = date; }
        //public void setFileData(string data) { this.fileData = data; }
        public void setStatus(string stat) { this.status = stat; }
        //public void setMD5(string md5) { this.md5hash = md5; }
        //public void setLineNum(int line) { this.lineNum = line; }
        public void setLinkedFileName(string linked) { this.linkedFileName = linked; }
        //public void setStartIndex(long startIndex) { this.fileStartIndex = startIndex; }

        // Getters
        public string getFileName() { return this.fileName; }
        public long getFileSize() { return this.fileSize; }
        public DateTime getFileDate() { return this.fileDate; }
        //public string getFileData() { return this.fileData; }
        public string getStatus() { return this.status; }
        //public string getMD5() { return this.md5hash; }
        //public int getLineNum() { return this.lineNum; }
        public string getLinkedFileName() { return this.linkedFileName; }
        //public long getStartIndex() { return this.fileStartIndex; }

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
