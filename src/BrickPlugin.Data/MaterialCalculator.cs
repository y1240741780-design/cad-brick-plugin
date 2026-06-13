using System.Collections.Generic;
using System.Linq;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Data
{
    /// <summary>
    /// 材料统计计算器
    /// </summary>
    public class MaterialCalculator
    {
        /// <summary>
        /// 按规格分组统计
        /// </summary>
        public List<MaterialSummary> SummarizeBySpec(List<LayoutResult> results)
        {
            var allPlacements = results.SelectMany(r => r.Placements).ToList();
            var allCutRecords = results.SelectMany(r => r.MaterialSummary?.CutRecords ?? new List<CutRecord>()).ToList();

            // 按原始规格分组
            var groups = allPlacements
                .GroupBy(p => p.OriginalSpec.Name)
                .ToList();

            var summaries = new List<MaterialSummary>();

            foreach (var group in groups)
            {
                var spec = group.First().OriginalSpec;
                int whole = group.Count(p => p.IsWhole);
                int cut = group.Count(p => p.IsCut);
                double brickArea = (spec.Length * spec.Width) / 1_000_000.0;
                double totalArea = (whole + cut) * brickArea;

                var groupCutRecords = allCutRecords
                    .Where(cr => group.Any(p => p.Id == cr.BrickId))
                    .ToList();

                double wasteArea = groupCutRecords.Sum(cr => cr.WasteArea / 1_000_000.0);

                summaries.Add(new MaterialSummary
                {
                    SpecName = spec.Name,
                    WholeBrickCount = whole,
                    CutBrickCount = cut,
                    TotalArea = totalArea,
                    WasteArea = wasteArea,
                    CutRecords = groupCutRecords
                });
            }

            return summaries;
        }

        /// <summary>
        /// 计算总损耗率
        /// </summary>
        public double CalculateTotalWasteRate(List<MaterialSummary> summaries)
        {
            if (summaries == null || summaries.Count == 0) return 0;
            double totalArea = summaries.Sum(s => s.TotalArea);
            double totalWaste = summaries.Sum(s => s.WasteArea);
            return totalArea > 0 ? (totalWaste / totalArea) * 100.0 : 0;
        }

        /// <summary>
        /// 生成材料汇总字符串（用于在 CAD 表格中显示）
        /// </summary>
        public string GenerateSummaryText(MaterialSummary summary)
        {
            return $"规格: {summary.SpecName} | " +
                   $"整砖: {summary.WholeBrickCount}块 | " +
                   $"切割: {summary.CutBrickCount}块 | " +
                   $"总量: {summary.TotalCount}块 | " +
                   $"面积: {summary.TotalArea:F2}m² | " +
                   $"损耗: {summary.WasteRate:F1}%";
        }
    }
}
