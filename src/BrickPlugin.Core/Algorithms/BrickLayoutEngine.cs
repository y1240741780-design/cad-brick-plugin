using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fanben.BrickPlugin.Core.Algorithms;
using Fanben.BrickPlugin.Core.AreaAnalyzer;
using Fanben.BrickPlugin.Core.CutOptimizer;
using Fanben.BrickPlugin.Core.Models;
using Fanben.BrickPlugin.Core.StartPoint;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 排砖引擎 — 主入口，编排整个排砖流程
    /// </summary>
    public class BrickLayoutEngine
    {
        private readonly Dictionary<LayoutPattern, IPatternStrategy> _strategies;
        private readonly StartPointResolver _startPointResolver;
        private readonly CutOptimizer _cutOptimizer;
        private readonly AreaAnalyzer _areaAnalyzer;

        public BrickLayoutEngine()
        {
            _startPointResolver = new StartPointResolver();
            _cutOptimizer = new CutOptimizer();
            _areaAnalyzer = new AreaAnalyzer();

            _strategies = new Dictionary<LayoutPattern, IPatternStrategy>
            {
                [LayoutPattern.Straight] = new StraightPattern(),
                [LayoutPattern.BrickBond] = new BrickBondPattern(),
                [LayoutPattern.Herringbone] = new HerringbonePattern(),
                [LayoutPattern.Diagonal] = new DiagonalPattern()
            };
        }

        /// <summary>
        /// 执行排砖计算
        /// </summary>
        public LayoutResult Execute(LayoutRequest request)
        {
            var sw = Stopwatch.StartNew();
            var result = new LayoutResult { Request = request };

            try
            {
                // 校验输入
                if (request.Regions == null || request.Regions.Count == 0)
                    throw new ArgumentException("没有指定排砖区域");
                if (request.BrickSpec == null)
                    throw new ArgumentException("没有指定砖规格");

                // 确保包围盒已计算
                foreach (var region in request.Regions)
                {
                    if (region.BoundaryPoints != null && region.BoundaryPoints.Count > 0)
                        region.BoundingBox = _areaAnalyzer.ComputeBoundingBox(region.BoundaryPoints);
                }

                // 获取排砖策略
                if (!_strategies.TryGetValue(request.Pattern, out var strategy))
                    throw new NotSupportedException($"不支持的铺贴方式: {request.Pattern}");

                // 设置工字铺参数
                if (strategy is BrickBondPattern brickBond)
                    brickBond.OffsetRatio = request.BrickBondOffset;

                // 对每个区域执行排砖
                var allPlacements = new List<BrickPlacement>();

                foreach (var region in request.Regions)
                {
                    // 计算起铺点
                    var startPoint = _startPointResolver.Resolve(
                        region, request.BrickSpec, request.JointSetting,
                        request.StartPointMode, request.CornerDirection,
                        request.ManualStartPoint);

                    // 执行排版
                    var regionPlacements = strategy.Layout(
                        region, request.BrickSpec, request.JointSetting, startPoint);

                    allPlacements.AddRange(regionPlacements);
                }

                // 切割优化
                var (optimized, cutRecords) = _cutOptimizer.Optimize(
                    allPlacements, request.BrickSpec, request.MinCutRatio, request.CutMode);

                // 生成材料统计
                var summary = _cutOptimizer.GenerateSummary(
                    optimized, cutRecords, request.BrickSpec);

                result.Placements = optimized;
                result.MaterialSummary = summary;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// 为墙砖模式自动识别门窗洞口
        /// </summary>
        public void AnalyzeWallOpenings(TileRegion wallRegion,
            List<List<Point2D>> innerPolylines)
        {
            wallRegion.Openings = _areaAnalyzer.DetectOpenings(wallRegion, innerPolylines);
        }
    }
}
