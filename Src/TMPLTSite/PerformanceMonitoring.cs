using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ProjectFlx
{
    internal class PerformanceMonitoring
    {

        /// <summary>
        /// DO NOT USE UNTIL FIGURE OUT HOW TO ENABLE PERFORMANC METRICS IN IIS and .NET
        /// </summary>
        /// <returns></returns>
        internal static XmlDocument getProcessInfoHistory()
        {

            XmlDocument doc;
            string nodeFormat = "<info StartTime=\"{0,12:MM/dd hh:mmt}\" ProcessID=\"{1,5}\" Status=\"{2,12}\" TotalSeconds=\"{3,10:0}\" RequestCount=\"{4,12}\" ShutDownReason=\"{5,18}\" PeakMemoryUsed=\"{6,15:0k}\"/>";
            StringBuilder sb = new StringBuilder();
            using(XmlWriter xwriter = XmlWriter.Create(sb))
            {
            xwriter.WriteStartElement("process");


                ProcessInfo[] history = ProcessModelInfo.GetHistory(100);
                

            for ( int i=0; i<history.Length; i++ ) {
            {
                var obj = new Object[] 
                  {
                    history[i].StartTime, 
                    history[i].ProcessID, 
                    history[i].Status, 
                    history[i].Age.TotalSeconds,
                    history[i].RequestCount, 
                    history[i].ShutdownReason, 
                    history[i].PeakMemoryUsed
                  };
                xwriter.WriteRaw(String.Format(nodeFormat, obj));
            }
       }
            doc = new XmlDocument();
            doc.LoadXml(sb.ToString());
            }
            return doc;
        }

    }
}
