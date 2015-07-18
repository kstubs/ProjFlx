using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charts;
using System.Xml;
using System.Drawing;

namespace ProjectFlx
{
    public class ChartGenerator
    {
        public static string _localpath = null;

        public static string generateBarChart(Xport.AnyChart.data Data)
        {
            var chart3d = new Histogram3DChart(400, 300, 0, 25, Path.Combine(_localpath, @"ProjectFLX\Documents\styles.xml"), "default");
            chart3d.setChartTemplate(Path.Combine(_localpath, @"ProjectFLX\Documents\Charts.xsl"));
            chart3d.IncludeLegend = false;
            chart3d.WithGuideLines = false;
            chart3d.Shade3D = true;
            chart3d.ColumnGap = 50;
            chart3d.ColumnGroupCount = 2;
            chart3d.Stacked = false;
            
            var svg = chart3d.GenerateChart(Data);
            return ProjectFlx.Schema.Helper.StripXmlDecleration(svg.OuterXml);
            
        }
    }
}
