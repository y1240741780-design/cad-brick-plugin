using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Data
{
    /// <summary>
    /// Excel 导出器 — 使用 ClosedXML 生成 .xlsx 文件
    /// </summary>
    public class ExcelExporter
    {
        /// <summary>
        /// 导出排砖结果到 Excel 文件
        /// </summary>
        /// <param name="result">排砖结果</param>
        /// <param name="filePath">输出路径</param>
        /// <returns>是否成功</returns>
        public bool ExportToExcel(LayoutResult result, string filePath)
        {
            try
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    // Sheet 1: 材料汇总表
                    ExportMaterialSummary(workbook, result.MaterialSummary);

                    // Sheet 2: 切割明细表
                    ExportCutDetails(workbook, result.MaterialSummary?.CutRecords);

                    // Sheet 3: 排版数据
                    ExportPlacementData(workbook, result.Placements);

                    // 保存
                    workbook.SaveAs(filePath);
                }
                return true;
            }
            catch (Exception)
            {
                // 如果 ClosedXML 不可用，回退到 CSV 导出
                return ExportToCsvFallback(result, filePath);
            }
        }

        private void ExportMaterialSummary(ClosedXML.Excel.XLWorkbook wb, MaterialSummary summary)
        {
            var ws = wb.Worksheets.Add("材料汇总");
            ws.Cell(1, 1).Value = "规格";
            ws.Cell(1, 2).Value = "整砖数";
            ws.Cell(1, 3).Value = "切割砖数";
            ws.Cell(1, 4).Value = "总用量";
            ws.Cell(1, 5).Value = "总面积(m²)";
            ws.Cell(1, 6).Value = "废料面积(m²)";
            ws.Cell(1, 7).Value = "损耗率(%)";

            ws.Cell(2, 1).Value = summary.SpecName;
            ws.Cell(2, 2).Value = summary.WholeBrickCount;
            ws.Cell(2, 3).Value = summary.CutBrickCount;
            ws.Cell(2, 4).Value = summary.TotalCount;
            ws.Cell(2, 5).Value = Math.Round(summary.TotalArea, 2);
            ws.Cell(2, 6).Value = Math.Round(summary.WasteArea, 2);
            ws.Cell(2, 7).Value = Math.Round(summary.WasteRate, 1);

            // 样式
            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Columns().AdjustToContents();
        }

        private void ExportCutDetails(ClosedXML.Excel.XLWorkbook wb, List<CutRecord> records)
        {
            var ws = wb.Worksheets.Add("切割明细");
            ws.Cell(1, 1).Value = "砖编号";
            ws.Cell(1, 2).Value = "原始长(mm)";
            ws.Cell(1, 3).Value = "原始宽(mm)";
            ws.Cell(1, 4).Value = "切割长(mm)";
            ws.Cell(1, 5).Value = "切割宽(mm)";
            ws.Cell(1, 6).Value = "废料面积(mm²)";

            if (records != null)
            {
                int row = 2;
                foreach (var cr in records)
                {
                    ws.Cell(row, 1).Value = cr.BrickId;
                    ws.Cell(row, 2).Value = cr.OriginalLength;
                    ws.Cell(row, 3).Value = cr.OriginalWidth;
                    ws.Cell(row, 4).Value = cr.CutLength;
                    ws.Cell(row, 5).Value = cr.CutWidth;
                    ws.Cell(row, 6).Value = Math.Round(cr.WasteArea, 1);
                    row++;
                }
            }

            var headerRange = ws.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Columns().AdjustToContents();
        }

        private void ExportPlacementData(ClosedXML.Excel.XLWorkbook wb, List<BrickPlacement> placements)
        {
            var ws = wb.Worksheets.Add("排版数据");
            ws.Cell(1, 1).Value = "砖编号";
            ws.Cell(1, 2).Value = "区域";
            ws.Cell(1, 3).Value = "X坐标";
            ws.Cell(1, 4).Value = "Y坐标";
            ws.Cell(1, 5).Value = "旋转角";
            ws.Cell(1, 6).Value = "实际长(mm)";
            ws.Cell(1, 7).Value = "实际宽(mm)";
            ws.Cell(1, 8).Value = "是否切割";
            ws.Cell(1, 9).Value = "行";
            ws.Cell(1, 10).Value = "列";

            int row = 2;
            foreach (var p in placements)
            {
                ws.Cell(row, 1).Value = p.Id;
                ws.Cell(row, 2).Value = p.RegionName;
                ws.Cell(row, 3).Value = Math.Round(p.Position.X, 1);
                ws.Cell(row, 4).Value = Math.Round(p.Position.Y, 1);
                ws.Cell(row, 5).Value = p.Rotation;
                ws.Cell(row, 6).Value = Math.Round(p.ActualLength, 1);
                ws.Cell(row, 7).Value = Math.Round(p.ActualWidth, 1);
                ws.Cell(row, 8).Value = p.IsCut ? "是" : "否";
                ws.Cell(row, 9).Value = p.Row;
                ws.Cell(row, 10).Value = p.Column;
                row++;
            }

            var headerRange = ws.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Columns().AdjustToContents();
        }

        /// <summary>
        /// CSV 回退方案（当 ClosedXML 不可用时）
        /// </summary>
        private bool ExportToCsvFallback(LayoutResult result, string filePath)
        {
            try
            {
                string csvPath = Path.ChangeExtension(filePath, ".csv");

                using (var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8))
                {
                    // 写入材料汇总
                    var summary = result.MaterialSummary;
                    writer.WriteLine("规格,整砖数,切割砖数,总用量,总面积(m²),废料面积(m²),损耗率(%)");
                    if (summary != null)
                    {
                        writer.WriteLine($"{summary.SpecName},{summary.WholeBrickCount},{summary.CutBrickCount}," +
                            $"{summary.TotalCount},{summary.TotalArea:F2},{summary.WasteArea:F2},{summary.WasteRate:F1}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("砖编号,区域,X,Y,旋转,长,宽,切割,行,列");

                    foreach (var p in result.Placements)
                    {
                        writer.WriteLine($"{p.Id},{p.RegionName},{p.Position.X:F1},{p.Position.Y:F1}," +
                            $"{p.Rotation},{p.ActualLength:F1},{p.ActualWidth:F1},{(p.IsCut ? "是" : "否")},{p.Row},{p.Column}");
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
