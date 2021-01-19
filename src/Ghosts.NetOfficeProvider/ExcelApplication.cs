using NetOffice.ExcelApi;
using NetOffice.ExcelApi.Enums;
using NetOffice.ExcelApi.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Excel = NetOffice.ExcelApi;

namespace Ghosts.NetOfficeProvider
{
    public class ExcelApplication: IMSOfficeApplication
    {
        public ExcelApplicationData CreateExcelApplication()
        {
            // start excel and turn off msg boxes
            Excel.Application excelApplication = new Excel.Application
            {
                DisplayAlerts = false,
                Visible = true
            };

            try
            {
                excelApplication.WindowState = XlWindowState.xlMinimized;
                foreach (Excel.Workbook item in excelApplication.Workbooks)
                {
                    item.Windows[1].WindowState = XlWindowState.xlMinimized;
                }
            }
            catch (Exception e)
            {
                //_log.Trace($"Could not minimize: {e}");
            }

            // create a utils instance, not need for but helpful to keep the lines of code low
            CommonUtils utils = new CommonUtils(excelApplication);

            //_log.Trace("Excel adding workbook");
            // add a new workbook
            Excel.Workbook workBook = excelApplication.Workbooks.Add();
            // _log.Trace("Excel adding worksheet");
            Excel.Worksheet workSheet = (Excel.Worksheet)workBook.Worksheets[1];

            // draw back color and perform the BorderAround method
            workSheet.Range("$B2:$B5").Interior.Color = utils.Color.ToDouble(Color.DarkGreen);
            workSheet.Range("$B2:$B5").BorderAround(XlLineStyle.xlContinuous, XlBorderWeight.xlMedium,
                XlColorIndex.xlColorIndexAutomatic);

            // draw back color and border the range explicitly
            workSheet.Range("$D2:$D5").Interior.Color = utils.Color.ToDouble(Color.DarkGreen);
            workSheet.Range("$D2:$D5")
                .Borders[(Excel.Enums.XlBordersIndex)XlBordersIndex.xlInsideHorizontal]
                .LineStyle = XlLineStyle.xlDouble;
            workSheet.Range("$D2:$D5")
                .Borders[(Excel.Enums.XlBordersIndex)XlBordersIndex.xlInsideHorizontal]
                .Weight = 4;
            workSheet.Range("$D2:$D5")
                .Borders[(Excel.Enums.XlBordersIndex)XlBordersIndex.xlInsideHorizontal]
                .Color = utils.Color.ToDouble(Color.Black);

            ExcelApplicationData excelApplicationData = new ExcelApplicationData();
            excelApplicationData.ExcelApplication = excelApplication;
            excelApplicationData.ExcelWorkBook = workBook;
            excelApplicationData.ExcelWorkSheet = workSheet;

            return excelApplicationData;
        }
    }
    public class ExcelApplicationData
    {
        public Workbook ExcelWorkBook { get; set; }
        public Worksheet ExcelWorkSheet { get; set; }

        public Application ExcelApplication { get; set; }
    }
}
