using System;

namespace Fanben.BrickPlugin.Core.Models
{
    /// <summary>
    /// 排砖类型
    /// </summary>
    public enum TileType
    {
        /// <summary>墙砖 — 沿墙面/立面排布</summary>
        Wall = 0,

        /// <summary>地砖 — 在封闭区域内填满</summary>
        Floor = 1
    }

    /// <summary>
    /// 铺贴方式（排列图案）
    /// </summary>
    public enum LayoutPattern
    {
        /// <summary>直铺 / 通缝铺 — 砖对齐排列</summary>
        Straight = 0,

        /// <summary>工字铺 / 错缝铺 — 砖错开 1/2 或 1/3</summary>
        BrickBond = 1,

        /// <summary>人字铺 — Herringbone，45° 交错</summary>
        Herringbone = 2,

        /// <summary>斜铺 — 整体图案旋转 45°</summary>
        Diagonal = 3
    }

    /// <summary>
    /// 起铺点模式
    /// </summary>
    public enum StartPointMode
    {
        /// <summary>自动居中 — 从区域中心开始排</summary>
        Center = 0,

        /// <summary>角落起铺 — 从左下角整砖起步</summary>
        Corner = 1,

        /// <summary>用户手动指定起铺点</summary>
        Manual = 2
    }

    /// <summary>
    /// 角落起铺的方向
    /// </summary>
    public enum CornerDirection
    {
        BottomLeft = 0,
        BottomRight = 1,
        TopLeft = 2,
        TopRight = 3
    }

    /// <summary>
    /// 避让模式
    /// </summary>
    public enum AvoidanceMode
    {
        /// <summary>不避让</summary>
        None = 0,

        /// <summary>自动检测门窗洞口</summary>
        AutoDetect = 1,

        /// <summary>用户手动指定避让区域</summary>
        ManualSpecify = 2,

        /// <summary>两者都启用</summary>
        Both = 3
    }

    /// <summary>
    /// 呈现方式
    /// </summary>
    [Flags]
    public enum PresentationStyle
    {
        /// <summary>无呈现</summary>
        None = 0,

        /// <summary>轮廓多段线</summary>
        Outline = 1,

        /// <summary>实体块 (BlockReference)</summary>
        Block = 2,

        /// <summary>Hatch 填充图案</summary>
        Hatch = 4,

        /// <summary>全部呈现</summary>
        All = Outline | Block | Hatch
    }

    /// <summary>
    /// 切割约束模式
    /// </summary>
    public enum CutConstraintMode
    {
        /// <summary>无限制</summary>
        None = 0,

        /// <summary>最小尺寸限制（小块不小于砖的指定比例）</summary>
        MinSize = 1,

        /// <summary>优先整砖 + 最小尺寸限制</summary>
        PreferFullTile = 2,

        /// <summary>综合：最小尺寸 + 优先整砖 + 切割前确认</summary>
        Comprehensive = 3
    }
}
