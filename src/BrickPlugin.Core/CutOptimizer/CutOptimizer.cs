using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.CutOptimizer
{
    /// <summary>
    /// 切割优化器 — 执行最小尺寸约束和优先整砖策略
    /// </summary>
    public class CutOptimizer
    {
        /// <summary>
        /// 对排砖结果执行切割约束检查
        /// </summary>
        /// <param name="placements">原始排砖结果</param>
        /// <param name="spec">砖规格</param>
        /// <param name="minCutRatio">最小切割比例（如 1/3）</param>
        /// <param name="mode">约束模式</param>
        /// <returns>优化后的排砖结果和切割记录</returns>
        public (List<BrickPlacement>, List<CutRecord>) Optimize(
            List<BrickPlacement> placements, BrickSpec spec,
            double minCutRatio, CutConstraintMode mode)
        {
            if (mode == CutConstraintMode.None)
                return (placements, new List<CutRecord>());

            var optimized = new List<BrickPlacement>();
            var cutRecords = new List<CutRecord>();
            double minLength = spec.Length * minCutRatio;
            double minWidth = spec.Width * minCutRatio;

            foreach (var placement in placements)
            {
                if (!placement.IsCut)
                {
                    optimized.Add(placement);
                    continue;
                }

                // 检查切割后的砖是否达到最小尺寸要求
                bool tooSmall = placement.ActualLength < minLength ||
                                placement.ActualWidth < minWidth;

                if (tooSmall && mode >= CutConstraintMode.MinSize)
                {
                    // 标记为不合格切割
                    var cutRecord = new CutRecord
                    {
                        BrickId = placement.Id,
                        OriginalLength = spec.Length,
                        OriginalWidth = spec.Width,
                        CutLength = placement.ActualLength,
                        CutWidth = placement.ActualWidth
                    };

                    cutRecords.Add(cutRecord);

                    // PreferFullTile 模式：丢弃不合格的小碎块
                    if (mode >= CutConstraintMode.PreferFullTile)
                        continue; // 不加入 optimized，相当于丢弃
                }

                // 记录切割（即使是合格的）
                if (placement.IsCut)
                {
                    cutRecords.Add(new CutRecord
                    {
                        BrickId = placement.Id,
                        OriginalLength = spec.Length,
                        OriginalWidth = spec.Width,
                        CutLength = placement.ActualLength,
                        CutWidth = placement.ActualWidth
                    });
                }

                optimized.Add(placement);
            }

            return (optimized, cutRecords);
        }

        /// <summary>
        /// 生成材料统计
        /// </summary>
        public MaterialSummary GenerateSummary(List<BrickPlacement> placements,
            List<CutRecord> cutRecords, BrickSpec spec)
        {
            int wholeCount = 0, cutCount = 0;

            foreach (var p in placements)
            {
                if (p.IsWhole) wholeCount++;
                else cutCount++;
            }

            double totalBricks = wholeCount + cutCount;
            double brickArea = (spec.Length * spec.Width) / 1_000_000.0; // mm² → m²
            double totalArea = totalBricks * brickArea;

            double wasteArea = 0;
            foreach (var cr in cutRecords)
                wasteArea += cr.WasteArea / 1_000_000.0;

            return new MaterialSummary
            {
                SpecName = spec.Name,
                WholeBrickCount = wholeCount,
                CutBrickCount = cutCount,
                TotalArea = totalArea,
                WasteArea = wasteArea,
                CutRecords = cutRecords
            };
        }
    }
}
