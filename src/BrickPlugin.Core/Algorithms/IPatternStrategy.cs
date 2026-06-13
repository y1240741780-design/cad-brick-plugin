using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 排砖策略接口 — 策略模式，每种铺贴方式一个实现
    /// </summary>
    public interface IPatternStrategy
    {
        /// <summary>铺贴方式名称</summary>
        LayoutPattern Pattern { get; }

        /// <summary>
        /// 对指定区域执行排砖计算
        /// </summary>
        /// <param name="region">排砖区域</param>
        /// <param name="spec">砖规格</param>
        /// <param name="joint">灰缝设置</param>
        /// <param name="startPoint">起铺点</param>
        /// <returns>砖块放置列表</returns>
        List<BrickPlacement> Layout(TileRegion region, BrickSpec spec,
                                    JointSetting joint, Point2D startPoint);
    }
}
